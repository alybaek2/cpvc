using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class HistoryListTree : ListTree<HistoryEvent>
    {
        public HistoryListTree(History history)
        {
            SetHistory(history);
        }

        public void SetHistory(History history)
        {
            if (_history != null)
            {
                _history.Auditors -= ProcessHistoryChange;
            }

            _history = history;

            if (_history != null)
            {
                Init();
                _history.Auditors += ProcessHistoryChange;
            }
        }

        private void Init()
        {
            InitRoot(_history.RootEvent);

            List<HistoryEvent> nodes = new List<HistoryEvent>();
            nodes.AddRange(_history.RootEvent.Children);

            while (nodes.Any())
            {
                HistoryEvent historyEvent = nodes[0];
                nodes.RemoveAt(0);

                AddEventToListTree(historyEvent);

                nodes.AddRange(historyEvent.Children);
            }
        }

        public void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {
            PositionChangedEventArgs<HistoryEvent> changeArgs = UpdateListTree(args);
            if (changeArgs != null)
            {
                PositionChanged?.Invoke(this, changeArgs);
            }
        }

        static private bool InterestingEvent(HistoryEvent historyEvent)
        {
            if (historyEvent is RootHistoryEvent ||
                historyEvent is BookmarkHistoryEvent ||
                historyEvent.Children.Count != 1)
            {
                return true;
            }

            return false;
        }

        private ListTreeNode<HistoryEvent> Add(ListTreeNode<HistoryEvent> parent, HistoryEvent historyEvent)
        {
            ListTreeNode<HistoryEvent> child = new ListTreeNode<HistoryEvent>(historyEvent);

            return Add(parent, child);
        }

        private ListTreeNode<HistoryEvent> Add(ListTreeNode<HistoryEvent> parent, ListTreeNode<HistoryEvent> child)
        {

            // Check first if child already exists in the tree! And that parent exists in the tree!

            // Insert into children of parent first!
            // But! We must either add a new child, or inject this child inbetween the parent and an existing child!
            // Which case are we?

            // This class should not be doing this! HistoryControl should be... ListTree should be made to be generic at some point!
            int descendentChildIndex = 0;
            while (descendentChildIndex < parent.Children.Count)
            {
                if (child.Data.IsEqualToOrAncestorOf(parent.Children[descendentChildIndex].Data))
                {
                    break;
                }

                descendentChildIndex++;
            }

            // This is wrong! All of parent's children could be descendents of child!

            int childIndex;
            if (descendentChildIndex < parent.Children.Count)
            {
                ListTreeNode<HistoryEvent> descendentChild = parent.Children[descendentChildIndex];
                childIndex = descendentChildIndex;

                child.Children.Add(descendentChild);
                descendentChild.Parent = child;

                parent.Children.RemoveAt(descendentChildIndex);
            }
            else
            {
                childIndex = GetChildIndex(parent, child);
            }

            parent.Children.Insert(childIndex, child);
            child.Parent = parent;
            _eventsToNodes.Add(child.Data, child);

            // Insert into horizontal events!
            int newHorizontalIndex = GetHorizontalInsertionIndex(parent, childIndex);

            _horizontalNodes.Insert(newHorizontalIndex, child);

            RefreshHorizontalPositions(newHorizontalIndex);

            // Insert vertically!
            int verticalIndex = GetVerticalIndex(child);
            _verticalNodes.Insert(verticalIndex, child);

            RefreshVerticalPositions(verticalIndex);

            return child;
        }



        private bool AddEventToListTree(HistoryEvent historyEvent)
        {
            HistoryEvent parentHistoryEvent = historyEvent.Parent;
            ListTreeNode<HistoryEvent> parentNode = GetNode(parentHistoryEvent);
            bool wasParentInteresting = parentNode != null;
            bool isParentInteresting = InterestingEvent(parentHistoryEvent);
            ListTreeNode<HistoryEvent> node = GetNode(historyEvent);

            if (node != null)
            {
                // The node is already in the tree... we shouldn't be trying to add it!
                throw new Exception("Node was already in the tree.");
            }

            if (!InterestingEvent(historyEvent))
            {
                return false;
            }

            bool add = true;

            // First, check if the parent's interestingness has changed from false to true.
            if (!wasParentInteresting && isParentInteresting)
            {
                // Need to add the parent!

                // But first, find the child who will share this new parent!
                ListTreeNode<HistoryEvent> cousinNode;
                HistoryEvent he = parentHistoryEvent;
                while (true)
                {
                    he = he.Children[0];
                    cousinNode = GetNode(he);
                    if (cousinNode != null)
                    {
                        break;
                    }
                }

                parentNode = InsertNewParent(cousinNode, parentHistoryEvent);
            }
            else if (!wasParentInteresting && !isParentInteresting)
            {
                // Work our way up the tree to find who should be our parent!
                HistoryEvent he = parentHistoryEvent;
                while (true)
                {
                    ListTreeNode<HistoryEvent> n = GetNode(he);
                    if (n != null)
                    {
                        parentNode = n;
                        break;
                    }

                    he = he.Parent;
                }
            }
            else if (wasParentInteresting && !isParentInteresting)
            {
                // Replace the parent node with the child!
                Update(parentHistoryEvent, historyEvent);
                add = false;
            }
            else if (wasParentInteresting && isParentInteresting)
            {
                // Nothing to do! Just add the new node!
            }

            if (add && InterestingEvent(historyEvent))
            {
                Add(parentNode, historyEvent);
            }

            return true;
        }

        private PositionChangedEventArgs<HistoryEvent> UpdateListTree(HistoryChangedEventArgs args)
        {
            bool changed = false;

            switch (args.Action)
            {
                case HistoryChangedAction.Add:
                    {
                        //changed = AddEventToListTree(args.HistoryEvent);
                        changed = RefreshNode(args.HistoryEvent.Parent, InterestingEvent(args.HistoryEvent.Parent));
                        changed |= RefreshNode(args.HistoryEvent, InterestingEvent(args.HistoryEvent));
                    }
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                        changed = Update(node);
                        //changed = RefreshNode(args.HistoryEvent);
                    }
                    break;
                case HistoryChangedAction.DeleteBranch:
                    {
                        List<HistoryEvent> eventsToDelete = new List<HistoryEvent>();
                        eventsToDelete.Add(args.HistoryEvent);

                        while (eventsToDelete.Any())
                        {
                            HistoryEvent historyEvent = eventsToDelete[0];
                            eventsToDelete.RemoveAt(0);

                            ListTreeNode<HistoryEvent> node = GetNode(historyEvent);
                            if (node == null)
                            {
                                eventsToDelete.AddRange(historyEvent.Children);
                            }
                            else
                            {
                                RemoveRecursive(node);
                                changed = true;
                            }
                        }

                        ListTreeNode<HistoryEvent> parentNode = GetNode(args.OriginalParentEvent);
                        if (parentNode != null)
                        {
                            if (!InterestingEvent(args.OriginalParentEvent))
                            {
                                RemoveNonRecursive(parentNode);
                            }

                            changed |= true;
                        }
                        else
                        {
                            if (AddEventToListTree(args.OriginalParentEvent))
                            {
                                changed = true;
                            }
                        }
                    }
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    {
                        ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                        // Special case
                        ListTreeNode<HistoryEvent> originalParentNode = GetNode(args.OriginalParentEvent);
                        bool interestingParent = InterestingEvent(args.OriginalParentEvent);
                        if (originalParentNode == null && interestingParent)
                        {
                            // Just swap out the data!
                            node.Data = args.OriginalParentEvent;
                            changed = true;
                        }
                        else if (originalParentNode == null && !interestingParent)
                        {
                            // Go up the tree until we find an event with a node! Then add the deleted nodes' children to that node.
                            throw new NotImplementedException();
                        }
                        else if (originalParentNode != null && interestingParent)
                        {
                            // Just move the children of the deleted node over to originalParentNode!
                            throw new NotImplementedException();
                        }
                        else
                        {
                            // Original parent node exists but is no longer interesting. Could happen if the parent node had 2 children, but now
                            // only has one as a result of this bookmark node being deleted. Need to remove the parent node and add its remaining
                            // children to its parent.
                            throw new NotImplementedException();
                        }

                        //AddEventToListTree(args.OriginalParentEvent);

                        //RemoveNonRecursive(node);

                        //changed = true;

                        // The parent of the deleted bookmark event is now null... this screws up the moving of child nodes to the ancestor node!
                        // Need to find a solution to this!
                        //changed = RefreshNode(args.OriginalParentEvent, InterestingEvent(args.OriginalParentEvent));
                        //changed |= RefreshNode(args.HistoryEvent, false);

                        // Has the parent event gone from being not interesting to interesting? If so, make a new node and insert it
                        // Or, why not just change the Data for the existing node? Need to update _eventsToNodes of course!

                        //HistoryEvent interestingParentEvent = args.OriginalParentEvent;
                        //while (interestingParentEvent != null)
                        //{
                        //    if (InterestingEvent(interestingParentEvent))
                        //    {
                        //        break;
                        //    }

                        //    interestingParentEvent = interestingParentEvent.Parent;
                        //}

                        //// Does this event have a node?
                        //if (!_eventsToNodes.TryGetValue(interestingParentEvent, out ListTreeNode<HistoryEvent> parentNode))
                        //{
                        //    // Now figure out which node this new node should belong to!
                        //    ListTreeNode<HistoryEvent> interestingParentParentNode = null;
                        //    HistoryEvent interestingParentParentEvent = interestingParentEvent.Parent;
                        //    while (interestingParentParentEvent != null)
                        //    {
                        //        if (_eventsToNodes.TryGetValue(interestingParentParentEvent, out interestingParentParentNode))
                        //        {
                        //            break;
                        //        }

                        //        interestingParentParentEvent = interestingParentParentEvent.Parent;
                        //    }

                        //    parentNode = CreateNode(interestingParentParentNode, interestingParentEvent);


                        //}

                        //// If the parent event is already interesting, move the deleted nodes children
                        //ListTreeNode<HistoryEvent> nodeToDelete = _eventsToNodes[args.HistoryEvent];

                        //foreach (ListTreeNode<HistoryEvent> childNode in nodeToDelete.Children)
                        //{
                        //    _horizontalNodes.Remove(childNode);
                        //    //_eventsToNodes.Remove(childNode.Data);
                        //}

                        //RefreshHorizontalPositions(0);
                        ////RefreshVerticalPositions(0);

                        //foreach (ListTreeNode<HistoryEvent> childNode in nodeToDelete.Children)
                        //{
                        //    int childIndex = GetChildIndex(parentNode, childNode);
                        //    childNode.Parent = parentNode;
                        //    parentNode.Children.Insert(childIndex, childNode);

                        //    int horizontalIndex = GetHorizontalInsertionIndex(parentNode, childIndex);
                        //    _horizontalNodes.Insert(horizontalIndex, childNode);

                        //    RefreshHorizontalPositions(0);
                        //}

                        //// 
                        //parentNode.Children.Remove(nodeToDelete);
                        //_verticalNodes.Remove(nodeToDelete);
                        //nodeToDelete.Parent = null;
                        //_horizontalNodes.Remove(nodeToDelete);
                        //_eventsToNodes.Remove(args.HistoryEvent);

                        //RefreshHorizontalPositions(0);
                        //RefreshVerticalPositions(0);

                        //changed = true;
                    }
                    break;
            }

            if (changed)
            {
                PositionChangedEventArgs<HistoryEvent> changeArgs = new PositionChangedEventArgs<HistoryEvent>(HorizontalOrdering(), VerticalOrdering());

                return changeArgs;
            }

            return null;
        }

        private ListTreeNode<HistoryEvent> CreateNode(ListTreeNode<HistoryEvent> parentNode, HistoryEvent childEvent)
        {
            // Child node is assumed to be interesting!
            ListTreeNode<HistoryEvent> node = new ListTreeNode<HistoryEvent>(childEvent);

            int childIndex = GetChildIndex(parentNode, node);
            parentNode.Children.Insert(childIndex, node);
            node.Parent = parentNode;
            int horizontalIndex = GetHorizontalInsertionIndex(parentNode, childIndex);
            _horizontalNodes.Insert(horizontalIndex, node);
            int verticalIndex = GetVerticalIndex(node);
            _verticalNodes.Insert(verticalIndex, node);
            _eventsToNodes.Add(childEvent, node);

            return node;
        }

        private bool RefreshNode(HistoryEvent historyEvent, bool isVisible)
        {
            bool wasVisible = _eventsToNodes.ContainsKey(historyEvent);
            //bool isVisible = InterestingEvent(historyEvent) && !remove;

            if (wasVisible && !isVisible)
            {
                // Remove the node and attach its children to the node's parents.
                ListTreeNode<HistoryEvent> node = _eventsToNodes[historyEvent];
                ListTreeNode<HistoryEvent> parentNode = node.Parent;

                parentNode.Children.Remove(node);
                _verticalNodes.Remove(node);
                node.Parent = null;
                _horizontalNodes.Remove(node);
                _eventsToNodes.Remove(historyEvent);

                foreach (ListTreeNode<HistoryEvent> childNode in node.Children)
                {
                    _horizontalNodes.Remove(childNode);
                    //_eventsToNodes.Remove(childNode.Data);
                }

                foreach (ListTreeNode<HistoryEvent> childNode in node.Children)
                {
                    int childIndex = GetChildIndex(parentNode, childNode);
                    childNode.Parent = parentNode;
                    parentNode.Children.Insert(childIndex, childNode);

                    int horizontalIndex = GetHorizontalInsertionIndex(parentNode, childIndex);
                    _horizontalNodes.Insert(horizontalIndex, childNode);
                }

                RefreshHorizontalPositions(0);
                RefreshVerticalPositions(0);

                return true;
            }
            else if (!wasVisible && isVisible)
            {
                // Insert a new node in the tree!

                // First, find our closest immediate ancestor in the tree.
                ListTreeNode<HistoryEvent> ancestorNode = null;
                HistoryEvent h = historyEvent.Parent;
                while (h != null)
                {
                    if (_eventsToNodes.ContainsKey(h))
                    {
                        ancestorNode = _eventsToNodes[h];
                        break;
                    }

                    h = h.Parent;
                }

                // Add the node!
                ListTreeNode<HistoryEvent> node = new ListTreeNode<HistoryEvent>(historyEvent);

                int childIndex = GetChildIndex(ancestorNode, node);
                ancestorNode.Children.Insert(childIndex, node);
                node.Parent = ancestorNode;
                int horizontalIndex = GetHorizontalInsertionIndex(ancestorNode, childIndex);
                _horizontalNodes.Insert(horizontalIndex, node);
                int verticalIndex = GetVerticalIndex(node);
                _verticalNodes.Insert(verticalIndex, node);
                _eventsToNodes.Add(historyEvent, node);


                // Go through all of ancestorNode's children and move them over to the new node if they're descendents
                for (int c = ancestorNode.Children.Count - 1; c >= 0; c--)
                {
                    ListTreeNode<HistoryEvent> childNode = ancestorNode.Children[c];
                    if (ReferenceEquals(node, childNode))
                    {
                        continue;
                    }

                    if (node.Data.IsEqualToOrAncestorOf(childNode.Data))
                    {
                        ancestorNode.Children.RemoveAt(c);
                        node.Children.Insert(0, childNode);
                        childNode.Parent = node;

                        _horizontalNodes.Remove(childNode);
                        horizontalIndex = GetHorizontalInsertionIndex(node, 0);
                        _horizontalNodes.Insert(horizontalIndex, childNode);

                        _verticalNodes.Remove(childNode);
                        verticalIndex = GetVerticalIndex(childNode);
                        _verticalNodes.Insert(verticalIndex, childNode);

                        //_eventsToNodes.Add(childNode.Data, childNode);
                    }
                }

                RefreshHorizontalPositions(0);
                RefreshVerticalPositions(0);

                return true;
            }
            else
            {
                // No change!
            }

            return false;
        }

        protected override int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            UInt64 yMax = y.GetMaxDescendentTicks();
            UInt64 xMax = x.GetMaxDescendentTicks();
            int result = yMax.CompareTo(xMax);

            if (result != 0)
            {
                return result;
            }

            return y.Id.CompareTo(x.Id);
        }

        protected override int VerticalSort(HistoryEvent x, HistoryEvent y)
        {
            if (_verticalTies.Any())
            {
                _verticalTies.Remove(new Tuple<HistoryEvent, HistoryEvent>(x, y));
                _verticalTies.Remove(new Tuple<HistoryEvent, HistoryEvent>(y, x));
            }

            if (x.Ticks < y.Ticks)
            {
                return -1;
            }
            else if (x.Ticks > y.Ticks)
            {
                return 1;
            }
            else
            {
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x.IsEqualToOrAncestorOf(y))
                {
                    return -1;
                }
                else if (y.IsEqualToOrAncestorOf(x))
                {
                    return 1;
                }
            }

            _verticalTies.Add(new Tuple<HistoryEvent, HistoryEvent>(x, y));
            _verticalTies.Add(new Tuple<HistoryEvent, HistoryEvent>(y, x));

            return x.Id.CompareTo(y.Id);
        }

        private History _history;

        public event NotifyPositionChangedEventHandler<HistoryEvent> PositionChanged;
    }
}
