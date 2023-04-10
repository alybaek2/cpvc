using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class SortedVerticalHistoryEventList
    {
        public SortedVerticalHistoryEventList()
        {
            _historyEvents = new List<HistoryEvent>();
            _verticalTies = new HashSet<Tuple<HistoryEvent, HistoryEvent>>();
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
            foreach (Tuple<HistoryEvent, HistoryEvent> tie in _verticalTies)
            {
                if (tie.Item1 == historyEvent || tie.Item2 == historyEvent)
                {
                    removeTies.Add(tie);
                }
            }

            foreach (Tuple<HistoryEvent, HistoryEvent> tie in removeTies)
            {
                _verticalTies.Remove(tie);
            }

            return removed;
        }

        public bool FixOrder(HistoryEvent historyEvent)
        {
            if (CheckOrder(historyEvent))
            {
                // Update the vertical order
                int verticalIndex = _historyEvents.IndexOf(historyEvent);

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

        private bool CheckOrder(HistoryEvent historyEvent)
        {
            int verticalIndex = _historyEvents.IndexOf(historyEvent);

            if (verticalIndex > 0)
            {
                bool previouslyTied = _verticalTies.Any() && _verticalTies.Contains(new Tuple<HistoryEvent, HistoryEvent>(historyEvent, _historyEvents[verticalIndex - 1]));
                if (VerticalSort(_historyEvents[verticalIndex - 1], historyEvent) >= 0)
                {
                    return true;
                }

                if (previouslyTied)
                {
                    return true;
                }
            }

            if (verticalIndex + 1 < _historyEvents.Count)
            {
                bool previouslyTied = _verticalTies.Any() && _verticalTies.Contains(new Tuple<HistoryEvent, HistoryEvent>(historyEvent, _historyEvents[verticalIndex + 1]));
                if (VerticalSort(historyEvent, _historyEvents[verticalIndex + 1]) >= 0)
                {
                    return true;
                }

                if (previouslyTied)
                {
                    return true;
                }
            }

            return false;
        }

        private int VerticalSort(HistoryEvent x, HistoryEvent y)
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

        public List<HistoryEvent> GetEvents()
        {
            return _historyEvents.Where(n => HistoryListTree.InterestingEvent(n)).ToList();
        }

        private List<HistoryEvent> _historyEvents;
        private HashSet<Tuple<HistoryEvent, HistoryEvent>> _verticalTies;
    }

    public class HistoryListTree
    {
        public HistoryListTree(History history)
        {
            _sortedChildren = new ConditionalWeakTable<HistoryEvent, List<HistoryEvent>>();

            _interestingParents = new Dictionary<HistoryEvent, HistoryEvent>();
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
                    if (historyEvent.Parent == null)
                    {
                        // This is the root!
                        continue;
                    }

                    _interestingParents.Add(historyEvent, GetInterestingParent(historyEvent));
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
                RefreshSortedChildrenMap(allNodes[c]);

                _verticalEvents.Add(allNodes[c]);

                allNodes.AddRange(allNodes[c].Children);
            }

            //foreach(HistoryEvent he in allNodes)
            //{
            //    _verticalEvents.Add(he);
            //}
        }

        private void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {
            PositionChangedEventArgs<HistoryEvent> changeArgs = UpdateListTree(args);
            if (changeArgs != null)
            {
                PositionChanged?.Invoke(this, changeArgs);
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

        public List<HistoryEvent> HorizontalOrdering
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
                        if (!_sortedChildren.TryGetValue(he, out List<HistoryEvent> children))
                        {
                            throw new Exception("There should be a node here!");
                        }

                        events.InsertRange(0, children);
                    }
                }

                return horizontalEvents;
            }
        }

        private bool RefreshAncestorChildrenMaps(HistoryEvent historyEvent)
        {
            bool result = false;
            HistoryEvent parentEvent = historyEvent.Parent;

            while (parentEvent != null)
            {
                result |= RefreshSortedChildrenMap(parentEvent);

                parentEvent = parentEvent.Parent;
            }

            return result;
        }

        private bool RefreshSortedChildrenMap(HistoryEvent historyEvent)
        {
            if (historyEvent.Children.Count <= 1)
            {
                if (_sortedChildren.TryGetValue(historyEvent, out _))
                {
                    _sortedChildren.Remove(historyEvent);
                }

                return false;
            }

            // We should have something in this map!
            if (_sortedChildren.TryGetValue(historyEvent, out List<HistoryEvent> children))
            {
                foreach (HistoryEvent ce in historyEvent.Children)
                {
                    if (!children.Contains(ce))
                    {
                        children.Add(ce);
                    }
                }

                for (int c = children.Count - 1; c >= 0; c--)
                {
                    if (!historyEvent.Children.Contains(children[c]))
                    {
                        children.RemoveAt(c);
                    }
                }

                children.Sort(HorizontalSort);
            }
            else
            {
                children = new List<HistoryEvent>(historyEvent.Children);
                children.Sort(HorizontalSort);

                _sortedChildren.Add(historyEvent, children);
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
                        changed = true;

                        RefreshSortedChildrenMap(args.HistoryEvent);
                        RefreshAncestorChildrenMaps(args.HistoryEvent);

                        _verticalEvents.Add(args.HistoryEvent);
                    }
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        changed = _verticalEvents.FixOrder(args.HistoryEvent);
                        changed |= RefreshSortedChildrenMap(args.HistoryEvent);
                        if (changed)
                        {
                            RefreshAncestorChildrenMaps(args.HistoryEvent);
                        }
                    }
                    break;
                case HistoryChangedAction.DeleteBranch:
                    {
                        changed = true;

                        RefreshSortedChildrenMap(args.HistoryEvent);
                        RefreshSortedChildrenMap(args.OriginalParentEvent);

                        List<HistoryEvent> childEvents = new List<HistoryEvent>();
                        childEvents.Add(args.HistoryEvent);

                        while (childEvents.Any())
                        {
                            HistoryEvent he = childEvents[0];
                            childEvents.RemoveAt(0);

                            _verticalEvents.Remove(he);

                            childEvents.AddRange(he.Children);
                        }
                    }
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    {
                        changed = true;

                        RefreshSortedChildrenMap(args.HistoryEvent);
                        RefreshSortedChildrenMap(args.OriginalParentEvent);
                        RefreshAncestorChildrenMaps(args.OriginalParentEvent);

                        _verticalEvents.Remove(args.HistoryEvent);
                    }
                    break;
            }

            if (changed)
            {
                return new PositionChangedEventArgs<HistoryEvent>(HorizontalOrdering, VerticalOrdering, InterestingParents);
            }

            return null;
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

        private ConditionalWeakTable<HistoryEvent, List<HistoryEvent>> _sortedChildren;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParents;

        public event NotifyPositionChangedEventHandler<HistoryEvent> PositionChanged;
    }
}
