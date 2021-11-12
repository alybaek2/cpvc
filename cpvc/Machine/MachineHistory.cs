using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public enum HistoryEventType
    {
        None,
        AddCoreAction,
        AddBookmark,
        DeleteEvent,
        DeleteEventAndChildren,
        SetCurrent
    }

    internal class HistoryNode
    {
        public HistoryNode()
        {
            _historyEvent = new HistoryEvent(this);
            Children = new List<HistoryNode>();
        }

        // Do we need a HistoryNodeType, perhaps? Type can only be AddCoreAction or AddBookmark.
        public HistoryEventType Type;
        public UInt64 Ticks;
        public CoreAction CoreAction;
        public Bookmark Bookmark;

        public DateTime CreateDate;

        public HistoryNode Parent;
        public List<HistoryNode> Children
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
                if (_historyNode.Type == HistoryEventType.AddCoreAction && _historyNode.CoreAction.Type == CoreRequest.Types.RunUntil)
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

        public delegate void HistoryEventDelegate(HistoryEvent historyEvent, UInt64 ticks, HistoryEventType type, CoreAction coreAction, Bookmark bookmark);

        public HistoryEventDelegate Auditors;

        public MachineHistory()
        {
            _nodes = new HashSet<HistoryNode>();

            _rootNode = new HistoryNode
            {
                Type = HistoryEventType.None,
                Parent = null,
                Ticks = 0
            };

            _nodes.Add(_rootNode);

            _currentNode = _rootNode;
        }

        public HistoryEvent AddCoreAction(CoreAction coreAction)
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
                    _currentNode.Type == HistoryEventType.AddCoreAction &&
                    _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    _currentNode.CoreAction.StopTicks = coreAction.StopTicks;

                    return _currentNode.HistoryEvent;
                }
            }

            HistoryNode historyNode = new HistoryNode
            {
                Type = HistoryEventType.AddCoreAction,
                Ticks = coreAction.Ticks,
                CoreAction = coreAction,
                Parent = _currentNode,
                CreateDate = DateTime.Now
            };

            AddChildNode(historyNode, notify);

            return historyNode.HistoryEvent;
        }

        public HistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark)
        {
            if (_currentNode.Children.Count == 0 &&
                _currentNode.Type == HistoryEventType.AddCoreAction &&
                _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
            {
                // This should probably be added to AddChildNode!
                Auditors?.Invoke(_currentNode.HistoryEvent, _currentNode.Ticks, _currentNode.Type, _currentNode.CoreAction, _currentNode.Bookmark);
            }

            HistoryNode historyNode = new HistoryNode
            {
                Type = HistoryEventType.AddBookmark,
                Ticks = ticks,
                Bookmark = bookmark,
                Parent = _currentNode,
                CreateDate = DateTime.Now
            };

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

                Auditors?.Invoke(historyEvent, 0, HistoryEventType.DeleteEventAndChildren, null, null);

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

                Auditors?.Invoke(historyEvent, 0, HistoryEventType.DeleteEvent, null, null);

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


        public void Write(CompactedMachineFile machineFile)
        {
            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryNode> historyNodes = new List<HistoryNode>();
            historyNodes.AddRange(_rootNode.Children);

            int? newCurrentNodeId = null;

            int persistentId = 0;

            Dictionary<HistoryNode, int> nodeIds = new Dictionary<HistoryNode, int>();

            HistoryNode previousNode = null;
            while (historyNodes.Count > 0)
            {
                int currentPersistentId = persistentId;
                HistoryNode currentNode = historyNodes[0];
                nodeIds[currentNode] = currentPersistentId;
                persistentId++;

                if (previousNode != currentNode.Parent && previousNode != null)
                {
                    machineFile.WriteCurrent(nodeIds[currentNode.Parent]);
                }

                switch (currentNode.Type)
                {
                    case HistoryEventType.AddCoreAction:
                        machineFile.WriteCoreAction(currentPersistentId, currentNode.Ticks, currentNode.CoreAction);
                        break;
                    case HistoryEventType.AddBookmark:
                        machineFile.WriteAddBookmark(currentPersistentId, currentNode.Ticks, currentNode.Bookmark);
                        break;
                    default:
                        throw new Exception("Unexpected node type!");
                }

                if (currentNode == _currentNode)
                {
                    newCurrentNodeId = currentPersistentId;
                }

                historyNodes.RemoveAt(0);
                previousNode = currentNode;

                // Place the current event's children at the top of the "stack". This effectively means we're doing a depth-first traversion of the history tree.
                historyNodes.InsertRange(0, currentNode.Children);
            }

            if (newCurrentNodeId != null)
            {
                machineFile.WriteCurrent(newCurrentNodeId.Value);
            }
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
                if (_currentNode.Type == HistoryEventType.AddCoreAction && _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    Auditors?.Invoke(_currentNode.HistoryEvent, _currentNode.Ticks, _currentNode.Type, _currentNode.CoreAction, _currentNode.Bookmark);
                }

                _currentNode = historyNode;

                Auditors?.Invoke(historyNode.HistoryEvent, 0, HistoryEventType.SetCurrent, null, null);
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
                Auditors?.Invoke(historyNode.HistoryEvent, historyNode.Ticks, historyNode.Type, historyNode.CoreAction, historyNode.Bookmark);
            }
        }
    }
}
