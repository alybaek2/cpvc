﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class HistoryEventOrdering
    {
        public HistoryEventOrdering(History history)
        {
            //_sortedChildren = new ConditionalWeakTable<HistoryEvent, List<HistoryEvent>>();
            _sortedChildren2 = new Dictionary<HistoryEvent, List<HistoryEvent>>();

            _interestingParents = new Dictionary<HistoryEvent, HistoryEvent>();
            _interestingParents2 = new Dictionary<HistoryEvent, HistoryEvent>();
            _verticalEvents = new SortedVerticalHistoryEventList();

            SetHistory(history);
        }

        public Dictionary<HistoryEvent, HistoryEvent> InterestingParents
        {
            get
            {
                _interestingParents.Clear();

                List<HistoryEvent> allEvents = VerticalOrdering;

                foreach (HistoryEvent historyEvent in allEvents)
                {
                    if (historyEvent.Parent != null)
                    {
                        _interestingParents.Add(historyEvent, GetInterestingParent(historyEvent));
                    }
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
        }

        private void Init()
        {
            // Add the vertical nodes!
            List<HistoryEvent> allNodes = new List<HistoryEvent>();
            allNodes.Add(_history.RootEvent);

            for (int c = 0; c < allNodes.Count; c++)
            {
                //RefreshSortedChildrenMap(allNodes[c]);
                InitChildren2(allNodes[c]);

                _verticalEvents.Add(allNodes[c]);

                allNodes.AddRange(allNodes[c].Children);
            }
        }

        private void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {
            if (Update(args))
            {
                // Test!
                //List<HistoryEvent> horizontal = HorizontalOrdering;
                List<HistoryEvent> horizontal2 = HorizontalOrdering2;

                //if (horizontal.Count != horizontal2.Count)
                //{
                //    throw new Exception("oops!");
                //}

                //for (int i = 0; i < horizontal.Count; i++)
                //{
                //    if (horizontal[i] != horizontal2[i])
                //    {
                //        throw new Exception("oopsies!");
                //    }
                //}

                Dictionary<HistoryEvent, HistoryEvent> interestingParents = InterestingParents;

                // Check!
                if (interestingParents.Keys.Count != _interestingParents2.Keys.Count)
                {
                    throw new Exception("Oh no!");
                }

                foreach (HistoryEvent he in interestingParents.Keys)
                {
                    if (!_interestingParents2.ContainsKey(he))
                    {
                        throw new Exception("Noooo!");
                    }
                    if (interestingParents[he] != _interestingParents2[he])
                    {
                        throw new Exception("Blargh!");
                    }
                }

                OrderingChanged?.Invoke(this, new PositionChangedEventArgs<HistoryEvent>(HorizontalOrdering2, VerticalOrdering, interestingParents));
            }
        }

        static public bool InterestingEvent(HistoryEvent historyEvent)
        {
            if (historyEvent is RootHistoryEvent ||
                historyEvent is BookmarkHistoryEvent ||
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
                return _verticalEvents.GetEvents();
            }
        }

        //public List<HistoryEvent> HorizontalOrdering
        //{
        //    get
        //    {
        //        List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();

        //        List<HistoryEvent> events = new List<HistoryEvent>();
        //        events.Add(_history.RootEvent);

        //        while (events.Any())
        //        {
        //            HistoryEvent he = events[0];
        //            events.RemoveAt(0);

        //            if (InterestingEvent(he))
        //            {
        //                horizontalEvents.Add(he);
        //            }

        //            // Add children
        //            if (he.Children.Count == 1)
        //            {
        //                HistoryEvent ch = he.Children[0];
        //                events.Insert(0, ch);
        //            }
        //            else if (he.Children.Count > 1)
        //            {
        //                if (!_sortedChildren.TryGetValue(he, out List<HistoryEvent> children))
        //                {
        //                    throw new Exception("There should be a node here!");
        //                }

        //                events.InsertRange(0, children);
        //            }
        //        }

        //        return horizontalEvents;
        //    }
        //}

        public List<HistoryEvent> HorizontalOrdering2
        {
            get
            {
                List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();

                List<HistoryEvent> events = new List<HistoryEvent>();
                events.Add(_history.RootEvent);

                while (events.Any())
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
                        if (!_sortedChildren2.TryGetValue(he, out List<HistoryEvent> children))
                        {
                            throw new Exception("There should be a node here!");
                        }

                        events.InsertRange(0, children);
                    }
                }

                return horizontalEvents;
            }
        }

        //private bool RefreshAncestorChildrenMaps(HistoryEvent historyEvent)
        //{
        //    bool result = false;
        //    HistoryEvent parentEvent = historyEvent.Parent;

        //    while (parentEvent != null)
        //    {
        //        result |= RefreshSortedChildrenMap(parentEvent);

        //        parentEvent = parentEvent.Parent;
        //    }

        //    return result;
        //}

        //private bool RefreshSortedChildrenMap(HistoryEvent historyEvent)
        //{
        //    if (historyEvent.Children.Count <= 1)
        //    {
        //        if (_sortedChildren.TryGetValue(historyEvent, out _))
        //        {
        //            _sortedChildren.Remove(historyEvent);
        //        }

        //        return false;
        //    }

        //    // We should have something in this map!
        //    if (_sortedChildren.TryGetValue(historyEvent, out List<HistoryEvent> children))
        //    {
        //        foreach (HistoryEvent ce in historyEvent.Children)
        //        {
        //            if (!children.Contains(ce))
        //            {
        //                children.Add(ce);
        //            }
        //        }

        //        for (int c = children.Count - 1; c >= 0; c--)
        //        {
        //            if (!historyEvent.Children.Contains(children[c]))
        //            {
        //                children.RemoveAt(c);
        //            }
        //        }

        //        children.Sort(HorizontalSort);
        //    }
        //    else
        //    {
        //        children = new List<HistoryEvent>(historyEvent.Children);
        //        children.Sort(HorizontalSort);

        //        _sortedChildren.Add(historyEvent, children);
        //    }

        //    return true;
        //}

        private void DeleteBranch2(HistoryEvent historyEvent, HistoryEvent originalParentEvent)
        {
            List<HistoryEvent> descendents = new List<HistoryEvent>();

            descendents.Add(historyEvent);

            while (descendents.Any())
            {
                HistoryEvent he = descendents[0];
                descendents.RemoveAt(0);

                _sortedChildren2.Remove(he);
                _interestingParents2.Remove(he);

                descendents.AddRange(he.Children);
            }

            if (_sortedChildren2.TryGetValue(originalParentEvent, out List<HistoryEvent> sortedChildren))
            {
                sortedChildren.Remove(originalParentEvent);
            }
        }

        private bool UpdateHistoryTicks2(HistoryEvent historyEvent)
        {
            if (historyEvent.Parent == null)
            {
                return false;
            }

            if (!_sortedChildren2.TryGetValue(historyEvent.Parent, out List<HistoryEvent> sortedChildren))
            {
                return false;
            }

            int index = sortedChildren.IndexOf(historyEvent);

            if (index > 0)
            {
                // Go left?
                if (HorizontalSort(historyEvent, sortedChildren[index - 1]) < 0)
                {
                    // Yes!
                    int originalIndex = index;
                    index--;
                    while (index > 0)
                    {
                        if (HorizontalSort(historyEvent, sortedChildren[index - 1]) > 0)
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
                if (HorizontalSort(historyEvent, sortedChildren[index + 1]) > 0)
                {
                    // Yes!
                    int originalIndex = index;
                    index--;
                    while ((index + 1) < sortedChildren.Count)
                    {
                        if (HorizontalSort(historyEvent, sortedChildren[index + 1]) < 0)
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

        private void DeleteBookmark2(HistoryEvent historyEvent, HistoryEvent originalParentEvent, List<HistoryEvent> originalChildrenEvents)
        {
            foreach (HistoryEvent child in originalChildrenEvents)
            {
                UpdateInterestingParent2(child);
            }

            if (originalParentEvent.Children.Count <= 1)
            {
                _sortedChildren2.Remove(originalParentEvent);

                _interestingParents2.Remove(historyEvent);
                //UpdateInterestingParent2(historyEvent);
                UpdateInterestingParent2(originalParentEvent);

                foreach (HistoryEvent child in originalChildrenEvents)
                {
                    UpdateInterestingParent2(child);
                }

                return;
            }

            // The original parent will have had historyEvent's children added to it, so just re-sort all the children!
            if (!_sortedChildren2.TryGetValue(originalParentEvent, out List<HistoryEvent> sortedChildren))
            {
                sortedChildren = new List<HistoryEvent>(originalParentEvent.Children);
                _sortedChildren2.Add(originalParentEvent, sortedChildren);
                sortedChildren.Sort(HorizontalSort);
            }
            else
            {
                sortedChildren.Sort(HorizontalSort);
            }

            UpdateInterestingParent2(historyEvent);
            UpdateInterestingParent2(originalParentEvent);
            _interestingParents2.Remove(historyEvent);

            foreach (HistoryEvent child in originalChildrenEvents)
            {
                UpdateInterestingParent2(child);
            }
        }

        private void InitChildren2(HistoryEvent historyEvent)
        {
            if (historyEvent.Children.Count <= 1)
            {
                return;
            }

            List<HistoryEvent> sortedChildren = new List<HistoryEvent>(historyEvent.Children);
            _sortedChildren2.Add(historyEvent, sortedChildren);
            sortedChildren.Sort(HorizontalSort);
        }

        private void AddEventToChildren2(HistoryEvent historyEvent)
        {
            if (historyEvent.Parent == null)
            {
                return;
            }

            if (historyEvent.Parent.Children.Count <= 1)
            {
                _sortedChildren2.Remove(historyEvent.Parent);

                return;
            }

            if (!_sortedChildren2.TryGetValue(historyEvent.Parent, out List<HistoryEvent> sortedChildren))
            {
                sortedChildren = new List<HistoryEvent>(historyEvent.Parent.Children);
                _sortedChildren2.Add(historyEvent.Parent, sortedChildren);
                sortedChildren.Sort(HorizontalSort);
            }
            else
            {
                // Assume that the children are sorted! Just insert at the appropriate place!
                int newChildIndex = 0;
                while (newChildIndex < sortedChildren.Count)
                {
                    if (HorizontalSort(historyEvent, sortedChildren[newChildIndex]) <= 0)
                    {
                        break;
                    }

                    newChildIndex++;
                }

                sortedChildren.Insert(newChildIndex, historyEvent);
            }
        }

        private bool UpdateInterestingParent2(HistoryEvent historyEvent)
        {
            // Really need to check if historyEvent is part of our tree!
            // If not, remove it!

            bool isInteresting = InterestingEvent(historyEvent);
            bool wasInteresting = _interestingParents2.ContainsKey(historyEvent);

            if (isInteresting && wasInteresting)
            {
                // Nothing to do here!
                return false;
            }
            else if (isInteresting && !wasInteresting)
            {
                // Find our interesting children!
                List<HistoryEvent> interestingChildren = new List<HistoryEvent>(historyEvent.Children);

                int i = 0;
                while (i < interestingChildren.Count)
                {
                    HistoryEvent he = interestingChildren[i];

                    if (InterestingEvent(he))
                    {
                        i++;
                    }
                    else
                    {
                        interestingChildren.RemoveAt(i);
                        interestingChildren.AddRange(he.Children);
                    }
                }

                foreach (HistoryEvent he in interestingChildren)
                {
                    // We are now the interesting parent!
                    _interestingParents2[he] = historyEvent;
                }

                // Find our parent!
                HistoryEvent ourParent = historyEvent.Parent;
                while (ourParent != null && !InterestingEvent(ourParent))
                {
                    ourParent = ourParent.Parent;
                }

                if (ourParent != null)
                {
                    _interestingParents2[historyEvent] = ourParent;
                }

                return true;
            }
            else if (!isInteresting && wasInteresting)
            {
                HistoryEvent ourInterestingParent = _interestingParents2[historyEvent];

                // Find our interesting children!
                List<HistoryEvent> interestingChildren = new List<HistoryEvent>(historyEvent.Children);

                for (int i = 0; i < interestingChildren.Count;)
                {
                    HistoryEvent he = interestingChildren[i];

                    if (InterestingEvent(he))
                    {
                        i++;
                    }
                    else
                    {
                        interestingChildren.RemoveAt(i);
                        interestingChildren.AddRange(he.Children);
                    }
                }

                foreach (HistoryEvent he in interestingChildren)
                {
                    _interestingParents2[he] = ourInterestingParent;
                }

                _interestingParents2.Remove(historyEvent);

                return true;
            }
            else
            {
                // isn't interesting and wasn't interesting... nothing to do!
                return false;
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

                        //RefreshSortedChildrenMap(args.HistoryEvent);
                        //RefreshAncestorChildrenMaps(args.HistoryEvent);

                        _verticalEvents.Add(args.HistoryEvent);

                        // 
                        AddEventToChildren2(args.HistoryEvent);
                        UpdateInterestingParent2(args.HistoryEvent);
                        if (args.HistoryEvent.Parent != null)
                        {
                            UpdateInterestingParent2(args.HistoryEvent.Parent);
                        }

                        HistoryEvent ancestor = args.HistoryEvent.Parent;
                        while (ancestor != null)
                        {
                            UpdateHistoryTicks2(ancestor);
                            ancestor = ancestor.Parent;
                        }

                        // Update interesting parents
                    }
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        changed = _verticalEvents.FixOrder(args.HistoryEvent);
                        bool verticalChanged = changed;

                        //changed |= RefreshSortedChildrenMap(args.HistoryEvent);
                        //if (changed)
                        //{
                        //    //RefreshAncestorChildrenMaps(args.HistoryEvent);
                        //}

                        changed |= UpdateHistoryTicks2(args.HistoryEvent);

                        if (verticalChanged)
                        {
                            HistoryEvent ancestor = args.HistoryEvent.Parent;
                            while (ancestor != null)
                            {
                                UpdateHistoryTicks2(ancestor);
                                ancestor = ancestor.Parent;
                            }
                        }
                    }
                    break;
                case HistoryChangedAction.DeleteBranch:
                    {
                        changed = true;

                        //RefreshSortedChildrenMap(args.HistoryEvent);
                        //RefreshSortedChildrenMap(args.OriginalParentEvent);

                        List<HistoryEvent> childEvents = new List<HistoryEvent>();
                        childEvents.Add(args.HistoryEvent);

                        while (childEvents.Any())
                        {
                            HistoryEvent he = childEvents[0];
                            childEvents.RemoveAt(0);

                            _verticalEvents.Remove(he);

                            childEvents.AddRange(he.Children);
                        }

                        DeleteBranch2(args.HistoryEvent, args.OriginalParentEvent);
                        UpdateInterestingParent2(args.OriginalParentEvent);
                    }
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    {
                        changed = true;

                        //RefreshSortedChildrenMap(args.HistoryEvent);
                        //RefreshSortedChildrenMap(args.OriginalParentEvent);
                        //RefreshAncestorChildrenMaps(args.OriginalParentEvent);

                        _verticalEvents.Remove(args.HistoryEvent);

                        DeleteBookmark2(args.HistoryEvent, args.OriginalParentEvent, args.OriginalChildrenEvents);

                        // Find out who the interesting parent of all the moved children should be...
                        if (args.OriginalChildrenEvents.Any())
                        {
                            HistoryEvent interestingParentEvent = args.OriginalParentEvent;

                            while (interestingParentEvent != null && !InterestingEvent(interestingParentEvent))
                            {
                                interestingParentEvent = interestingParentEvent.Parent;
                            }

                            //foreach (HistoryEvent child in args.OriginalChildrenEvents)
                            //{

                            //}

                            List<HistoryEvent> interestingChildren = new List<HistoryEvent>(args.OriginalChildrenEvents);

                            for (int i = 0; i < interestingChildren.Count;)
                            {
                                HistoryEvent he = interestingChildren[i];

                                if (InterestingEvent(he))
                                {
                                    i++;
                                }
                                else
                                {
                                    interestingChildren.RemoveAt(i);
                                    interestingChildren.AddRange(he.Children);
                                }
                            }

                            foreach (HistoryEvent he in interestingChildren)
                            {
                                _interestingParents2[he] = interestingParentEvent;
                            }
                        }

                    }
                    break;
            }

            //if (changed)
            //{
            //    return new PositionChangedEventArgs<HistoryEvent>(HorizontalOrdering, VerticalOrdering, InterestingParents);
            //}

            return changed;
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

        protected int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            UInt64 yMax = y.MaxDescendentTicks;
            UInt64 xMax = x.MaxDescendentTicks;
            int result = yMax.CompareTo(xMax);

            if (result != 0)
            {
                return result;
            }

            return y.Id.CompareTo(x.Id);
        }

        private History _history;

        private SortedVerticalHistoryEventList _verticalEvents;

        //private ConditionalWeakTable<HistoryEvent, List<HistoryEvent>> _sortedChildren;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParents;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParents2;

        private Dictionary<HistoryEvent, List<HistoryEvent>> _sortedChildren2;

        public event NotifyPositionChangedEventHandler<HistoryEvent> OrderingChanged;

        private class SortedVerticalHistoryEventList
        {
            public SortedVerticalHistoryEventList()
            {
                _historyEvents = new List<HistoryEvent>();
                _ties = new HashSet<Tuple<HistoryEvent, HistoryEvent>>();
            }

            public bool Add(HistoryEvent historyEvent)
            {
                if (_historyEvents.Contains(historyEvent))
                {
                    return false;
                }


                int insertionIndex = 0;
                while (insertionIndex < _historyEvents.Count)
                {
                    int comparison = VerticalSort(historyEvent, _historyEvents[insertionIndex]);
                    if (comparison <= 0)
                    {
                        break;
                    }

                    insertionIndex++;
                }

                _historyEvents.Insert(insertionIndex, historyEvent);

                return true;
            }

            public bool Remove(HistoryEvent historyEvent)
            {
                bool removed = _historyEvents.Remove(historyEvent);

                // Get rid of ties...
                List<Tuple<HistoryEvent, HistoryEvent>> removeTies = new List<Tuple<HistoryEvent, HistoryEvent>>();
                foreach (Tuple<HistoryEvent, HistoryEvent> tie in _ties)
                {
                    if (tie.Item1 == historyEvent || tie.Item2 == historyEvent)
                    {
                        removeTies.Add(tie);
                    }
                }

                foreach (Tuple<HistoryEvent, HistoryEvent> tie in removeTies)
                {
                    _ties.Remove(tie);
                }

                return removed;
            }

            public bool FixOrder(HistoryEvent historyEvent)
            {
                int verticalIndex = _historyEvents.IndexOf(historyEvent);

                bool CheckOrder()
                {
                    if (verticalIndex > 0)
                    {
                        HistoryEvent previousHistoryEvent = _historyEvents[verticalIndex - 1];
                        bool wasTied = IsTied(historyEvent, previousHistoryEvent);
                        if (VerticalSort(previousHistoryEvent, historyEvent) >= 0)
                        {
                            return true;
                        }

                        if (wasTied)
                        {
                            return true;
                        }
                    }

                    if (verticalIndex + 1 < _historyEvents.Count)
                    {
                        HistoryEvent nextHistoryEvent = _historyEvents[verticalIndex + 1];
                        bool wasTied = IsTied(historyEvent, nextHistoryEvent);
                        if (VerticalSort(historyEvent, nextHistoryEvent) >= 0)
                        {
                            return true;
                        }

                        if (wasTied)
                        {
                            return true;
                        }
                    }

                    return false;
                }

                if (CheckOrder())
                {
                    _historyEvents.RemoveAt(verticalIndex);

                    int newVerticalIndex = 0;
                    while (newVerticalIndex < _historyEvents.Count)
                    {
                        if (VerticalSort(historyEvent, _historyEvents[newVerticalIndex]) < 0)
                        {
                            break;
                        }

                        newVerticalIndex++;
                    }

                    _historyEvents.Insert(newVerticalIndex, historyEvent);

                    return true;
                }

                return false;
            }

            private bool IsTied(HistoryEvent x, HistoryEvent y)
            {
                if (!_ties.Any())
                {
                    return false;
                }

                return _ties.Contains(new Tuple<HistoryEvent, HistoryEvent>(x, y));
            }

            private int VerticalSort(HistoryEvent x, HistoryEvent y)
            {
                if (_ties.Any())
                {
                    _ties.Remove(new Tuple<HistoryEvent, HistoryEvent>(x, y));
                    _ties.Remove(new Tuple<HistoryEvent, HistoryEvent>(y, x));
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

                _ties.Add(new Tuple<HistoryEvent, HistoryEvent>(x, y));
                _ties.Add(new Tuple<HistoryEvent, HistoryEvent>(y, x));

                return x.Id.CompareTo(y.Id);
            }

            public List<HistoryEvent> GetEvents()
            {
                return _historyEvents.Where(n => HistoryEventOrdering.InterestingEvent(n)).ToList();
            }

            private List<HistoryEvent> _historyEvents;
            private HashSet<Tuple<HistoryEvent, HistoryEvent>> _ties;
        }
    }
}
