using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CPvC
{
    public enum HistoryChangedAction
    {
        Add,
        DeleteBookmark,
        DeleteBranch,
        SetCurrent,
        UpdateCurrent
    }


    public class History : INotifyPropertyChanged
    {
        private RootHistoryNode _rootNode;
        private HistoryNode _currentNode;

        private HashSet<HistoryNode> _nodes;

        public delegate void HistoryEventDelegate(HistoryEvent historyEvent, HistoryChangedAction changeAction);

        public event HistoryChangedEventHandler Auditors;

        private int _nextId;

        public History()
        {
            _nodes = new HashSet<HistoryNode>();
            _nextId = 0;

            _rootNode = new RootHistoryNode();

            _nodes.Add(_rootNode);

            _currentNode = _rootNode;
        }

        public bool IsClosedEvent(HistoryEvent historyEvent)
        {
            return
                historyEvent != CurrentEvent ||
                historyEvent.Children.Count > 0 ||
                !(historyEvent is CoreActionHistoryEvent coreActionHistoryEvent) ||
                !(coreActionHistoryEvent.CoreAction is RunUntilAction);
        }

        public HistoryEvent MostRecentClosedEvent(HistoryEvent historyEvent)
        {
            if (!IsClosedEvent(historyEvent))
            {
                return historyEvent.Parent;
            }

            return historyEvent;
        }

        public CoreActionHistoryEvent AddCoreAction(IMachineAction coreAction)
        {
            return AddCoreAction(coreAction, _nextId);
        }

        public CoreActionHistoryEvent AddCoreAction(IMachineAction coreAction, int id)
        {
            // Instead of continually adding "RunUntil" actions, just keep updating the
            // current one if it's a RunUntil, and only notify once we've finished. That
            // happens either when we add a non-RunUntil node after a RunUntil node, or
            // when we change the current node and the current node is a RunUntil. See
            // SetCurrentNode for that case.
            if (coreAction is RunUntilAction runUntilAction)
            {
                if (_currentNode is CoreActionHistoryNode currentCoreActionNode &&
                    currentCoreActionNode.Children.Count == 0 &&
                    currentCoreActionNode.CoreAction is RunUntilAction currentCoreAction)
                {
                    currentCoreAction.StopTicks = runUntilAction.StopTicks;

                    HistoryChangedEventArgs args = new HistoryChangedEventArgs(this, _currentNode.HistoryEvent, HistoryChangedAction.UpdateCurrent, null, null, null);
                    Auditors?.Invoke(this, args);

                    return currentCoreActionNode.HistoryEvent as CoreActionHistoryEvent;
                }
            }

            CoreActionHistoryNode historyNode = new CoreActionHistoryNode(id, coreAction.Ticks, coreAction, _currentNode, DateTime.Now);

            _nextId = Math.Max(_nextId, id) + 1;

            AddChildNode(historyNode);

            return historyNode.HistoryEvent as CoreActionHistoryEvent;
        }

        public BookmarkHistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark)
        {
            return AddBookmark(ticks, bookmark, DateTime.UtcNow, _nextId);
        }

        public BookmarkHistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark, DateTime creationTime)
        {
            return AddBookmark(ticks, bookmark, creationTime, _nextId);
        }

        public BookmarkHistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark, DateTime creationTime, int id)
        {
            BookmarkHistoryNode historyNode = new BookmarkHistoryNode(id, ticks, bookmark, _currentNode, creationTime);

            _nextId = Math.Max(_nextId, id) + 1;

            if (ticks < _currentNode.Ticks)
            {
                throw new Exception("Can't add a bookmark with a smaller ticks than current!");
            }

            AddChildNode(historyNode);

            return historyNode.HistoryEvent as BookmarkHistoryEvent;
        }

        public bool DeleteBranch(HistoryEvent historyEvent)
        {
            HistoryNode historyNode = historyEvent.Node;
            if (!_nodes.Contains(historyNode))
            {
                throw new Exception("Attempted to delete a branch that doesn't belong to this history!");
            }

            if (historyNode == _rootNode)
            {
                throw new Exception("Can't delete root node!");
            }

            if (historyNode.IsEqualToOrAncestorOf(_currentNode))
            {
                return false;
            }

            HistoryNode parent = historyNode.Parent;
            parent.Children.Remove(historyNode);
            historyNode.Parent = null;

            // Remove this node, and all its children
            List<HistoryNode> nodesToRemove = new List<HistoryNode>();
            nodesToRemove.Add(historyNode);
            while (nodesToRemove.Count > 0)
            {
                HistoryNode childHistoryNode = nodesToRemove[0];

                _nodes.Remove(childHistoryNode);

                nodesToRemove.AddRange(childHistoryNode.Children);
                nodesToRemove.RemoveAt(0);
            }

            parent.InvalidateCachedMDT();

            Notify(historyEvent, HistoryChangedAction.DeleteBranch, parent.HistoryEvent, null, null);

            return true;
        }

        public bool DeleteBookmark(HistoryEvent historyEvent)
        {
            HistoryNode historyNode = historyEvent.Node;
            if (!_nodes.Contains(historyNode))
            {
                throw new Exception("Attempted to delete a bookmark history event that doesn't belong to this history!");
            }

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

            List<HistoryEvent> movedChildren = new List<HistoryEvent>(historyEvent.Children);

            historyNode.Children.Clear();
            historyNode.Parent.Children.Remove(historyNode);
            historyNode.Parent.InvalidateCachedMDT();
            HistoryEvent oldParentEvent = historyNode.Parent.HistoryEvent;
            historyNode.Parent = null;
            _nodes.Remove(historyNode);

            Notify(historyEvent, HistoryChangedAction.DeleteBookmark, oldParentEvent, movedChildren, null);

            return true;
        }

        //public bool Contains(HistoryEvent historyEvent)
        //{
        //    return _nodes.Contains(historyEvent.Node);
        //}

        // Browsing methods
        public RootHistoryEvent RootEvent
        {
            get
            {
                return _rootNode.HistoryEvent as RootHistoryEvent;
            }
        }

        public HistoryEvent CurrentEvent
        {
            get
            {
                return _currentNode.HistoryEvent;
            }

            set
            {
                if (!_nodes.Contains(value.Node))
                {
                    throw new Exception("Attempted to set the current event to an event that doesn't belong to this history!");
                }

                if (_currentNode == value.Node)
                {
                    return;
                }

                HistoryNode originalCurrent = _currentNode;

                _currentNode = value.Node;

                Notify(_currentNode.HistoryEvent, HistoryChangedAction.SetCurrent, null, null, originalCurrent.HistoryEvent);

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void AddChildNode(HistoryNode historyNode)
        {
            _currentNode.Children.Add(historyNode);
            _nodes.Add(historyNode);

            // Could probably just get the MDT of the new node, and see if it's bigger...
            _currentNode.InvalidateCachedMDT();

            _currentNode = historyNode;
            OnPropertyChanged(nameof(CurrentEvent));

            Notify(historyNode.HistoryEvent, HistoryChangedAction.Add, null, null, null);
        }

        private void Notify(HistoryEvent historyEvent, HistoryChangedAction action, HistoryEvent originalParentHistoryEvent, List<HistoryEvent> originalChildrenEvents, HistoryEvent originalCurrent)
        {
            HistoryChangedEventArgs args = new HistoryChangedEventArgs(this, historyEvent, action, originalParentHistoryEvent, originalChildrenEvents, originalCurrent);
            Auditors?.Invoke(this, args);
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
