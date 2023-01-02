using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CPvC
{
    //public class HistoryControl : ListView
    //{
    //    private History _history;

    //    private readonly HistoryEventOrderings _orderings;
    //    private volatile bool _updatePending;


    //    public HistoryControl()
    //    {
    //        _orderings = new HistoryEventOrderings();
    //        _updatePending = false;

    //        DataContextChanged += HistoryControl_DataContextChanged;
    //    }

    //    private void HistoryControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    //    {
    //        if (ReferenceEquals(e.OldValue, e.NewValue))
    //        {
    //            return;
    //        }

    //        History oldHistory = (History)e.OldValue;
    //        if (oldHistory != null)
    //        {
    //            oldHistory.Auditors -= ProcessHistoryChange;
    //        }

    //        History newHistory = (History)e.NewValue;
    //        if (newHistory != null)
    //        {
    //            newHistory.Auditors += ProcessHistoryChange;
    //        }

    //        _history = newHistory;

    //        lock (_orderings)
    //        {
    //            _orderings.SetHistory(_history);
    //            _updatePending = false;
    //        }

    //        ScheduleUpdateItems();
    //    }

    //    public void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
    //    {
    //        lock (_orderings)
    //        {
    //            if (_orderings.Process(args.HistoryEvent, args.Action))
    //            {
    //                ScheduleUpdateItems();
    //            }
    //        }
    //    }

    //    public void ScheduleUpdateItems()
    //    {
    //        if (_updatePending)
    //        {
    //            return;
    //        }

    //        _updatePending = true;

    //        DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
    //        timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
    //        timer.Tick += (sender, args) =>
    //        {
    //            timer.Stop();
    //            _updatePending = false;

    //            Stopwatch sw = Stopwatch.StartNew();
    //            UpdateItems();
    //            sw.Stop();

    //            CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
    //        };

    //        timer.Start();
    //    }

    //    public void UpdateItems()
    //    {
    //        if (_history == null)
    //        {
    //            Items.Clear();
    //            return;
    //        }

    //        List<HistoryViewItem> historyItems = null;
    //        lock (_orderings)
    //        {
    //            Stopwatch sw = Stopwatch.StartNew();
    //            historyItems = _orderings.UpdateItems();
    //            sw.Stop();

    //            CPvC.Diagnostics.Trace("Orderings UpdateItems took {0}ms", sw.ElapsedMilliseconds);
    //        }

    //        {
    //            Stopwatch sw = Stopwatch.StartNew();
    //            // Draw items to their respective canvasses.
    //            HistoryViewItem next = null;
    //            for (int i = historyItems.Count - 1; i >= 0; i--)
    //            {
    //                HistoryViewItem item = historyItems[i];
    //                item.Draw(next, _history.CurrentEvent);

    //                next = item;
    //            }
    //            sw.Stop();

    //            CPvC.Diagnostics.Trace("Drawing items took {0}ms", sw.ElapsedMilliseconds);
    //        }

    //        {
    //            Stopwatch sw = Stopwatch.StartNew();
    //            Items.Clear();
    //            for (int i = historyItems.Count - 1; i >= 0; i--)
    //            {
    //                Items.Add(historyItems[i]);
    //            }
    //            sw.Stop();

    //            CPvC.Diagnostics.Trace("Adding items took {0}ms", sw.ElapsedMilliseconds);
    //        }
    //    }
    //}
}
