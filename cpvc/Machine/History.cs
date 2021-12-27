using System;
using System.Collections.Generic;

namespace CPvC
{
    public enum HistoryChangedAction
    {
        Add,
        DeleteBookmark,
        DeleteBranch,
        SetCurrent
    }


    public class History
    {
        private RootHistoryNode _rootNode;
        private HistoryNode _currentNode;

        private HashSet<HistoryNode> _nodes;

        public delegate void HistoryEventDelegate(HistoryEvent historyEvent, HistoryChangedAction changeAction);

        public HistoryEventDelegate Auditors;

        private int _nextId;

        public History()
        {
            _nodes = new HashSet<HistoryNode>();
            _nextId = 0;

            _rootNode = new RootHistoryNode();

            _nodes.Add(_rootNode);

            _currentNode = _rootNode;
        }

        public CoreActionHistoryEvent AddCoreAction(CoreAction coreAction)
        {
            // Instead of continually adding "RunUntil" actions, just keep updating the
            // current one if it's a RunUntil, and only notify once we've finished. That
            // happens either when we add a non-RunUntil node after a RunUntil node, or
            // when we change the current node and the current node is a RunUntil. See
            // SetCurrentNode for that case.
            bool notify = (coreAction.Type != CoreRequest.Types.RunUntil);
            if (!notify)
            {
                CoreActionHistoryNode currentCoreActionNode = _currentNode as CoreActionHistoryNode;
                if (currentCoreActionNode != null &&
                    currentCoreActionNode.Children.Count == 0 &&
                    currentCoreActionNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    currentCoreActionNode.CoreAction.StopTicks = coreAction.StopTicks;

                    return currentCoreActionNode.HistoryEvent as CoreActionHistoryEvent;
                }
            }

            CoreActionHistoryNode historyNode = new CoreActionHistoryNode(_nextId++, coreAction.Ticks, coreAction, _currentNode, DateTime.Now);

            AddChildNode(historyNode, notify);

            return historyNode.HistoryEvent as CoreActionHistoryEvent;
        }

        public BookmarkHistoryEvent AddBookmark(UInt64 ticks, Bookmark bookmark)
        {
            CoreActionHistoryNode currentCoreActionNode = _currentNode as CoreActionHistoryNode;
            if (currentCoreActionNode != null &&
                currentCoreActionNode.Children.Count == 0 &&
                currentCoreActionNode.CoreAction.Type == CoreRequest.Types.RunUntil)
            {
                // This should probably be added to AddChildNode!
                Auditors?.Invoke(currentCoreActionNode.HistoryEvent, HistoryChangedAction.Add);
            }

            BookmarkHistoryNode historyNode = new BookmarkHistoryNode(_nextId++, ticks, bookmark, _currentNode, DateTime.Now);

            if (ticks < _currentNode.Ticks)
            {
                throw new Exception("Can't add a bookmark with a smaller ticks than current!");
            }

            AddChildNode(historyNode, true);

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

            set
            {
                if (!_nodes.Contains(value.Node))
                {
                    throw new Exception("Attempted to set the current event to an event that doesn't belong to this history!");
                }

                // If the current node is a RunUntil, finish it off by sending a notification...
                CoreActionHistoryNode currentCoreActionNode = _currentNode as CoreActionHistoryNode;
                if (currentCoreActionNode != null && currentCoreActionNode.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    Auditors?.Invoke(_currentNode.HistoryEvent, HistoryChangedAction.Add);
                }

                _currentNode = value.Node;

                Auditors?.Invoke(_currentNode.HistoryEvent, HistoryChangedAction.SetCurrent);
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
