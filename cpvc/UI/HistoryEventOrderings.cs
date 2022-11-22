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
        public HistoryEventOrderings(ItemCollection itemCollection, History history)
        {
            _itemCollection = itemCollection;
            _fullRefresh = false;
            Init(history);
        }

        public IEnumerable<HistoryEvent> GetVerticallySorted()
        {
            return _verticalEvents;
        }

        public IEnumerable<HistoryEvent> GetHorizonallySorted()
        {
            return _horizontalEvents;
        }

        public int GetVerticalPosition(HistoryEvent historyEvent)
        {
            return _verticalPosition[historyEvent];
        }

        public int GetHorizontalPosition(HistoryEvent historyEvent)
        {
            return _horizontalPosition[historyEvent];
        }

        public HistoryEvent GetParent(HistoryEvent historyEvent)
        {
            return _parentEvents[historyEvent];
        }

        public int Count()
        {
            return _verticalEvents.Count;
        }

        public bool VerticalPositionChanged(HistoryEvent historyEvent)
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

            int v = GetVerticalPosition(historyEvent);
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
                            foreach (object o in _itemCollection)
                            {
                                HistoryViewItem item = (HistoryViewItem)o;
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

            _lastAddedEvent = historyEvent;
            _fullRefresh = true;
        }

        public void Process(HistoryControl historyControl, HistoryEvent e, HistoryEvent formerParentEvent, HistoryChangedAction action)
        {
            if (_fullRefresh)
            {
                // No need to check, we're refreshing the tree anyway!
                return;
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
                    if (!ReferenceEquals(e, _lastAddedEvent))
                    {
                        //_fullRefresh = true;
                    }
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    _fullRefresh = VerticalPositionChanged(e);
                    break;
            }

            if (_fullRefresh)
            {
                historyControl.GenerateTree();
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

            _horizontalPosition = new Dictionary<HistoryEvent, int>();
            _verticalPosition = new Dictionary<HistoryEvent, int>();

            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                _verticalPosition.Add(_verticalEvents[v], v);
            }

            for (int h = 0; h < _horizontalEvents.Count; h++)
            {
                _horizontalPosition.Add(_horizontalEvents[h], h);
            }
        }

        private Dictionary<HistoryEvent, HistoryEvent> _parentEvents;
        private List<HistoryEvent> _horizontalEvents;
        private List<HistoryEvent> _verticalEvents;
        private Dictionary<HistoryEvent, int> _horizontalPosition;
        private Dictionary<HistoryEvent, int> _verticalPosition;

        private bool _fullRefresh;
        private HistoryEvent _lastAddedEvent;
        private ItemCollection _itemCollection;
    }
}
