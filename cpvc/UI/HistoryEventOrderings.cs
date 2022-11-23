using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CPvC
{
    public class HistoryEventOrderings
    {
        public HistoryEventOrderings(History history)
        {
            _history = history;
            _fullRefresh = true;
            _items = new List<HistoryViewItem>();
            Init(_history);
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
            }

            if (v > 0)
            {
                if (VerticalSort(_verticalEvents[v - 1], historyEvent) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void Add(HistoryEvent historyEvent)
        {
            HistoryEvent parentEvent = historyEvent.Parent;

            if (_verticalPosition.TryGetValue(parentEvent, out int parentVerticalPosition))
            {
                if (!InterestingEvent(parentEvent))
                {
                    if (InterestingEvent(historyEvent))
                    {
                        bool n = true;
                        if (parentVerticalPosition < _verticalEvents.Count - 1)
                        {
                            if (VerticalSort(historyEvent, _verticalEvents[parentVerticalPosition + 1]) > 0)
                            {
                                n = false;
                            }
                        }

                        if (parentVerticalPosition > 0)
                        {
                            if (VerticalSort(_verticalEvents[parentVerticalPosition - 1], historyEvent) > 0)
                            {
                                n = false;
                            }
                        }

                        if (n)
                        {
                            foreach (HistoryViewItem item in _items)
                            {
                                //HistoryViewItem item = (HistoryViewItem)o;
                                if (item.HistoryEvent == parentEvent)
                                {
                                    item.HistoryEvent = historyEvent;

                                    _verticalEvents[parentVerticalPosition] = historyEvent;
                                    _verticalPosition.Remove(parentEvent);
                                    _verticalPosition.Add(historyEvent, parentVerticalPosition);

                                    return;
                                }
                            }
                        }
                    }
                }
            }

            _fullRefresh = true;
        }

        public List<HistoryViewItem> UpdateItems()
        {
            if (!_fullRefresh)
            {
                return _items;
            }

            // Do a full refresh!
            Init(_history);

            List<HistoryViewItem> historyItems = _verticalEvents.Select(x => new HistoryViewItem(x)).ToList();

            foreach (HistoryEvent horizontalEvent in _horizontalEvents)
            {
                int v = _verticalPosition[horizontalEvent];

                HistoryEvent parentEvent = _parentEvents[horizontalEvent];
                int pv = parentEvent != null ? _verticalPosition[parentEvent] : -1;
                int ph = parentEvent != null ? historyItems[pv].Events.FindIndex(x => ReferenceEquals(x, parentEvent)) : 0;

                // "Draw" the history event from pv + 1 to v
                for (int d = pv + 1; d <= v; d++)
                {
                    HistoryViewItem historyViewItem = historyItems[d];

                    // Pad out the Events to ensure the line connecting us to our parent never moves to the left.... it just looks better that way!
                    for (int padIndex = historyViewItem.Events.Count; padIndex < ph; padIndex++)
                    {
                        historyViewItem.Events.Add(null);
                    }

                    historyViewItem.Events.Add(horizontalEvent);
                    ph = Math.Max(ph, historyViewItem.Events.Count - 1);
                }
            }

            _items = historyItems;
            _fullRefresh = false;

            return historyItems;
        }

        public bool Process(HistoryEvent e, HistoryChangedAction action)
        {
            if (_fullRefresh)
            {
                // No need to check, we're refreshing the tree anyway!
                return true;
            }

            switch (action)
            {
                case HistoryChangedAction.Add:
                    Add(e);
                    break;
                case HistoryChangedAction.DeleteBranch:
                case HistoryChangedAction.DeleteBookmark:
                    _fullRefresh = true;
                    break;
                case HistoryChangedAction.SetCurrent:
                    _fullRefresh = true;
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    _fullRefresh = VerticalPositionChanged(e);
                    break;
            }

            return _fullRefresh;
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
            int result = y.GetMaxDescendentTicks().CompareTo(x.GetMaxDescendentTicks());

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

            return x.Id.CompareTo(y.Id);
        }

        private void Init(History history)
        {
            _parentEvents = new Dictionary<HistoryEvent, HistoryEvent>();
            _horizontalEvents = new List<HistoryEvent>();

            _verticalEvents = new List<HistoryEvent>();
            List<HistoryEvent> children = new List<HistoryEvent>();

            _horizontalPosition = new Dictionary<HistoryEvent, int>();
            _verticalPosition = new Dictionary<HistoryEvent, int>();

            if (history == null)
            {
                return;
            }

            _horizontalEvents.Add(history.RootEvent);

            int i = 0;

            while (i < _horizontalEvents.Count)
            {
                children.Clear();
                children.AddRange(_horizontalEvents[i].Children);
                children.Sort((x, y) => HorizontalSort(x, y));

                if (!InterestingEvent(_horizontalEvents[i]))
                {
                    _horizontalEvents.RemoveAt(i);
                    i--;
                }
                else
                {
                    _verticalEvents.Add(_horizontalEvents[i]);
                }

                _horizontalEvents.InsertRange(i + 1, children);
                i++;
            }

            // Figure out parents.
            foreach (HistoryEvent historyEvent in _horizontalEvents)
            {
                HistoryEvent parentEvent = historyEvent.Parent;
                while (parentEvent != null && !_verticalEvents.Contains(parentEvent))
                {
                    parentEvent = parentEvent.Parent;
                }

                _parentEvents.Add(historyEvent, parentEvent);
            }

            _verticalEvents.Sort((x, y) => VerticalSort(x, y));

            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                _verticalPosition.Add(_verticalEvents[v], v);
            }

            for (int h = 0; h < _horizontalEvents.Count; h++)
            {
                _horizontalPosition.Add(_horizontalEvents[h], h);
            }
        }

        private History _history;
        private Dictionary<HistoryEvent, HistoryEvent> _parentEvents;
        private List<HistoryEvent> _horizontalEvents;
        private List<HistoryEvent> _verticalEvents;
        private Dictionary<HistoryEvent, int> _horizontalPosition;
        private Dictionary<HistoryEvent, int> _verticalPosition;

        private bool _fullRefresh;
        private List<HistoryViewItem> _items;
    }
}
