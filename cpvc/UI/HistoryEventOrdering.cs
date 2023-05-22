using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC
{
    public class HistoryEventOrdering
    {
        public HistoryEventOrdering(History history)
        {
            _sortedChildren = new Dictionary<HistoryEvent, List<HistoryEvent>>();

            _interestingParents = new Dictionary<HistoryEvent, HistoryEvent>();
            _verticalEvents = new SortedVerticalHistoryEventList();
            _descendentWithMaxTicks = new Dictionary<HistoryEvent, HistoryEvent>();

            SetHistory(history);
        }

        public Dictionary<HistoryEvent, HistoryEvent> InterestingParents
        {
            get
            {
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
        }

        private void Init()
        {
            // Add the vertical nodes!
            List<HistoryEvent> allNodes = new List<HistoryEvent>();
            allNodes.Add(_history.RootEvent);

            for (int c = 0; c < allNodes.Count; c++)
            {
                InitChildren(allNodes[c]);

                _verticalEvents.Add(allNodes[c]);

                HistoryEvent interestingParent = GetInterestingParent(allNodes[c].Parent);
                if (interestingParent != null)
                {
                    _interestingParents.Add(allNodes[c], interestingParent);
                }

                allNodes.AddRange(allNodes[c].Children);
            }

            GetDescendentWithMaxTicks(_history.RootEvent);
        }

        private void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {
            if (Update(args))
            {
                OrderingChanged?.Invoke(this, new PositionChangedEventArgs<HistoryEvent>(HorizontalOrdering, VerticalOrdering, InterestingParents));
            }
        }

        private bool InterestingEvent(HistoryEvent historyEvent)
        {
            if (historyEvent is RootHistoryEvent ||
                historyEvent is BookmarkHistoryEvent ||
                historyEvent == _history.CurrentEvent ||
                historyEvent.Children.Count != 1)
            {
                return true;
            }

            return false;
        }

        public List<HistoryEvent> VerticalOrdering
        {
            get
            {
                return _verticalEvents.GetEvents().Where(historyEvent => InterestingEvent(historyEvent)).ToList();
            }
        }

        public List<HistoryEvent> HorizontalOrdering
        {
            get
            {
                List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();

                List<HistoryEvent> events = new List<HistoryEvent>();
                events.Add(_history.RootEvent);

                while (events.Count > 0)
                {
                    HistoryEvent he = events[0];
                    events.RemoveAt(0);

                    if (InterestingEvent(he))
                    {
                        horizontalEvents.Add(he);
                    }

                    // Add children
                    if (he.Children.Count == 1)
                    {
                        HistoryEvent ch = he.Children[0];
                        events.Insert(0, ch);
                    }
                    else if (he.Children.Count > 1)
                    {
                        if (!_sortedChildren.TryGetValue(he, out List<HistoryEvent> children))
                        {
                            throw new Exception("History event should exist in sorted children dictionary!");
                        }

                        events.InsertRange(0, children);
                    }
                }

                return horizontalEvents;
            }
        }

        private void DeleteBranch(HistoryEvent historyEvent, HistoryEvent originalParentEvent)
        {
            List<HistoryEvent> descendents = new List<HistoryEvent>();

            descendents.Add(historyEvent);

            while (descendents.Count > 0)
            {
                HistoryEvent he = descendents[0];
                descendents.RemoveAt(0);

                _sortedChildren.Remove(he);
                _interestingParents.Remove(he);

                descendents.AddRange(he.Children);
            }

            if (_sortedChildren.TryGetValue(originalParentEvent, out List<HistoryEvent> sortedChildren))
            {
                sortedChildren.Remove(historyEvent);
            }
        }

        private bool HorizontallySorted(HistoryEvent first, HistoryEvent second)
        {
            return HorizontalSort(first, second) < 0;
        }

        static private bool VerticallySorted(HistoryEvent first, HistoryEvent second)
        {
            return VerticalSort(first, second) < 0;
        }

        private bool UpdateHistoryTicks(HistoryEvent historyEvent)
        {
            if (historyEvent.Parent == null)
            {
                return false;
            }

            if (!_sortedChildren.TryGetValue(historyEvent.Parent, out List<HistoryEvent> sortedChildren))
            {
                return false;
            }

            int index = sortedChildren.IndexOf(historyEvent);
            if (index > 0)
            {
                // Go left?
                if (HorizontallySorted(historyEvent, sortedChildren[index - 1]))
                {
                    // Yes!
                    int originalIndex = index;
                    index--;
                    while (index > 0)
                    {
                        if (!HorizontallySorted(historyEvent, sortedChildren[index - 1]))
                        {
                            break;
                        }

                        index--;
                    }

                    sortedChildren.RemoveAt(originalIndex);
                    sortedChildren.Insert(index, historyEvent);

                    return true;
                }
            }
            else if ((index + 1) < sortedChildren.Count)
            {
                // Go right?
                if (!HorizontallySorted(historyEvent, sortedChildren[index + 1]))
                {
                    // Yes!
                    int originalIndex = index;
                    index--;
                    while ((index + 1) < sortedChildren.Count)
                    {
                        if (HorizontallySorted(historyEvent, sortedChildren[index + 1]))
                        {
                            break;
                        }

                        index++;
                    }

                    sortedChildren.RemoveAt(originalIndex);
                    sortedChildren.Insert(index, historyEvent);

                    return true;
                }
            }

            return false;
        }

        private void DeleteBookmark(HistoryEvent historyEvent, HistoryEvent originalParentEvent, List<HistoryEvent> originalChildrenEvents)
        {
            _interestingParents.Remove(historyEvent);
            UpdateInterestingParent(originalParentEvent);

            foreach (HistoryEvent child in originalChildrenEvents)
            {
                UpdateInterestingParent(child);
            }

            if (originalParentEvent.Children.Count <= 1)
            {
                _sortedChildren.Remove(originalParentEvent);
            }
            else
            {
                if (!_sortedChildren.TryGetValue(originalParentEvent, out List<HistoryEvent> sortedChildren))
                {
                    sortedChildren = new List<HistoryEvent>(originalParentEvent.Children);
                    _sortedChildren.Add(originalParentEvent, sortedChildren);
                }

                sortedChildren.Sort(HorizontalSort);
            }
        }

        private void InitChildren(HistoryEvent historyEvent)
        {
            if (historyEvent.Children.Count <= 1)
            {
                return;
            }

            List<HistoryEvent> sortedChildren = new List<HistoryEvent>(historyEvent.Children);
            _sortedChildren.Add(historyEvent, sortedChildren);
            sortedChildren.Sort(HorizontalSort);
        }

        private void AddEventToChildren(HistoryEvent historyEvent)
        {
            if (historyEvent.Parent == null)
            {
                return;
            }

            if (historyEvent.Parent.Children.Count <= 1)
            {
                _sortedChildren.Remove(historyEvent.Parent);

                return;
            }

            if (!_sortedChildren.TryGetValue(historyEvent.Parent, out List<HistoryEvent> sortedChildren))
            {
                sortedChildren = new List<HistoryEvent>(historyEvent.Parent.Children);
                _sortedChildren.Add(historyEvent.Parent, sortedChildren);
                sortedChildren.Sort(HorizontalSort);
            }
            else
            {
                // Assume that the children are sorted! Just insert at the appropriate place!
                int newChildIndex = 0;
                while (newChildIndex < sortedChildren.Count)
                {
                    if (HorizontallySorted(historyEvent, sortedChildren[newChildIndex]))
                    {
                        break;
                    }

                    newChildIndex++;
                }

                sortedChildren.Insert(newChildIndex, historyEvent);
            }
        }

        private HistoryEvent GetInterestingParent(HistoryEvent parentHistoryEvent)
        {
            while (parentHistoryEvent != null && !InterestingEvent(parentHistoryEvent))
            {
                parentHistoryEvent = parentHistoryEvent.Parent;
            }

            return parentHistoryEvent;
        }

        private bool UpdateInterestingParent(HistoryEvent historyEvent)
        {
            void SetInterestingParentForInterestingDescendents(List<HistoryEvent> historyEvents, HistoryEvent newParentEvent)
            {
                List<HistoryEvent> interestingChildren = new List<HistoryEvent>(historyEvents);
                while (interestingChildren.Count > 0)
                {
                    HistoryEvent he = interestingChildren[0];
                    interestingChildren.RemoveAt(0);

                    if (InterestingEvent(he))
                    {
                        // We are now the interesting parent!
                        _interestingParents[he] = newParentEvent;
                    }
                    else
                    {
                        interestingChildren.AddRange(he.Children);
                    }
                }
            }

            // Really need to check if historyEvent is part of our tree!
            // If not, remove it!

            bool isInteresting = InterestingEvent(historyEvent);
            bool wasInteresting = _interestingParents.ContainsKey(historyEvent);

            if (isInteresting && !wasInteresting)
            {
                SetInterestingParentForInterestingDescendents(historyEvent.Children, historyEvent);

                // Find our parent!
                HistoryEvent ourParent = GetInterestingParent(historyEvent.Parent);
                _interestingParents[historyEvent] = ourParent;

                return true;
            }
            else if (!isInteresting && wasInteresting)
            {
                HistoryEvent ourInterestingParent = _interestingParents[historyEvent];

                SetInterestingParentForInterestingDescendents(historyEvent.Children, ourInterestingParent);

                _interestingParents.Remove(historyEvent);

                return true;
            }

            return false;
        }

        private HistoryEvent GetDescendentWithMaxTicks(HistoryEvent historyEvent)
        {
            if (_descendentWithMaxTicks.TryGetValue(historyEvent, out HistoryEvent max))
            {
                return max;
            }

            List<HistoryEvent> descendents = new List<HistoryEvent>();
            descendents.Add(historyEvent);

            for (int d = 0; d < descendents.Count; d++)
            {
                descendents.AddRange(descendents[d].Children.Where(child => !_descendentWithMaxTicks.ContainsKey(child)));
            }

            for (int d = descendents.Count - 1; d >= 0; d--)
            {
                HistoryEvent he = descendents[d];

                if (he.Children.Count == 0)
                {
                    _descendentWithMaxTicks[he] = he;
                }
                else
                {
                    HistoryEvent maxChild = he.Children[0];
                    for (int i = 1; i < he.Children.Count; i++)
                    {
                        if (HorizontallySorted(_descendentWithMaxTicks[he.Children[i]], _descendentWithMaxTicks[maxChild]))
                        {
                            maxChild = he.Children[i];
                        }
                    }

                    _descendentWithMaxTicks[he] = _descendentWithMaxTicks[maxChild];
                }
            }

            return _descendentWithMaxTicks[historyEvent];
        }

        private void UpdateAdd(HistoryEvent historyEvent)
        {
            _verticalEvents.Add(historyEvent);

            // 
            AddEventToChildren(historyEvent);
            UpdateInterestingParent(historyEvent);
            if (historyEvent.Parent != null)
            {
                UpdateInterestingParent(historyEvent.Parent);
            }

            HistoryEvent ancestor = historyEvent.Parent;
            while (ancestor != null)
            {
                UpdateHistoryTicks(ancestor);
                ancestor = ancestor.Parent;
            }
        }

        private void InvalidateMax(HistoryEvent historyEvent)
        {
            while (historyEvent != null)
            {
                // Assume that if "historyEvent" is already invalidated, then so are its ancestors.
                if (!_descendentWithMaxTicks.ContainsKey(historyEvent))
                {
                    break;
                }

                _descendentWithMaxTicks.Remove(historyEvent);

                historyEvent = historyEvent.Parent;
            }
        }

        private bool Update(HistoryChangedEventArgs args)
        {
            bool changed = false;

            switch (args.Action)
            {
                case HistoryChangedAction.Add:
                    {
                        changed = true;

                        UpdateAdd(args.HistoryEvent);

                        InvalidateMax(args.HistoryEvent.Parent);
                    }
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        changed = _verticalEvents.FixOrder(args.HistoryEvent);
                        bool verticalChanged = changed;

                        changed |= UpdateHistoryTicks(args.HistoryEvent);

                        InvalidateMax(args.HistoryEvent);

                        if (verticalChanged)
                        {
                            HistoryEvent ancestor = args.HistoryEvent.Parent;
                            while (ancestor != null)
                            {
                                UpdateHistoryTicks(ancestor);
                                ancestor = ancestor.Parent;
                            }
                        }
                    }
                    break;
                case HistoryChangedAction.DeleteBranch:
                    {
                        changed = true;

                        List<HistoryEvent> childEvents = new List<HistoryEvent>();
                        childEvents.Add(args.HistoryEvent);

                        while (childEvents.Count > 0)
                        {
                            HistoryEvent he = childEvents[0];
                            childEvents.RemoveAt(0);

                            _verticalEvents.Remove(he);

                            childEvents.AddRange(he.Children);
                        }

                        DeleteBranch(args.HistoryEvent, args.OriginalParentEvent);
                        UpdateInterestingParent(args.OriginalParentEvent);
                        InvalidateMax(args.OriginalParentEvent);
                    }
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    {
                        changed = true;

                        _verticalEvents.Remove(args.HistoryEvent);

                        DeleteBookmark(args.HistoryEvent, args.OriginalParentEvent, args.OriginalChildrenEvents);

                        // Find out who the interesting parent of all the moved children should be...
                        if (args.OriginalChildrenEvents.Count > 0)
                        {
                            HistoryEvent interestingParentEvent = GetInterestingParent(args.OriginalParentEvent);

                            List<HistoryEvent> interestingChildren = new List<HistoryEvent>(args.OriginalChildrenEvents);

                            while (interestingChildren.Count > 0)
                            {
                                HistoryEvent he = interestingChildren[0];
                                interestingChildren.RemoveAt(0);

                                if (InterestingEvent(he))
                                {
                                    _interestingParents[he] = interestingParentEvent;
                                }
                                else
                                {
                                    interestingChildren.AddRange(he.Children);
                                }
                            }
                        }

                        if (args.OriginalChildrenEvents.Count == 0)
                        {
                            InvalidateMax(args.OriginalParentEvent);
                        }
                    }
                    break;
            }

            return changed;
        }

        protected int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            HistoryEvent yMax = GetDescendentWithMaxTicks(y);
            HistoryEvent xMax = GetDescendentWithMaxTicks(x);

            return VerticalSort(yMax, xMax);
        }

        static private int VerticalSort(HistoryEvent x, HistoryEvent y)
        {
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
                    // A HistoryEvent shouldn't be compared with itself.
                    throw new InvalidOperationException("HistoryEvent was compared with itself.");
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

            return x.Id.CompareTo(y.Id);
        }

        private History _history;

        private SortedVerticalHistoryEventList _verticalEvents;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParents;
        private Dictionary<HistoryEvent, List<HistoryEvent>> _sortedChildren;
        private Dictionary<HistoryEvent, HistoryEvent> _descendentWithMaxTicks;

        public event NotifyPositionChangedEventHandler<HistoryEvent> OrderingChanged;

        private class SortedVerticalHistoryEventList
        {
            public SortedVerticalHistoryEventList()
            {
                _historyEvents = new List<HistoryEvent>();
                _historyEventIndices = new Dictionary<HistoryEvent, int>();
            }

            public bool Add(HistoryEvent historyEvent)
            {
                if (Contains(historyEvent))
                {
                    return false;
                }

                // The list is sorted, so do a binary search
                int binarySearchInsertionIndexLower = 0;
                int binarySearchInsertionIndexUpper = _historyEvents.Count;

                while (binarySearchInsertionIndexLower < binarySearchInsertionIndexUpper)
                {
                    int index = (binarySearchInsertionIndexUpper + binarySearchInsertionIndexLower) / 2;

                    if (VerticallySorted(historyEvent, _historyEvents[index]))
                    {
                        binarySearchInsertionIndexUpper = index;
                    }
                    else
                    {
                        binarySearchInsertionIndexLower = index + 1;
                    }
                }

                Insert(binarySearchInsertionIndexUpper, historyEvent);

                return true;
            }

            public bool FixOrder(HistoryEvent historyEvent)
            {
                int verticalIndex = IndexOf(historyEvent);

                bool CheckOrder()
                {
                    if (verticalIndex > 0)
                    {
                        if (!VerticallySorted(_historyEvents[verticalIndex - 1], historyEvent))
                        {
                            return true;
                        }
                    }

                    if (verticalIndex + 1 < _historyEvents.Count)
                    {
                        if (!VerticallySorted(historyEvent, _historyEvents[verticalIndex + 1]))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (CheckOrder())
                {
                    RemoveAt(verticalIndex);

                    int newVerticalIndex = 0;
                    while (newVerticalIndex < _historyEvents.Count)
                    {
                        if (VerticallySorted(historyEvent, _historyEvents[newVerticalIndex]))
                        {
                            break;
                        }

                        newVerticalIndex++;
                    }

                    Insert(newVerticalIndex, historyEvent);

                    return true;
                }

                return false;
            }

            public List<HistoryEvent> GetEvents()
            {
                return _historyEvents;
            }

            private void Insert(int index, HistoryEvent historyEvent)
            {
                _historyEvents.Insert(index, historyEvent);
                _historyEventIndices[historyEvent] = index;

                for (int i = index + 1; i < _historyEvents.Count; i++)
                {
                    _historyEventIndices[_historyEvents[i]] = i;
                }
            }

            public bool Remove(HistoryEvent historyEvent)
            {
                int index = _historyEventIndices[historyEvent];
                RemoveAt(index);

                return true;
            }

            private void RemoveAt(int index)
            {
                HistoryEvent historyEvent = _historyEvents[index];
                _historyEvents.RemoveAt(index);
                _historyEventIndices.Remove(historyEvent);

                for (int i = index; i < _historyEvents.Count; i++)
                {
                    _historyEventIndices[_historyEvents[i]] = i;
                }
            }

            private bool Contains(HistoryEvent historyEvent)
            {
                return _historyEventIndices.ContainsKey(historyEvent);
            }

            private int IndexOf(HistoryEvent historyEvent)
            {
                return _historyEventIndices[historyEvent];
            }

            private List<HistoryEvent> _historyEvents;
            private Dictionary<HistoryEvent, int> _historyEventIndices;
        }
    }
}
