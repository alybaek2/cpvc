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

        private HistoryListTree _listTree;

        private bool _updatePending;

        public HistoryControl2()
        {
            _updatePending = false;

            _branchLines = new Dictionary<History, BranchLine>();

            DataContextChanged += HistoryControl2_DataContextChanged;
        }



        private void HistoryControl2_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.OldValue, e.NewValue))
            {
                return;
            }

            History newHistory = (History)e.NewValue;
            _history = newHistory;

            {
                Children.Clear();

                if (_history == null)
                {
                    _listTree = null;
                }
                else
                {
                    _listTree = new HistoryListTree(_history);

                    _listTree.PositionChanged += ListTree_PositionChanged;

                    NotifyPositionChangedEventArgs changeArgs = new NotifyPositionChangedEventArgs(_listTree.HorizontalOrdering, _listTree.VerticalOrdering);

                    ScheduleUpdateCanvas(changeArgs);
                }

            }
        }

        private void ListTree_PositionChanged(object sender, NotifyPositionChangedEventArgs e)
        {
            ScheduleUpdateCanvas(e);
        }

        private Dictionary<ListTreeNode, BranchLine> DrawLines(List<ListTreeNode> horizontalOrdering, List<ListTreeNode> verticalOrdering)
        {
            Dictionary<ListTreeNode, BranchLine> lines = new Dictionary<ListTreeNode, BranchLine>();

            Dictionary<int, int> leftmost = new Dictionary<int, int>();

            foreach (ListTreeNode node in horizontalOrdering)
            {
                // Find the parent!
                ListTreeNode parentNode = node.Parent;
                BranchLine parentLine = null;
                if (parentNode != null)
                {
                    parentLine = lines[parentNode];
                }

                Point parentPoint = new Point(0, -1);
                if (parentLine != null)
                {
                    parentPoint = parentLine.Points[parentLine.Points.Count - 1];
                }

                // What's our vertical ordering?
                int verticalIndex = verticalOrdering.FindIndex(x => ReferenceEquals(x, node));

                // Draw!
                BranchLine line = new BranchLine();

                // Start with the parent!
                if (parentLine != null)
                {
                    //line.Points.Add(parentPoint);
                }

                int maxLeft = 0;
                for (int v = parentPoint.Y + 1; v <= verticalIndex; v++)
                {
                    if (!leftmost.TryGetValue(v, out int left))
                    {
                        left = 0;
                        leftmost.Add(v, left);
                    }

                    maxLeft = Math.Max(maxLeft, left);

                    line.Points.Add(new Point(maxLeft, v));

                    leftmost[v] = maxLeft + 1;
                }

                lines.Add(node, line);
            }

            return lines;
        }

        private void ScheduleUpdateCanvas(NotifyPositionChangedEventArgs changeArgs)
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
                UpdateCanvasListTree2(changeArgs);
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
            };

            timer.Start();
        }

        private void UpdateCanvasListTree2(NotifyPositionChangedEventArgs changeArgs)
        {
            Dictionary<ListTreeNode, BranchLine> lines = DrawLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            Children.Clear();

            foreach (KeyValuePair<ListTreeNode, BranchLine> kvp in lines)
            {
                ListTreeNode node = kvp.Key;
                BranchLine line = kvp.Value;

                Polyline polyline = new Polyline
                {
                    StrokeThickness = 2,
                    Stroke = Brushes.DarkBlue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = true
                };

                bool firstPoint = true;

                int xx = 0;
                BranchLine parentLine = null;
                if (node.Parent != null && lines.TryGetValue(node.Parent, out parentLine))
                {
                    Point pp = parentLine.Points[parentLine.Points.Count - 1];
                    double x = 16 * (pp.X + 0.5);
                    double y = 16 * (pp.Y + 0.5);

                    polyline.Points.Add(new System.Windows.Point(x, y));

                    //x = 16 * (line.Points[0].X + 0.5);
                    //y = 16 * line.Points[0].Y;
                    //polyline.Points.Add(new System.Windows.Point(x, y));

                    firstPoint = false;
                }

                for (int pindex = 0; pindex < line.Points.Count; pindex++)
                {
                    Point p = line.Points[pindex];

                    bool lastPoint2 = (pindex == line.Points.Count - 1);
                    double x = 16 * (p.X + 0.5);
                    double y = 16 * (p.Y + 0.5);
                    if (!firstPoint)
                    {
                        y = 16 * (p.Y + 0.0);
                    }

                    polyline.Points.Add(new System.Windows.Point(x, y));

                    //if (lastPoint2 && !firstPoint)
                    {
                        x = 16 * (p.X + 0.5);
                        y = 16 * (p.Y + 0.5);

                        polyline.Points.Add(new System.Windows.Point(x, y));
                    }
                }

                Children.Add(polyline);

                // Ensure lines are never "on top" of dots.
                Canvas.SetZIndex(polyline, 1);

                Point lastPoint = line.Points[line.Points.Count - 1];

                double radius = 0.25;
                double top = 0.5 - radius;

                const double _scalingX = 16;
                const double _scalingY = 16;

                bool filled = !ReferenceEquals(_history?.CurrentEvent, node.HistoryEvent);
                Ellipse circle = new Ellipse
                {
                    Stroke = Brushes.DarkBlue,
                    Fill = filled ? Brushes.DarkBlue : Brushes.White,
                    StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = true,
                    //Margin = new Thickness((lastPoint.X + 0.5) * 16 - 5, (lastPoint.Y + 0.5) * 16 - 5, 0, 0),
                    Margin = new Thickness(_scalingX * (lastPoint.X - radius + 0.5), _scalingY * (lastPoint.Y - radius + 0.5) , 0, 0),
                    Width = _scalingX * 2 * radius,
                    Height = _scalingY * 2 * radius
                };

                // Ensure the dot is always "on top".
                Canvas.SetZIndex(circle, 100);

                Children.Add(circle);

                Height = 16 * (_listTree?.VerticalOrdering.Count ?? 0);
            }
        }

        private Dictionary<History, BranchLine> _branchLines;

        private class BranchShapes
        {
            public BranchShapes()
            {
            }

            public Polyline Polyline
            {
                get;
                set;
            }

            public Ellipse Dot
            {
                get;
                set;
            }
        }

        private struct Point
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }
        }

        private class BranchLine
        {
            public BranchLine()
            {
                _points = new List<Point>();
            }

            public List<Point> Points
            {
                get
                {
                    return _points;
                }
            }

            private List<Point> _points;
        }
    }
}
