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
        private HistoryEventOrderings _orderings;
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
            _orderings = null;
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
                        refresh = _orderings?.VerticalPositionChanged(e) ?? true;
                        //refresh = _nodeList.Update(e);
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
            //_orderings = null;
            lock (_nodeList)
            {
                _orderings = new HistoryEventOrderings(_history);
                _updatePending = false;
            }

            List<HistoryViewItem> historyItems = _orderings.GetVerticallySorted().Select(x => new HistoryViewItem(x)).ToList();

            foreach (HistoryEvent horizontalEvent in _orderings.GetHorizonallySorted())
            {
                int v = _orderings.GetVerticalPosition(horizontalEvent);

                HistoryEvent parentEvent = _orderings.GetParent(horizontalEvent);
                int pv = parentEvent != null ? _orderings.GetVerticalPosition(parentEvent) : -1;
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
