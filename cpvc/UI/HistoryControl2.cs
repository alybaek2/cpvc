using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CPvC
{
    public class HistoryControl2 : Canvas
    {
        private History _history;

        private readonly HistoryEventOrderings _orderings;

        private Dictionary<HistoryEvent, Polyline> _polylines;
        private Dictionary<HistoryEvent, Ellipse> _circles;

        private bool _updatePending;

        public HistoryControl2()
        {
            _orderings = new HistoryEventOrderings();
            _updatePending = false;
            _polylines = new Dictionary<HistoryEvent, Polyline>();
            _circles = new Dictionary<HistoryEvent, Ellipse>();

            DataContextChanged += HistoryControl2_DataContextChanged;
        }

        private void HistoryControl2_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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

            lock (_orderings)
            {
                _orderings.SetHistory(_history);
                //_updatePending = false;
                _circles.Clear();
                Children.Clear();
            }

            ScheduleUpdateCanvas();
        }

        public void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {
            lock (_orderings)
            {
                if (_orderings.Process(args.HistoryEvent, args.Action))
                {
                    ScheduleUpdateCanvas();
                }
            }
        }

        private void ScheduleUpdateCanvas()
        {
            if (_updatePending)
            {
                return;
            }

            _updatePending = true;

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                _updatePending = false;

                Stopwatch sw = Stopwatch.StartNew();
                UpdateCanvas();
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
            };

            timer.Start();


            //Dispatcher.BeginInvoke(new Action(() => {
            //    UpdateCanvas();
            //}), null);
        }

        private void UpdateCanvas()
        {
            lock (_orderings)
            {
                List<HistoryEvent> horizontalEvents = _orderings.HorizontalOrdering();
                for (int h = 0; h < horizontalEvents.Count; h++)
                {
                    HistoryEvent horizontalEvent = horizontalEvents[h];
                    HistoryEvent parent = _orderings.ParentEvent(horizontalEvent);
                    int parentVertical = (parent != null) ? _orderings.VerticalPosition(parent) : -1;

                    int vertical = _orderings.VerticalPosition(horizontalEvent);

                    bool filled = _history.CurrentEvent != horizontalEvent;
                    if (!_circles.TryGetValue(horizontalEvent, out Ellipse circle))
                    {
                        circle = new Ellipse
                        {
                            Stroke = Brushes.DarkBlue,
                            Fill = filled ? Brushes.DarkBlue : Brushes.White,
                            StrokeThickness = 2,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            UseLayoutRounding = true,
                            Margin = new Thickness(h * 16, vertical * 16, 0, 0),
                            Width = 10,
                            Height = 10
                        };

                        _circles.Add(horizontalEvent, circle);
                        Children.Add(circle);
                    }
                    else
                    {
                        circle.Margin = new Thickness(h * 16, vertical * 16, 0, 0);
                        circle.Fill = filled ? Brushes.DarkBlue : Brushes.White;
                    }

                    //if (!_polylines.TryGetValue(horizontalEvent, out Polyline polyline))
                    //{
                    //    polyline = new Polyline
                    //    {
                    //        StrokeThickness = 2,
                    //        Stroke = Brushes.DarkBlue,
                    //        HorizontalAlignment = HorizontalAlignment.Left,
                    //        VerticalAlignment = VerticalAlignment.Top,
                    //        //Points = { new Point(200, 200), new Point(240, 240) },
                    //        UseLayoutRounding = true
                    //    };

                    //    _polylines.Add(horizontalEvent, polyline);
                    //    Children.Add(polyline);

                    //    Canvas.SetZIndex(polyline, 1);

                    //}

                    //int p = 0;
                    //for (int v = parentVertical + 1; v <= vertical; v++)
                    //{
                    //    if (p < polyline.Points.Count)
                    //    {
                    //        polyline.Points[p] = new Point(h * 16, v * 16);
                    //        Diagnostics.Trace("Inserting point with {0},{1}", h * 16, v * 16);
                    //    }
                    //    else
                    //    {
                    //        polyline.Points.Add(new Point(h * 16, v * 16));
                    //        Diagnostics.Trace("Adding point with {0},{1}", h * 16, v * 16);
                    //    }

                    //    p++;
                    //}

                }

                // Get rid of any circles that correspond to history events that are no longer interesting!
                List<HistoryEvent> deleteCircles = _circles.Keys.ToList();
                foreach (HistoryEvent historyEvent in horizontalEvents)
                {
                    deleteCircles.Remove(historyEvent);
                    //if (!horizontalEvents.Contains(historyEvent))
                    //{

                    //}
                }

                foreach (HistoryEvent historyEvent in deleteCircles)
                {
                    Ellipse circle = _circles[historyEvent];
                    Children.Remove(circle);
                    _circles.Remove(historyEvent);
                }

                //Children.Clear();

                //foreach (Polyline p in _polylines.Values)
                //{
                //    Children.Add(p);
                //}
            }
        }
    }
}
