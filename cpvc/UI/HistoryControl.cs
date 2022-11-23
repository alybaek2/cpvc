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
            _orderings = new HistoryEventOrderings(_history);

            GenerateTree();
        }

        public void ProcessHistoryChange(HistoryEvent e, HistoryChangedAction action)
        {
            if (_orderings.Process(e, action))
            {
                GenerateTree();
            }
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
                _updatePending = false;

                SetItems();
            };

            timer.Start();
        }

        public void SetItems()
        {
            if (_history == null)
            {
                Items.Clear();
                return;
            }

            List<HistoryViewItem> historyItems = _orderings.UpdateItems();

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
