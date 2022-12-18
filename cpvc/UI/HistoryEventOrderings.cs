using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CPvC
{
    public class HistoryEventOrderings
    {
        public HistoryEventOrderings()
        {
            _history = null;
            //_fullRefresh = false;
            _items = null;
            _horizontalEvents = new List<HistoryEvent>();
            _verticalEvents = new List<HistoryEvent>();
            _verticalTies = new HashSet<Tuple<HistoryEvent, HistoryEvent>>();
        }

        public List<HistoryEvent> HorizontalOrdering()
        {
            return _horizontalEvents;
        }

        public HistoryEvent ParentEvent(HistoryEvent historyEvent)
        {
            if (!_interestingParentEvents.ContainsKey(historyEvent))
            {
                string y = "";
            }

            return _interestingParentEvents[historyEvent];
        }

        public int VerticalPosition(HistoryEvent historyEvent)
        {
            return _verticalPosition[historyEvent];
        }

        public void SetHistory(History history)
        {
            _history = history;
            //_fullRefresh = true;
            _items = new List<HistoryViewItem>();

            Init(history);
        }

        private void CheckForHorizontalChanges(HistoryEvent historyEvent)
        {
            HashSet<HistoryEvent> eventsToInvalidate = new HashSet<HistoryEvent>();

            while (historyEvent?.Parent != null)
            {
                List<HistoryEvent> siblings = historyEvent.Parent.Children;

                int index = siblings.FindIndex(x => ReferenceEquals(x, historyEvent));
                if (index < 0)
                {
                    throw new Exception("Event could not be found in its parent's children!");
                }

                bool moved = false;
                //int v = _verticalPosition[historyEvent];
                if (index < siblings.Count - 1)
                {
                    if (HorizontalSort(historyEvent, siblings[index + 1]) >= 0)
                    {
                        moved = true;
                    }
                }

                if (!moved && index > 0)
                {
                    if (HorizontalSort(siblings[index - 1], historyEvent) >= 0)
                    {
                        moved = true;
                    }
                }

                if (moved)
                {
                    // Figure out all the history events that have moved in the list of siblings.
                    // Set all of their corresponding HistoryViewItems as "needs redraw" and update
                    // their Events. Include all of their descendents too.
                    List<HistoryEvent> sortedSiblings = new List<HistoryEvent>(siblings);
                    sortedSiblings.Sort((x, y) => HorizontalSort(x, y));

                    for (int i = 0; i < siblings.Count; i++)
                    {
                        if (!ReferenceEquals(siblings[i], sortedSiblings[i]))
                        {
                            eventsToInvalidate.Add(siblings[i]);

                            // Add all descendents too!
                            List<HistoryEvent> descendents = new List<HistoryEvent>();
                            descendents.Add(siblings[i]);
                            while (descendents.Any())
                            {
                                HistoryEvent h = descendents[0];
                                descendents.RemoveAt(0);

                                eventsToInvalidate.Add(h);

                                descendents.AddRange(h.Children);
                            }
                        }
                    }
                }

                historyEvent = historyEvent.Parent;
            }

            foreach (HistoryViewItem item in _items)
            {
                if (eventsToInvalidate.Contains(item.HistoryEvent))
                {
                    item.Invalidate();
                }
            }
        }

        private bool VerticalPositionChanged(HistoryEvent historyEvent)
        {
            // Has "interestingness" changed?
            if (InterestingEvent(historyEvent))
            {
                if (!_verticalEvents.Contains(historyEvent))
                {
                    return true;
                }
            }
            else
            {
                return _verticalEvents.Contains(historyEvent);
            }

            int v = _verticalPosition[historyEvent];
            if (v < _verticalEvents.Count - 1)
            {
                if (VerticalSort(historyEvent, _verticalEvents[v + 1]) >= 0)
                {
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pair = new Tuple<HistoryEvent, HistoryEvent>(historyEvent, _verticalEvents[v + 1]);
                if (_verticalTies.Contains(pair))
                {
                    _verticalTies.Remove(pair);
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pairReverse = new Tuple<HistoryEvent, HistoryEvent>(_verticalEvents[v + 1], historyEvent);
                if (_verticalTies.Contains(pairReverse))
                {
                    _verticalTies.Remove(pairReverse);
                    return true;
                }
            }

            if (v > 0)
            {
                if (VerticalSort(_verticalEvents[v - 1], historyEvent) >= 0)
                {
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pair = new Tuple<HistoryEvent, HistoryEvent>(_verticalEvents[v - 1], historyEvent);
                if (_verticalTies.Contains(pair))
                {
                    _verticalTies.Remove(pair);
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pairReverse = new Tuple<HistoryEvent, HistoryEvent>(_verticalEvents[v - 1], historyEvent);
                if (_verticalTies.Contains(pairReverse))
                {
                    _verticalTies.Remove(pairReverse);
                    return true;
                }
            }

            return false;
        }

        private void CalculateHorizontalEvent()
        {
            Init(_history);

            return;

            _horizontalEvents.Clear();
            List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();

            horizontalEvents.Add(_history.RootEvent);
            while (horizontalEvents.Any())
            {
                HistoryEvent horizontalEvent = horizontalEvents[0];
                horizontalEvents.RemoveAt(0);

                if (InterestingEvent(horizontalEvent))
                {
                    _horizontalEvents.Add(horizontalEvent);
                }

                horizontalEvents.AddRange(horizontalEvent.Children);
            }

            _interestingParentEvents.Clear();
            foreach (HistoryEvent historyEvent in _verticalEvents)
            {
                HistoryEvent parentEvent = historyEvent.Parent;
                while (parentEvent != null && !_verticalEvents.Contains(parentEvent))
                {
                    parentEvent = parentEvent.Parent;
                }

                _interestingParentEvents.Add(historyEvent, parentEvent);
            }
        }

        private void Add(HistoryEvent historyEvent)
        {
            HistoryEvent parentEvent = historyEvent.Parent;

            if (_verticalPosition.TryGetValue(parentEvent, out int parentVerticalPosition))
            {
                bool parentIsInteresting = InterestingEvent(parentEvent);
                bool interesting = InterestingEvent(historyEvent);
                if (!parentIsInteresting && interesting)
                {
                    bool verticalPositionUnchanged = false;
                    if (parentVerticalPosition >= _verticalEvents.Count - 1 || VerticalSort(historyEvent, _verticalEvents[parentVerticalPosition + 1]) <= 0)
                    {
                        if (parentVerticalPosition <= 0 || VerticalSort(_verticalEvents[parentVerticalPosition - 1], historyEvent) <= 0)
                        {
                            verticalPositionUnchanged = true;
                        }
                    }

                    if (verticalPositionUnchanged)
                    {
                        //foreach (HistoryViewItem item in _items)
                        {
                            //if (ReferenceEquals(item.HistoryEvent, parentEvent))
                            //{
                            //    item.HistoryEvent = historyEvent;

                            _verticalEvents[parentVerticalPosition] = historyEvent;
                            _verticalPosition.Remove(parentEvent);
                            _verticalPosition.Add(historyEvent, parentVerticalPosition);

                            int hpos = _horizontalEvents.FindIndex(x => ReferenceEquals(x, parentEvent));
                            if (hpos == -1)
                            {
                                throw new Exception("Should have been here!");
                            }

                            _horizontalEvents[hpos] = historyEvent;

                            HistoryEvent interestingParentEvent = _interestingParentEvents[parentEvent];
                            _interestingParentEvents.Add(historyEvent, interestingParentEvent);
                            _interestingParentEvents.Remove(parentEvent);

                            return;
                            //}
                        }
                    }
                }

                // Remove parent from vertical if it's no longer interesting!
                if (!parentIsInteresting)
                {
                    _verticalEvents.RemoveAt(parentVerticalPosition);
                    _verticalPosition.Remove(parentEvent);
                    _horizontalEvents.Remove(parentEvent);
                    _interestingParentEvents.Remove(parentEvent);
                }
            }

            // Add to vertical
            bool inserted = false;
            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                if (VerticalSort(historyEvent, _verticalEvents[v]) < 0)
                {
                    _verticalEvents.Insert(v, historyEvent);
                    _verticalPosition[historyEvent] = v;

                    // SHove all the rest down a spot!
                    for (int j = v + 1; j < _verticalEvents.Count; j++)
                    {
                        _verticalPosition[_verticalEvents[j]] = j;
                    }

                    inserted = true;
                    break;
                }
            }


            if (!inserted)
            {
                _verticalEvents.Add(historyEvent);
                _verticalPosition[historyEvent] = _verticalEvents.Count - 1;
            }

            CalculateHorizontalEvent();

            //_fullRefresh = true;
        }

        public List<HistoryViewItem> UpdateItems()
        {
            //if (!_fullRefresh)
            //{
            //    return _items;
            //}

            // Do a full refresh!
            Stopwatch sw = Stopwatch.StartNew();
            Init(_history);
            sw.Stop();

            CPvC.Diagnostics.Trace("Orderings Init took {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            List<HistoryViewItem> historyItems = _verticalEvents.Select(x => new HistoryViewItem(x)).ToList();

            List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();
            horizontalEvents.Add(_history.RootEvent);

            while (horizontalEvents.Any())
            {
                HistoryEvent horizontalEvent = horizontalEvents[0];
                horizontalEvents.RemoveAt(0);

                if (InterestingEvent(horizontalEvent))
                {
                    _horizontalEvents.Add(horizontalEvent);

                    int v = _verticalPosition[horizontalEvent];

                    HistoryEvent parentEvent = _interestingParentEvents[horizontalEvent];
                    int parentVerticalPosition = parentEvent != null ? _verticalPosition[parentEvent] : -1;
                    int parentHorizontalPosition = parentEvent != null ? historyItems[parentVerticalPosition].Events.FindIndex(x => ReferenceEquals(x, parentEvent)) : 0;

                    // "Draw" the history event from pv + 1 to v
                    for (int d = parentVerticalPosition + 1; d <= v; d++)
                    {
                        HistoryViewItem historyViewItem = historyItems[d];

                        // Pad out the Events to ensure the line connecting us to our parent never moves to the left.... it just looks better that way!
                        for (int padIndex = historyViewItem.Events.Count; padIndex < parentHorizontalPosition; padIndex++)
                        {
                            historyViewItem.Events.Add(null);
                        }

                        historyViewItem.Events.Add(horizontalEvent);
                        parentHorizontalPosition = Math.Max(parentHorizontalPosition, historyViewItem.Events.Count - 1);
                    }
                }

                List<HistoryEvent> children = horizontalEvent.Children;
                children.Sort((x, y) => HorizontalSort(x, y));
                horizontalEvents.InsertRange(0, children);
            }

            //foreach (HistoryEvent horizontalEvent in _horizontalEvents)
            //{
            //    int v = _verticalPosition[horizontalEvent];

            //    HistoryEvent parentEvent = _interestingParentEvents[horizontalEvent];
            //    int parentVerticalPosition = parentEvent != null ? _verticalPosition[parentEvent] : -1;
            //    int parentHorizontalPosition = parentEvent != null ? historyItems[parentVerticalPosition].Events.FindIndex(x => ReferenceEquals(x, parentEvent)) : 0;

            //    // "Draw" the history event from pv + 1 to v
            //    for (int d = parentVerticalPosition + 1; d <= v; d++)
            //    {
            //        HistoryViewItem historyViewItem = historyItems[d];

            //        // Pad out the Events to ensure the line connecting us to our parent never moves to the left.... it just looks better that way!
            //        for (int padIndex = historyViewItem.Events.Count; padIndex < parentHorizontalPosition; padIndex++)
            //        {
            //            historyViewItem.Events.Add(null);
            //        }

            //        historyViewItem.Events.Add(horizontalEvent);
            //        parentHorizontalPosition = Math.Max(parentHorizontalPosition, historyViewItem.Events.Count - 1);
            //    }
            //}

            sw.Stop();
            CPvC.Diagnostics.Trace("Orderings Draw and padding took {0}ms", sw.ElapsedMilliseconds);

            _items = historyItems;
            //_fullRefresh = false;

            return historyItems;
        }

        public bool Process(HistoryEvent e, HistoryChangedAction action)
        {
            //if (_fullRefresh)
            //{
            //    // No need to check, we're refreshing the tree anyway!
            //    return false;
            //}

            switch (action)
            {
                case HistoryChangedAction.Add:
                    Add(e);
                    break;
                case HistoryChangedAction.DeleteBranch:
                case HistoryChangedAction.DeleteBookmark:
                    //_fullRefresh = true;
                    break;
                case HistoryChangedAction.SetCurrent:
                    CPvC.Diagnostics.Trace("[Process] SetCurrent... doing full refresh!");
                    //_fullRefresh = true;
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        bool verticalPositionChanged = VerticalPositionChanged(e);
                        if (verticalPositionChanged)
                        {
                            _verticalEvents.Sort((x, y) => VerticalSort(x, y));

                            _verticalPosition.Clear();
                            for (int i = 0; i < _verticalEvents.Count; i++)
                            {
                                HistoryEvent h = _verticalEvents[i];
                                _verticalPosition[h] = i;
                            }

                            CalculateHorizontalEvent();

                            //CheckForHorizontalChanges(e);
                        }
                        else
                        {
                            //if (_verticalTies.Contains(e))
                            //{
                            //    // Check e's neighbours.
                            //}
                            // Even if vertical position hasn't changed, the horizontal position might have!
                            // if two siblings have the same MaxDescendentTicks, but sibling 1 was places vertically higher,
                            // and to the right of sibling 2, then sibling 1 updates so it should be placed to the left, it
                            // wont in this case since the vertical position didn't change. or does that mean that two siblings that are 
                            // tied should be placed to the left if it's also placed higher vertically?
                            return false;
                        }

                        //_fullRefresh = verticalPositionChanged;
                    }
                    break;
            }

            return true;

            //return _fullRefresh;
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

        private int HorizontalSort(HistoryEvent x, HistoryEvent y)
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

        private int VerticalSort(HistoryEvent x, HistoryEvent y)
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

            return x.Id.CompareTo(y.Id);
        }

        public HistoryEvent GetTile(int horizontalIndex, int verticalIndex)
        {
            HistoryEvent h = _horizontalEvents[horizontalIndex];
            HistoryEvent parent = null;
            if (!_interestingParentEvents.TryGetValue(h, out parent))
            {
                parent = null;
            }

            int parentVertical = (parent == null) ? -1 : _verticalPosition[parent];

            if (parentVertical < verticalIndex && verticalIndex <= _verticalPosition[h])
            {
                return h;
            }

            return null;
        }

        private void Init(History history)
        {
            _interestingParentEvents = new Dictionary<HistoryEvent, HistoryEvent>();
            _horizontalEvents.Clear();
            //_horizontalEvents = new List<HistoryEvent>();

            _verticalEvents.Clear();
            //_verticalEvents = new List<HistoryEvent>();
            List<HistoryEvent> children = new List<HistoryEvent>();

            //_horizontalPosition = new Dictionary<HistoryEvent, int>();
            _verticalPosition = new Dictionary<HistoryEvent, int>();

            if (history == null)
            {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            Dictionary<HistoryEvent, UInt64> maxDescendentTicks = new Dictionary<HistoryEvent, UInt64>();

            Stack<HistoryEvent> stack = new Stack<HistoryEvent>();

            stack.Push(history.RootEvent);
            while (stack.Any())
            {
                HistoryEvent top = stack.Peek();

                bool allChildrenDone = true;
                UInt64 max = top.Ticks;
                foreach (HistoryEvent child in top.Children)
                {
                    if (maxDescendentTicks.TryGetValue(child, out UInt64 ticks))
                    {
                        if (ticks > max)
                        {
                            max = ticks;
                        }
                    }
                    else
                    {
                        stack.Push(child);
                        allChildrenDone = false;
                        break;
                    }
                }

                if (allChildrenDone)
                {
                    maxDescendentTicks.Add(top, max);
                    stack.Pop();
                }
            }

            int CachedHorizontalSort(HistoryEvent x, HistoryEvent y)
            {
                int result = maxDescendentTicks[y].CompareTo(maxDescendentTicks[x]);
                if (result != 0)
                {
                    return result;
                }

                return y.Id.CompareTo(x.Id);
            }

            sw.Stop();
            CPvC.Diagnostics.Trace("Caching max descendent ticks took {0}ms", sw.ElapsedMilliseconds);



            List<HistoryEvent> historyEvents = new List<HistoryEvent>();
            historyEvents.Add(history.RootEvent);

            sw.Reset();
            //Stopwatch sw = Stopwatch.StartNew();

            //int i = 0;

            while (historyEvents.Any())
            {
                HistoryEvent historyEvent = historyEvents[0];
                historyEvents.RemoveAt(0);

                children.Clear();
                children.AddRange(historyEvent.Children);
                children.Sort((x, y) => CachedHorizontalSort(x, y));

                if (InterestingEvent(historyEvent))
                {
                    _verticalEvents.Add(historyEvent);
                    _horizontalEvents.Add(historyEvent);
                }
                else
                {
                    //horizontalEvents.RemoveAt(i);
                    //i--;
                }
                //else

                historyEvents.InsertRange(0, children);

                //i++;
            }

            sw.Stop();

            CPvC.Diagnostics.Trace("Traversing tree took {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            // Figure out parents.
            foreach (HistoryEvent historyEvent in _verticalEvents)
            {
                HistoryEvent parentEvent = historyEvent.Parent;
                while (parentEvent != null && !_verticalEvents.Contains(parentEvent))
                {
                    parentEvent = parentEvent.Parent;
                }

                _interestingParentEvents.Add(historyEvent, parentEvent);
            }

            sw.Stop();

            CPvC.Diagnostics.Trace("Calculating parents took {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            _verticalEvents.Sort((x, y) => VerticalSort(x, y));

            sw.Stop();

            CPvC.Diagnostics.Trace("Sorting vertically took {0}ms", sw.ElapsedMilliseconds);

            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                _verticalPosition.Add(_verticalEvents[v], v);
            }

            //for (int h = 0; h < _horizontalEvents.Count; h++)
            //{
            //    _horizontalPosition.Add(_horizontalEvents[h], h);
            //}
        }

        private History _history;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParentEvents;
        private List<HistoryEvent> _horizontalEvents;
        private List<HistoryEvent> _verticalEvents;
        //private Dictionary<HistoryEvent, int> _horizontalPosition;
        //private ConditionalWeakTable<HistoryEvent, List>
        private Dictionary<HistoryEvent, int> _verticalPosition;

        //private bool _fullRefresh;
        private List<HistoryViewItem> _items;
        private HashSet<Tuple<HistoryEvent, HistoryEvent>> _verticalTies;
    }
}
