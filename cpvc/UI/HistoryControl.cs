using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CPvC
{
    public class HistoryControl : ListView
    {
        private History _history;

        private HistoryViewNodeList _nodeList;
        private bool _updatePending;

        public static readonly DependencyProperty HistoryProperty =
            DependencyProperty.Register(
                "History",
                typeof(History),
                typeof(HistoryControl),
                new PropertyMetadata(null, PropertyChangedCallback));

        public HistoryControl()
        {
            _nodeList = new HistoryViewNodeList();
            _updatePending = false;
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            HistoryControl userControl = (HistoryControl)dependencyObject;
            userControl.History = (History)args.NewValue;
        }

        public History History
        {
            get
            {
                return (History)GetValue(HistoryProperty);
            }

            set
            {
                _history = value;
                SetValue(HistoryProperty, value);

                value.Auditors += ProcessHistoryChange;

                SetHistory(value);
            }
        }


        public void ProcessHistoryChange(HistoryEvent e, HistoryEvent formerParentEvent, HistoryChangedAction action)
        {
            lock (_nodeList)
            {
                bool refresh = false;

                switch (action)
                {
                    case HistoryChangedAction.Add:
                        _nodeList.Add(e);
                        refresh = true;
                        break;
                    case HistoryChangedAction.DeleteBranch:
                        _nodeList.Delete(e, formerParentEvent, true);
                        refresh = true;
                        break;
                    case HistoryChangedAction.DeleteBookmark:
                        _nodeList.Delete(e, formerParentEvent, false);
                        refresh = true;
                        break;
                    case HistoryChangedAction.UpdateCurrent:
                        refresh = _nodeList.Update(e);
                        break;
                }

                if (refresh)
                {
                    GenerateTree();
                }
            }
        }

        public void SetHistory(History history)
        {
            lock (_nodeList)
            {
                _nodeList.Add(_history.RootEvent);
                GenerateTree();
            }

        }

        public void GenerateTree()
        {
            lock (_nodeList)
            {
                if (_updatePending)
                {
                    return;
                }

                _updatePending = true;
            }

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();

                List<HistoryEvent> hz = null;
                List<HistoryEvent> vt = null;
                lock (_nodeList)
                {
                    hz = _nodeList.SortHorizontally(_history, _history.RootEvent);
                    vt = _nodeList.NodeList.ToList();
                    _updatePending = false;
                }

                SetItems(vt, hz);
            };

            timer.Start();
        }

        public void SetItems(List<HistoryEvent> verticalOrdering, List<HistoryEvent> horizontalOrdering)
        {
            Dictionary<HistoryEvent, int> horizontalLookup = new Dictionary<HistoryEvent, int>();
            Dictionary<HistoryEvent, int> verticalLookup = new Dictionary<HistoryEvent, int>();

            for (int i = 0; i < horizontalOrdering.Count; i++)
            {
                horizontalLookup[horizontalOrdering[i]] = i;
            }

            for (int i = 0; i < verticalOrdering.Count; i++)
            {
                verticalLookup[verticalOrdering[i]] = i;
            }

            List<HistoryViewItem> historyItems = new List<HistoryViewItem>();
            HistoryViewItem previousViewItem = null;

            for (int v = 0; v < verticalOrdering.Count; v++)
            {
                HistoryViewItem viewItem = new HistoryViewItem(verticalOrdering[v]);

                // Add events; either "passthrough" events, or the actual event for this HistoryViewItem.
                for (int h = 0; h < horizontalOrdering.Count; h++)
                {
                    if (!verticalLookup.ContainsKey(horizontalOrdering[h]))
                    {
                        // THis is usually the current node! WHy is it not in the vertical lookup??
                        continue;
                    }

                    int verticalHorizontalIndex = verticalLookup[horizontalOrdering[h]];
                    int previousVerticalIndex = -1;

                    HistoryEvent previousEvent = horizontalOrdering[h].Parent;
                    while (previousEvent != null)
                    {
                        if (verticalLookup.ContainsKey(previousEvent))
                        {
                            previousVerticalIndex = verticalLookup[previousEvent];
                            break;
                        }

                        previousEvent = previousEvent.Parent;
                    }

                    if (previousVerticalIndex < v && v <= verticalHorizontalIndex)
                    {
                        int hindex = viewItem.Events.Count;
                        if (previousViewItem != null)
                        {
                            int prevIndex = previousViewItem.Events.FindIndex(x => x == horizontalOrdering[h]);
                            if (prevIndex == -1)
                            {
                                prevIndex = previousViewItem.Events.FindIndex(x => x == previousEvent);
                            }

                            if (prevIndex != -1)
                            {
                                if (hindex < prevIndex)
                                {
                                    for (int g = 0; g < prevIndex - hindex; g++)
                                    {
                                        viewItem.Events.Add(null);
                                    }
                                }
                            }
                        }

                        viewItem.Events.Add(horizontalOrdering[h]);
                    }
                }

                historyItems.Add(viewItem);

                previousViewItem = viewItem;
            }

            // Draw items to their respective canvasses.
            HistoryViewItem next = null;
            for (int i = historyItems.Count - 1; i >= 0; i--)
            {
                HistoryViewItem item = historyItems[i];
                item.Draw(next, _history.CurrentEvent);

                next = item;
            }

            Items.Clear();
            for (int i = historyItems.Count - 1; i >= 0; i--)
            {
                Items.Add(historyItems[i]);
            }
        }

        public bool Update(HistoryEvent e)
        {
            if (!InterestingEvent(e))
            {
                return false;
            }

            lock (_nodeList)
            {
                // Has the vertical ordering changed?
                int verticalPosition = _nodeList.VerticalIndex(e);
                if (verticalPosition == -1)
                {
                    // Should really notify open history events and pass a paramter saying if its open or not?
                    CPvC.Diagnostics.Trace("Can't find node in vertical ordering! Rebuilding whole tree!");

                    GenerateTree();
                    return true;
                }

                bool change = false;
                if (verticalPosition >= 1)
                {
                    HistoryEvent previousEvent = _nodeList.NodeList[verticalPosition - 1];
                    if (VerticalSort(previousEvent, e) > 0)
                    {
                        CPvC.Diagnostics.Trace("Event is now less than previous event! Rebuilding whole tree!");
                        change = true;
                    }
                }

                if (verticalPosition < (_nodeList.NodeList.Count - 1))
                {
                    // OOps! _nodelist doesn't seem to be updating! This always sets change to true!
                    HistoryEvent previousEvent = _nodeList.NodeList[verticalPosition + 1];
                    if (VerticalSort(previousEvent, e) < 0)
                    {
                        CPvC.Diagnostics.Trace("Event is now more than next event! Rebuilding whole tree!");
                        change = true;
                    }
                }

                if (!change)
                {
                    return false;
                }

                GenerateTree();
            }

            return true;
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
                if (x.IsEqualToOrAncestorOf(y))
                {
                    return -1;
                }
                else if (y.IsEqualToOrAncestorOf(x))
                {
                    return 1;
                }
            }

            return 0;
        }

        static private bool InterestingEvent(HistoryEvent historyEvent)
        {
            bool interested = false;
            if (historyEvent is RootHistoryEvent)
            {
                interested = true;
            }
            else if (historyEvent is BookmarkHistoryEvent)
            {
                interested = true;
            }
            else if (historyEvent.Children.Count != 1)
            {
                interested = true;
            }

            return interested;
        }

    }
}
