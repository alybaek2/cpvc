﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public enum HistoryEventType
    {
        Root,
        CoreAction,
        Bookmark
    }

    public enum HistoryChangedAction
    {
        Add,
        Delete,
        DeleteRecursive,
        SetCurrent
    }

    internal class HistoryNode
    {
        internal HistoryNode()
        {
            Id = MachineHistory.RootId;
            Type = HistoryEventType.Root;
            Ticks = 0;
            Bookmark = null;
            CoreAction = null;
            Parent = null;
            CreateDate = DateTime.Now;

            _historyEvent = new HistoryEvent(this);
            Children = new List<HistoryNode>();
        }

        internal HistoryNode(int id, UInt64 ticks, Bookmark bookmark, HistoryNode parent, DateTime createDate)
        {
            Id = id;
            Type = HistoryEventType.Bookmark;
            Ticks = ticks;
            Bookmark = bookmark;
            CoreAction = null;
            Parent = parent;
            CreateDate = createDate;

            _historyEvent = new HistoryEvent(this);
            Children = new List<HistoryNode>();
        }

        internal HistoryNode(int id, UInt64 ticks, CoreAction action, HistoryNode parent, DateTime createDate)
        {
            Id = id;
            Type = HistoryEventType.CoreAction;
            Ticks = ticks;
            Bookmark = null;
            CoreAction = action;
            Parent = parent;
            CreateDate = createDate;

            _historyEvent = new HistoryEvent(this);
            Children = new List<HistoryNode>();
        }

        // Do we need a HistoryNodeType, perhaps? Type can only be AddCoreAction or AddBookmark.
        public HistoryEventType Type { get; private set; }
        public UInt64 Ticks { get; private set; }
        public CoreAction CoreAction { get; private set; }
        public Bookmark Bookmark { get; private set; }

        public DateTime CreateDate { get; private set; }

        public HistoryNode Parent { get; set; }
        public List<HistoryNode> Children
        {
            get;
        }

        public int Id
        {
            get;
        }

        private readonly HistoryEvent _historyEvent;

        public HistoryEvent HistoryEvent
        {
            get
            {
                return _historyEvent;
            }
        }

        public bool IsEqualToOrAncestorOf(HistoryNode ancestor)
        {
            while (ancestor != null)
            {
                if (ancestor == this)
                {
                    return true;
                }

                ancestor = ancestor.Parent;
            }

            return false;
        }
    }

    public class HistoryEvent
    {
        internal HistoryNode _historyNode;

        internal HistoryEvent(HistoryNode historyNode)
        {
            _historyNode = historyNode;
        }

        public HistoryEvent Parent
        {
            get
            {
                return _historyNode.Parent?.HistoryEvent;
            }
        }

        public List<HistoryEvent> Children
        {
            get
            {
                return _historyNode.Children.Select(x => x.HistoryEvent).ToList();
            }
        }

        public HistoryEventType Type
        {
            get
            {
                return _historyNode.Type;
            }
        }

        public int Id
        {
            get
            {
                return _historyNode.Id;
            }
        }

        public Bookmark Bookmark
        {
            get
            {
                return _historyNode.Bookmark;
            }
        }

        public CoreAction CoreAction
        {
            get
            {
                return _historyNode.CoreAction;
            }
        }

        public UInt64 Ticks
        {
            get
            {
                return _historyNode.Ticks;
            }
        }

        public UInt64 EndTicks
        {
            get
            {
                if (_historyNode.Type == HistoryEventType.CoreAction && _historyNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    return _historyNode.CoreAction.StopTicks;
                }

                return _historyNode.Ticks;
            }
        }

        public DateTime CreateDate
        {
            get
            {
                return _historyNode.CreateDate;
            }
        }

        public bool IsEqualToOrAncestorOf(HistoryEvent ancestor)
        {
            return _historyNode.IsEqualToOrAncestorOf(ancestor?._historyNode);
        }

        /// <summary>
        /// Returns the maximum ticks value of any given HistoryEvent's descendents. Used when sorting children in <c>AddEventToItem</c>.
        /// </summary>
        /// <param name="historyEvent">The HistoryEvent object.</param>
        /// <returns></returns>
        public UInt64 GetMaxDescendentTicks()
        {
            List<HistoryEvent> events = new List<HistoryEvent>();
            events.Add(this);
            UInt64 maxTicks = EndTicks;

            int i = 0;
            while (i < events.Count)
            {
                HistoryEvent e = events[0];
                events.RemoveAt(0);

                if (e.Children.Count == 0)
                {
                    if (e.EndTicks > maxTicks)
                    {
                        maxTicks = e.EndTicks;
                    }
                }
                else
                {
                    events.AddRange(e.Children);
                }
            }

            return maxTicks;
        }
    }

    public class MachineHistory
    {
        private HistoryNode _rootNode;
        private HistoryNode _currentNode;

        private HashSet<HistoryNode> _nodes;

        public delegate void HistoryEventDelegate(HistoryEvent historyEvent, UInt64 ticks, HistoryChangedAction changeAction, CoreAction coreAction, Bookmark bookmark);

        public HistoryEventDelegate Auditors;

        internal const int RootId = -1;
        private int _nextId;

        public MachineHistory()
        {
            _nodes = new HashSet<HistoryNode>();
            _nextId = 0;

            _rootNode = new HistoryNode();

            _nodes.Add(_rootNode);

            _currentNode = _rootNode;
        }

        public HistoryEvent AddCoreAction(CoreAction coreAction)
        {
            return AddCoreAction(coreAction, _nextId++);
        }

        public HistoryEvent AddCoreAction(CoreAction coreAction, int id)
        {
            // Instead of continually adding "RunUntil" actions, just keep updating the
            // current one if it's a RunUntil, and only notify once we've finished. That
            // happens either when we add a non-RunUntil node after a RunUntil node, or
            // when we change the current node and the current node is a RunUntil. See
            // SetCurrentNode for that case.
            bool notify = (coreAction.Type != CoreRequest.Types.RunUntil);
            if (!notify)
            {
                if (_currentNode.Children.Count == 0 &&
                    _currentNode.Type == HistoryEventType.CoreAction &&
                    _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    _currentNode.CoreAction.StopTicks = coreAction.StopTicks;

                    return _currentNode.HistoryEvent;
                }
            }

            HistoryNode historyNode = new HistoryNode(id, coreAction.Ticks, coreAction, _currentNode, DateTime.Now);

            _nextId = Math.Max(_nextId, id + 1);

            AddChildNode(historyNode, notify);

            return historyNode.HistoryEvent;
        }

        public HistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark)
        {
            return AddBookmark(ticks, bookmark, _nextId++);
        }

        public HistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark, int id)
        {
            if (_currentNode.Children.Count == 0 &&
                _currentNode.Type == HistoryEventType.CoreAction &&
                _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
            {
                // This should probably be added to AddChildNode!
                Auditors?.Invoke(_currentNode.HistoryEvent, _currentNode.Ticks, HistoryChangedAction.Add, _currentNode.CoreAction, _currentNode.Bookmark);
            }

            HistoryNode historyNode = new HistoryNode(id, ticks, bookmark, _currentNode, DateTime.Now);

            _nextId = Math.Max(_nextId, id + 1);

            if (ticks < _currentNode.Ticks)
            {
                throw new Exception("Can't add a bookmark with a smaller ticks than current!");
            }

            AddChildNode(historyNode, true);

            return historyNode.HistoryEvent;
        }

        public bool DeleteEventAndChildren(HistoryEvent historyEvent)
        {
            HistoryNode historyNode = historyEvent._historyNode;
            if (_nodes.Contains(historyNode))
            {
                if (historyNode == _rootNode)
                {
                    throw new Exception("Can't delete root node!");
                }

                if (historyEvent.IsEqualToOrAncestorOf(_currentNode.HistoryEvent))
                {
                    return false;
                }

                HistoryNode parent = historyNode.Parent;
                if (parent == null)
                {
                    return false;
                }

                parent.Children.Remove(historyNode);
                historyNode.Parent = null;

                // Remove this node, and all its children
                List<HistoryNode> eventsToRemove = new List<HistoryNode>();
                eventsToRemove.Add(historyNode);
                while (eventsToRemove.Count > 0)
                {
                    HistoryNode h = eventsToRemove[0];

                    _nodes.Remove(h);

                    eventsToRemove.AddRange(h.Children);
                    eventsToRemove.RemoveAt(0);
                }

                Auditors?.Invoke(historyEvent, 0, HistoryChangedAction.DeleteRecursive, null, null);

                return true;
            }
            else
            {
                throw new Exception("This history event doesn't belong to us!");
            }
        }

        public bool DeleteEvent(HistoryEvent historyEvent)
        {
            HistoryNode historyNode = historyEvent._historyNode;
            if (_nodes.Contains(historyNode))
            {
                if (historyNode == _rootNode)
                {
                    throw new Exception("Can't delete root node!");
                }

                if (historyNode == _currentNode)
                {
                    return false;
                }

                foreach (HistoryNode child in historyNode.Children)
                {
                    child.Parent = historyNode.Parent;
                    historyNode.Parent.Children.Add(child);
                }

                historyNode.Children.Clear();
                historyNode.Parent.Children.Remove(historyNode);
                historyNode.Parent = null;

                Auditors?.Invoke(historyEvent, 0, HistoryChangedAction.Delete, null, null);

                return true;
            }
            else
            {
                throw new Exception("This history event doesn't belong to us!");
            }
        }

        public void SetCurrent(HistoryEvent historyEvent)
        {
            SetCurrentNode(historyEvent._historyNode);
        }

        public HistoryEvent MostRecentBookmark()
        {
            HistoryEvent historyEvent = CurrentEvent;
            while (historyEvent.Type != HistoryEventType.Bookmark && historyEvent != RootEvent)
            {
                historyEvent = historyEvent.Parent;
            }

            return historyEvent;
        }

        // Browsing methods
        public HistoryEvent RootEvent
        {
            get
            {
                return _rootNode.HistoryEvent;
            }
        }

        public HistoryEvent CurrentEvent
        {
            get
            {
                return _currentNode.HistoryEvent;
            }
        }

        private void SetCurrentNode(HistoryNode historyNode)
        {
            if (_nodes.Contains(historyNode))
            {
                // If the current node is a RunUntil, finish it off by sending a notification...
                if (_currentNode.Type == HistoryEventType.CoreAction && _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    Auditors?.Invoke(_currentNode.HistoryEvent, _currentNode.Ticks, HistoryChangedAction.Add, _currentNode.CoreAction, _currentNode.Bookmark);
                }

                _currentNode = historyNode;

                Auditors?.Invoke(historyNode.HistoryEvent, 0, HistoryChangedAction.SetCurrent, null, null);
            }
            else
            {
                throw new Exception("This history event doesn't belong to us!");
            }
        }

        private void AddChildNode(HistoryNode historyNode, bool notify)
        {
            _currentNode.Children.Add(historyNode);
            _nodes.Add(historyNode);
            _currentNode = historyNode;

            if (notify)
            {
                Auditors?.Invoke(historyNode.HistoryEvent, historyNode.Ticks, HistoryChangedAction.Add, historyNode.CoreAction, historyNode.Bookmark);
            }
        }
    }
}
