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

            //_branchLines = new Dictionary<History, BranchLine>();

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

        private Dictionary<ListTreeNode, Line> DrawLines(List<ListTreeNode> horizontalOrdering, List<ListTreeNode> verticalOrdering)
        {
            Dictionary<ListTreeNode, Line> lines = new Dictionary<ListTreeNode, Line>();

            Dictionary<int, int> leftmost = new Dictionary<int, int>();

            foreach (ListTreeNode node in horizontalOrdering)
            {
                // Find the parent!
                ListTreeNode parentNode = node.Parent;
                Line parentLine = null;
                if (parentNode != null)
                {
                    parentLine = lines[parentNode];
                }

                // Draw!
                Line linepoints = new Line();

                void AddPoint(Point p)
                {
                    int pindex = linepoints._points.Count;
                    if (pindex > 0 && linepoints._points[pindex - 1].X != p.X)
                    {
                        linepoints.Add(p.X, p.Y - 1);
                    }

                    linepoints.Add(p.X, p.Y);
                }

                Point parentPoint = new Point(1, -2);
                if (parentLine != null)
                {
                    parentPoint = parentLine._points[parentLine._points.Count - 1];

                    AddPoint(new Point(parentPoint.X, parentPoint.Y));
                }

                // What's our vertical ordering?
                int verticalIndex = verticalOrdering.FindIndex(x => ReferenceEquals(x, node));
                int parentVerticalIndex = verticalOrdering.FindIndex(x => ReferenceEquals(x, parentNode));

                int maxLeft = parentPoint.X;
                //for (int v = parentPoint.Y + 2; v <= 2 * verticalIndex; v += 2)
                for (int v = parentVerticalIndex + 1; v <= verticalIndex; v += 1)
                {
                    if (!leftmost.TryGetValue(v, out int left))
                    {
                        left = 1;
                        leftmost.Add(v, left);
                    }

                    maxLeft = Math.Max(maxLeft, left);

                    AddPoint(new Point(maxLeft, v * 2));

                    leftmost[v] = maxLeft + 2;
                }

                lines.Add(node, linepoints);
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
            Dictionary<ListTreeNode, Line> lines = DrawLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            Children.Clear();

            foreach (KeyValuePair<ListTreeNode, Line> kvp in lines)
            {
                ListTreeNode node = kvp.Key;
                Line line = kvp.Value;

                Polyline polyline = new Polyline
                {
                    StrokeThickness = 2,
                    Stroke = Brushes.DarkBlue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = true
                };

                const double _scalingX = 8;
                const double _scalingY = 8;


                for (int pindex = 0; pindex < line._points.Count; pindex++)
                {
                    polyline.Points.Add(new System.Windows.Point(_scalingX * line._points[pindex].X, _scalingY * line._points[pindex].Y));
                }

                Children.Add(polyline);

                // Ensure lines are never "on top" of dots.
                Canvas.SetZIndex(polyline, 1);

                Point lastPoint = line._points[line._points.Count - 1];

                double radius = 0.5;

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
                    Margin = new Thickness(_scalingX * (lastPoint.X - radius), _scalingY * (lastPoint.Y - radius) , 0, 0),
                    Width = _scalingX * 2 * radius,
                    Height = _scalingY * 2 * radius
                };

                // Ensure the dot is always "on top".
                Canvas.SetZIndex(circle, 100);

                Children.Add(circle);

                Height = 16 * (_listTree?.VerticalOrdering.Count ?? 0);
            }
        }

        //private Dictionary<History, BranchLine> _branchLines;

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

        private class Line
        {
            public Line()
            {
                _points = new List<Point>();
            }

            public void Add(int x, int y)
            {
                if (_points.Count >= 2)
                {
                    Point lastPoint = _points[_points.Count - 1];
                    Point penultimatePoint = _points[_points.Count - 2];

                    if (penultimatePoint.X == lastPoint.X && lastPoint.X == x)
                    {
                        _points[_points.Count - 1] = new Point(x, y);
                        return;
                    }
                }

                _points.Add(new Point(x, y));
            }

            public List<Point> _points;
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

        //private class BranchLine
        //{
        //    public BranchLine()
        //    {
        //        _points = new List<Point>();
        //    }

        //    public List<Point> Points
        //    {
        //        get
        //        {
        //            return _points;
        //        }
        //    }

        //    private List<Point> _points;
        //}
    }
}
