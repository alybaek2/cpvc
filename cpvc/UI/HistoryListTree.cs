using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class HistoryListTree : ListTree<HistoryEvent>
    {
        public HistoryListTree(History history)
        {
            SetHistory(history);

            _interestingParents = new Dictionary<HistoryEvent, HistoryEvent>();
        }

        public Dictionary<HistoryEvent, HistoryEvent> InterestingParents
        {
            get
            {
                _interestingParents.Clear();

                foreach (ListTreeNode<HistoryEvent> node in _horizontalNodes)
                {
                    if (node.Parent == null)
                    {
                        // This is the root!
                        continue;
                    }

                    _interestingParents.Add(node.Data, GetInterestingParent(node.Data));
                }

                return _interestingParents;
            }
        }

        private void SetHistory(History history)
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

            _sortedChildren = new ConditionalWeakTable<HistoryEvent, List<HistoryEvent>>();
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

                if (historyEvent.Children.Count > 1)
                {
                    List<HistoryEvent> sortedChildren = new List<HistoryEvent>(historyEvent.Children);
                    sortedChildren.Sort(HorizontalSort);
                }

                nodes.AddRange(historyEvent.Children);
            }
        }

        private void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
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

            InterestingParents[historyEvent] = parent.Data;

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
                InterestingParents[descendentChild.Data] = child.Data;

                parent.Children.RemoveAt(descendentChildIndex);
            }
            else
            {
                childIndex = GetChildIndex(parent, child);
            }

            parent.Children.Insert(childIndex, child);
            child.Parent = parent;
            InterestingParents[child.Data] = parent.Data;
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
                InterestingParents.Add(historyEvent, InterestingParents[parentHistoryEvent]);
                InterestingParents.Remove(parentHistoryEvent);
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

        private bool RemoveEventFromChildrenMap(HistoryEvent historyEvent)
        {
            bool somethingRemoved = _sortedChildren.Remove(historyEvent);

            return somethingRemoved;
        }

        private bool AddEventToChildrenMap(HistoryEvent historyEvent)
        {
            bool somethingAdded = false;

            //// historyEVent should have no child events!
            //if (historyEvent.Children.Any())
            //{
            //    throw new ArgumentException("This method doesn't handle events with children!", nameof(historyEvent));
            //}

            if (_sortedChildren.TryGetValue(historyEvent, out List<HistoryEvent> _))
            {
                throw new InvalidOperationException("Event already exists!");
            }

            if (historyEvent.Children.Count > 1)
            {
                List<HistoryEvent> children = new List<HistoryEvent>(historyEvent.Children);
                children.Sort(HorizontalSort);

                _sortedChildren.Add(historyEvent, children);

                somethingAdded = true;
            }

            // This may affect the parent!
            if (historyEvent.Parent != null && historyEvent.Parent.Children.Count > 1)
            {
                if (!_sortedChildren.TryGetValue(historyEvent.Parent, out List<HistoryEvent> children))
                {
                    children = new List<HistoryEvent>(historyEvent.Parent.Children);
                    children.Sort(HorizontalSort);

                    _sortedChildren.Add(historyEvent.Parent, children);

                    somethingAdded = true;
                }
                else
                {
                    // Can probably be optimized if children is already sorted!
                    children.Add(historyEvent);
                    children.Sort(HorizontalSort);
                }
            }

            return somethingAdded;
        }

        private bool UpdateChildrenMap(HistoryEvent historyEvent)
        {
            bool somethingChanged = false;

            // If historyEvent's Ticks has changed, this might affect the horizontal sorting of all ancestor events!
            List<HistoryEvent> events = new List<HistoryEvent>();

            events.Add(historyEvent);

            while (events.Any())
            {
                HistoryEvent he = events[0];
                events.RemoveAt(0);

                if (_sortedChildren.TryGetValue(he, out List<HistoryEvent> children))
                {
                    if (children.Count > 1)
                    {
                        children.Sort(HorizontalSort);
                        somethingChanged = true;
                    }
                }

                events.AddRange(he.Children);
            }

            return somethingChanged;
        }

        private List<ListTreeNode<HistoryEvent>> GenerateHorizontalOrdering()
        {
            List<ListTreeNode<HistoryEvent>> horizontalOrdering = new List<ListTreeNode<HistoryEvent>>();

            List<ListTreeNode<HistoryEvent>> events = new List<ListTreeNode<HistoryEvent>>();
            events.Add(new ListTreeNode<HistoryEvent>(_history.RootEvent));

            while (events.Any())
            {
                ListTreeNode<HistoryEvent> he = events[0];
                events.RemoveAt(0);

                ListTreeNode<HistoryEvent> parentNode = null;
                if (InterestingEvent(he.Data))
                {
                    horizontalOrdering.Add(he);
                    parentNode = he;
                }
                else
                {
                    parentNode = he.Parent;
                }


                // Add children
                if (he.Data.Children.Count == 1)
                {
                    ListTreeNode<HistoryEvent> ch = new ListTreeNode<HistoryEvent>(he.Data.Children[0]);
                    ch.Parent = parentNode;
                    events.Insert(0, ch);
                }
                else if (he.Data.Children.Count > 1)
                {
                    if (!_sortedChildren.TryGetValue(he.Data, out List<HistoryEvent> children))
                    {
                        throw new Exception("There should be a node here!");
                    }

                    //for (int c = 0; c < children.Count; c++)
                    //{

                    //}

                    events.InsertRange(0, children.Select(x => {
                        ListTreeNode<HistoryEvent> ch = new ListTreeNode<HistoryEvent>(x);
                        ch.Parent = parentNode;
                        return ch;
                    }));
                }
            }

            return horizontalOrdering;
        }

        private PositionChangedEventArgs<HistoryEvent> UpdateListTree(HistoryChangedEventArgs args)
        {
            bool changed = false;

            switch (args.Action)
            {
                case HistoryChangedAction.Add:
                    {
                        changed = AddEventToListTree(args.HistoryEvent);

                        AddEventToChildrenMap(args.HistoryEvent);
                    }
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                        changed = Update(node);

                        UpdateChildrenMap(args.HistoryEvent);
                    }
                    break;
                case HistoryChangedAction.DeleteBranch:
                    {
                        RemoveEventFromChildrenMap(args.HistoryEvent);
                        if (args.OriginalParentEvent.Children.Count <= 1)
                        {
                            RemoveEventFromChildrenMap(args.OriginalParentEvent);
                        }

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

                        RemoveEventFromChildrenMap(args.HistoryEvent);
                    }
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    {
                        RemoveEventFromChildrenMap(args.HistoryEvent);
                        if (args.OriginalParentEvent.Children.Count <= 1)
                        {
                            RemoveEventFromChildrenMap(args.OriginalParentEvent);
                        }

                        ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                        // Special case
                        ListTreeNode<HistoryEvent> originalParentNode = GetNode(args.OriginalParentEvent);
                        bool interestingParent = InterestingEvent(args.OriginalParentEvent);
                        if (originalParentNode == null && interestingParent)
                        {
                            // Just swap out the data!

                            // Probably better to just walk up the tree to find the nearest interesting event!
                            InterestingParents[args.OriginalParentEvent] = InterestingParents[node.Data];

                            node.Data = args.OriginalParentEvent;
                            changed = true;

                            if (args.OriginalParentEvent.Children.Count > 1)
                            {
                                List<HistoryEvent> children = new List<HistoryEvent>(args.OriginalParentEvent.Children);
                                children.Sort(HorizontalSort);

                                _sortedChildren.Add(args.OriginalParentEvent, children);

                                //somethingAdded = true;
                            }

                            foreach (HistoryEvent ce in args.OriginalParentEvent.Children)
                            {
                                if (InterestingEvent(ce))
                                {
                                    InterestingParents[ce] = args.OriginalParentEvent;
                                }
                                else
                                {
                                    InterestingParents.Remove(ce);
                                }
                            }

                            InterestingParents.Remove(args.HistoryEvent);
                        }
                        else if (originalParentNode == null && !interestingParent)
                        {
                            // Go up the tree until we find an event with a node! Then add the deleted nodes' children to that node.
                            //throw new NotImplementedException();
                            //ListTreeNode<HistoryEvent> parentNode = node.Parent;
                            originalParentNode = node.Parent;

                            // Copied from below!
                            originalParentNode.Children.Remove(node);
                            _horizontalNodes.Remove(node);

                            // Just move the children of the deleted node over to originalParentNode!
                            for (int c = 0; c < node.Children.Count; c++)
                            {
                                _horizontalNodes.Remove(node.Children[c]);
                            }

                            RefreshHorizontalPositions(0);
                            RefreshVerticalPositions(0);

                            for (int c = 0; c < node.Children.Count; c++)
                            {
                                MoveNode(node.Children[c], originalParentNode);
                            }

                            _verticalNodes.Remove(node);
                            _eventsToNodes.Remove(node.Data);

                            RefreshHorizontalPositions(0);
                            RefreshVerticalPositions(0);

                            changed = true;
                        }
                        else if (originalParentNode != null && interestingParent)
                        {
                            originalParentNode.Children.Remove(node);
                            _horizontalNodes.Remove(node);

                            // Just move the children of the deleted node over to originalParentNode!
                            for (int c = 0; c < node.Children.Count; c++)
                            {
                                _horizontalNodes.Remove(node.Children[c]);
                            }

                            RefreshHorizontalPositions(0);
                            RefreshVerticalPositions(0);

                            for (int c = 0; c < node.Children.Count; c++)
                            {
                                MoveNode(node.Children[c], originalParentNode);
                            }

                            _verticalNodes.Remove(node);
                            _eventsToNodes.Remove(node.Data);

                            RefreshHorizontalPositions(0);
                            RefreshVerticalPositions(0);

                            changed = true;
                        }
                        else
                        {
                            //throw new NotImplementedException();

                            // Original parent node exists but is no longer interesting. Could happen if the parent node had 2 children, but now
                            // only has one as a result of this bookmark node being deleted. Need to remove the parent node and add its remaining
                            // children to its parent.
                            originalParentNode.Children.Remove(node);
                            node.Parent = null;
                            InterestingParents.Remove(node.Data); // [descendentChild.Data] = child.Data;

                            _horizontalNodes.Remove(node);
                            _verticalNodes.Remove(node);
                            _eventsToNodes.Remove(node.Data);

                            // Now move all of the parent (now not interesting!) to its parent.

                            // Go up the tree until we find an event with a node! Then add the deleted nodes' children to that node.
                            //throw new NotImplementedException();
                            //ListTreeNode<HistoryEvent> parentNode = node.Parent;
                            node = originalParentNode;
                            originalParentNode = originalParentNode.Parent;

                            // Copied from below!
                            originalParentNode.Children.Remove(node);
                            _horizontalNodes.Remove(node);

                            // Just move the children of the deleted node over to originalParentNode!
                            for (int c = 0; c < node.Children.Count; c++)
                            {
                                _horizontalNodes.Remove(node.Children[c]);
                            }

                            RefreshHorizontalPositions(0);
                            RefreshVerticalPositions(0);

                            for (int c = 0; c < node.Children.Count; c++)
                            {
                                MoveNode(node.Children[c], originalParentNode);
                            }

                            _verticalNodes.Remove(node);
                            _eventsToNodes.Remove(node.Data);

                            RefreshHorizontalPositions(0);
                            RefreshVerticalPositions(0);

                            changed = true;
                        }
                    }
                    break;
            }

            if (changed)
            {
                // Do our new ordering!
                List<ListTreeNode<HistoryEvent>> newOrdering = GenerateHorizontalOrdering();

                // Check our parent map thingy!
                foreach (ListTreeNode<HistoryEvent> node in HorizontalOrdering())
                {
                    if (node.Parent != null)
                    {
                        if (!InterestingParents.ContainsKey(node.Data) || InterestingParents[node.Data] != node.Parent.Data)
                        {
                            CPvC.Diagnostics.Trace("Whoops!");
                        }
                    }
                    else
                    {
                        if (InterestingParents.ContainsKey(node.Data))
                        {
                            CPvC.Diagnostics.Trace("Whoops 2!");
                        }
                    }
                }

                //Dictionary<ListTreeNode<HistoryEvent>, ListTreeOrderingEvent<HistoryEvent>> oldMap = new Dictionary<ListTreeNode<HistoryEvent>, ListTreeOrderingEvent<HistoryEvent>>();

                //List<ListTreeOrderingEvent<HistoryEvent>> oldOrdering = GetNewStyleOrdering(HorizontalOrdering());

                //List<ListTreeOrderingEvent<HistoryEvent>> oldOrdering = HorizontalOrdering().Select(x => new ListTreeOrderingEvent<HistoryEvent>(x.Data, x.Parent);
                PositionChangedEventArgs<HistoryEvent> changeArgs = new PositionChangedEventArgs<HistoryEvent>(HorizontalOrdering(), VerticalOrdering(), InterestingParents);
                //PositionChangedEventArgs<HistoryEvent> changeArgs = new PositionChangedEventArgs<HistoryEvent>(oldOrdering, VerticalOrdering());

                return changeArgs;
            }

            return null;
        }

        public List<ListTreeOrderingEvent<HistoryEvent>> GetNewStyleOrdering(List<ListTreeNode<HistoryEvent>> ordering)
        {
            Dictionary<ListTreeNode<HistoryEvent>, ListTreeOrderingEvent<HistoryEvent>> oldMap = new Dictionary<ListTreeNode<HistoryEvent>, ListTreeOrderingEvent<HistoryEvent>>();

            List<ListTreeOrderingEvent<HistoryEvent>> oldOrdering = new List<ListTreeOrderingEvent<HistoryEvent>>();
            foreach (ListTreeNode<HistoryEvent> node in ordering)
            {
                ListTreeOrderingEvent<HistoryEvent> ordEvent = new ListTreeOrderingEvent<HistoryEvent>(node.Data, null);
                oldMap.Add(node, ordEvent);

                oldOrdering.Add(ordEvent);
            }

            foreach (ListTreeNode<HistoryEvent> node in oldMap.Keys)
            {
                if (node.Parent != null)
                {
                    oldMap[node].Parent = oldMap[node.Parent];
                }
            }

            return oldOrdering;
        }

        private HistoryEvent GetInterestingParent(HistoryEvent historyEvent)
        {
            HistoryEvent interestingParent = historyEvent.Parent;
            while (interestingParent != null && !InterestingEvent(interestingParent))
            {
                interestingParent = interestingParent.Parent;
            }

            return interestingParent;
        }

        private void MoveNode(ListTreeNode<HistoryEvent> node, ListTreeNode<HistoryEvent> newParentNode)
        {
            //ListTreeNode<HistoryEvent> childNode = node.Children[c];
            int newIndex = GetChildIndex(newParentNode, node);
            newParentNode.Children.Insert(newIndex, node);
            node.Parent = newParentNode;
            InterestingParents[node.Data] = newParentNode.Data;

            if (newParentNode.Data.Children.Count > 1)
            {
                List<HistoryEvent> children = new List<HistoryEvent>(newParentNode.Data.Children);
                children.Sort(HorizontalSort);

                if (_sortedChildren.TryGetValue(newParentNode.Data, out _))
                {
                    _sortedChildren.Remove(newParentNode.Data);
                }

                _sortedChildren.Add(newParentNode.Data, children);
            }


            int horizontalIndex = GetHorizontalInsertionIndex(newParentNode, newIndex);
            _horizontalNodes.Insert(horizontalIndex, node);

            RefreshHorizontalPositions(0);
            RefreshVerticalPositions(0);
        }

        //private ListTreeNode<HistoryEvent> CreateNode(ListTreeNode<HistoryEvent> parentNode, HistoryEvent childEvent)
        //{
        //    // Child node is assumed to be interesting!
        //    ListTreeNode<HistoryEvent> node = new ListTreeNode<HistoryEvent>(childEvent);

        //    int childIndex = GetChildIndex(parentNode, node);
        //    parentNode.Children.Insert(childIndex, node);
        //    node.Parent = parentNode;
        //    int horizontalIndex = GetHorizontalInsertionIndex(parentNode, childIndex);
        //    _horizontalNodes.Insert(horizontalIndex, node);
        //    int verticalIndex = GetVerticalIndex(node);
        //    _verticalNodes.Insert(verticalIndex, node);
        //    _eventsToNodes.Add(childEvent, node);

        //    return node;
        //}

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

        private ConditionalWeakTable<HistoryEvent, List<HistoryEvent>> _sortedChildren;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParents;

        public event NotifyPositionChangedEventHandler<HistoryEvent> PositionChanged;
    }
}
