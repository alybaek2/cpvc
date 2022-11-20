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

        private HistoryEventOrderings _orderings;
        private bool _updatePending;


        public HistoryControl()
        {
            _orderings = null;
            _updatePending = false;

            DataContextChanged += HistoryControl_DataContextChanged;
        }

        private void HistoryControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.OldValue, e.NewValue))
            {
                return;
            }


            History oldHistory = (History)e.OldValue;
            if (oldHistory != null)
            {
                oldHistory.Auditors -= ProcessHistoryChange;
            }

            History newHistory = (History)e.NewValue;
            if (newHistory != null)
            {
                newHistory.Auditors += ProcessHistoryChange;
            }

            _history = newHistory;

            SetHistory(newHistory);
        }

        public void ProcessHistoryChange(HistoryEvent e, HistoryEvent formerParentEvent, HistoryChangedAction action)
        {
            _orderings.Process(this, e, formerParentEvent, action);
        }

        public void SetHistory(History history)
        {
            GenerateTree();
        }

        public void GenerateTree()
        {
            // Need some kind of lock here!
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
            if (_history == null)
            {
                Items.Clear();
                _updatePending = false;
                return;
            }

            HistoryEventOrderings oldOrderings = _orderings;

            // Probably need a lock here!
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
