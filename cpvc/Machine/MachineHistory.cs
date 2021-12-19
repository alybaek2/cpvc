using System;
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
        DeleteBookmark,
        DeleteBranch,
        SetCurrent
    }


    public class MachineHistory
    {
        private HistoryNode _rootNode;
        private HistoryNode _currentNode;

        private HashSet<HistoryNode> _nodes;

        public delegate void HistoryEventDelegate(HistoryEvent historyEvent, HistoryChangedAction changeAction);

        public HistoryEventDelegate Auditors;

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
            return AddCoreAction(coreAction, _nextId);
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

            _nextId = Math.Max(_nextId, id) + 1;

            AddChildNode(historyNode, notify);

            return historyNode.HistoryEvent;
        }

        public HistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark)
        {
            return AddBookmark(ticks, bookmark, _nextId);
        }

        public HistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark, int id)
        {
            if (_currentNode.Children.Count == 0 &&
                _currentNode.Type == HistoryEventType.CoreAction &&
                _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
            {
                // This should probably be added to AddChildNode!
                Auditors?.Invoke(_currentNode.HistoryEvent, HistoryChangedAction.Add);
            }

            HistoryNode historyNode = new HistoryNode(id, ticks, bookmark, _currentNode, DateTime.Now);

            _nextId = Math.Max(_nextId, id) + 1;

            if (ticks < _currentNode.Ticks)
            {
                throw new Exception("Can't add a bookmark with a smaller ticks than current!");
            }

            AddChildNode(historyNode, true);

            return historyNode.HistoryEvent;
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

            Auditors?.Invoke(historyEvent, HistoryChangedAction.DeleteBranch);

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

            historyNode.Children.Clear();
            historyNode.Parent.Children.Remove(historyNode);
            historyNode.Parent = null;

            Auditors?.Invoke(historyEvent, HistoryChangedAction.DeleteBookmark);

            return true;
        }

        public void SetCurrent(HistoryEvent historyEvent)
        {
            if (!_nodes.Contains(historyEvent.Node))
            {
                throw new Exception("Attempted to set the current event to an event that doesn't belong to this history!");
            }

            // If the current node is a RunUntil, finish it off by sending a notification...
            if (_currentNode.Type == HistoryEventType.CoreAction && _currentNode.CoreAction.Type == CoreRequest.Types.RunUntil)
            {
                Auditors?.Invoke(_currentNode.HistoryEvent, HistoryChangedAction.Add);
            }

            _currentNode = historyEvent.Node;

            Auditors?.Invoke(_currentNode.HistoryEvent, HistoryChangedAction.SetCurrent);
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

        private void AddChildNode(HistoryNode historyNode, bool notify)
        {
            _currentNode.Children.Add(historyNode);
            _nodes.Add(historyNode);
            _currentNode = historyNode;

            if (notify)
            {
                Auditors?.Invoke(historyNode.HistoryEvent, HistoryChangedAction.Add);
            }
        }
    }
}
