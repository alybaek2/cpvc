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

                SetItems();
            };

            timer.Start();
        }

        public void SetItems()
        {
            HistoryEventOrderings orderings = null;
            //List<HistoryEvent> verticalOrdering = null;
            //List<HistoryEvent> horizontalOrdering = null;
            lock (_nodeList)
            {
                orderings = new HistoryEventOrderings(_history);
                //horizontalOrdering = _nodeList.SortHorizontally(_history, _history.RootEvent);
                //verticalOrdering = _nodeList.NodeList.ToList();
                _updatePending = false;
            }

            List<HistoryViewItem> historyItems = new List<HistoryViewItem>();
            //for (int v = 0; v < verticalOrdering.Count; v++)
            //{
            //    historyItems.Add(new HistoryViewItem(verticalOrdering[v]));
            //}

            historyItems = orderings.GetVerticallySorted().Select(x => new HistoryViewItem(x)).ToList();

            foreach (HistoryEvent horizontalEvent in orderings.GetHorizonallySorted())
            //for (int h = 0; h < horizontalOrdering.Count; h++)
            {
                //HistoryEvent historyEvent = horizontalOrdering[h];

                int v = orderings.GetVerticalPosition(horizontalEvent);
                //int v = verticalOrdering.FindIndex(x => ReferenceEquals(x, horizontalEvent));
                //if (v < 0)
                //{
                //    throw new Exception("Couldn't find history event in vertical ordering.");
                //}

                // Find the parent HistoryViewItem... there must be a more efficient way of doing this!
                //int pv = -1;
                //int ph = 0;
                //for (pv = orderings.Count() - 1; pv >= 0; pv--)
                //{
                //    HistoryEvent parentEvent = orderings.GetParent(horizontalEvent);
                //    if (verticalOrdering[pv].IsEqualToOrAncestorOf(horizontalEvent) && !ReferenceEquals(horizontalEvent, verticalOrdering[pv]))
                //    {
                //        ph = historyItems[pv].Events.FindIndex(x => ReferenceEquals(x, verticalOrdering[pv]));
                //        //ph = horizontalOrdering.FindIndex(x => ReferenceEquals(x, verticalOrdering[pv]));
                //        break;
                //    }
                //}

                HistoryEvent parentEvent = orderings.GetParent(horizontalEvent);
                int pv = parentEvent != null ? orderings.GetVerticalPosition(parentEvent) : -1;
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
    }
}
