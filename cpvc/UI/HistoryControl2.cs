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

        private const int _scalingX = 8;
        private const int _scalingY = 8;

        public HistoryControl2()
        {
            _updatePending = false;

            //_branchLines = new Dictionary<History, BranchLine>();
            //_branchShapes = new Dictionary<ListTreeNode, Tuple<Line, BranchShapes>>();
            _linesToBranchShapes = new Dictionary<Line, BranchShapes>();
            //_branchShapes = new Dictionary<ListTreeNode, Tuple<Line, BranchShapes>>();
            _nodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, Line>();
            _linesToBranchShapes = new Dictionary<Line, BranchShapes>();

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

                    NotifyPositionChangedEventArgs<HistoryEvent> changeArgs = new NotifyPositionChangedEventArgs<HistoryEvent>(_listTree.HorizontalOrdering(), _listTree.VerticalOrdering());

                    ScheduleUpdateCanvas(changeArgs);
                }

            }
        }

        private void ListTree_PositionChanged(object sender, NotifyPositionChangedEventArgs<HistoryEvent> e)
        {
            ScheduleUpdateCanvas(e);
        }

        private Dictionary<ListTreeNode<HistoryEvent>, Line> DrawLines(List<ListTreeNode<HistoryEvent>> horizontalOrdering, List<ListTreeNode<HistoryEvent>> verticalOrdering)
        {
            Dictionary<ListTreeNode<HistoryEvent>, Line> lines = new Dictionary<ListTreeNode<HistoryEvent>, Line>();

            Dictionary<int, int> leftmost = new Dictionary<int, int>();

            foreach (ListTreeNode<HistoryEvent> node in horizontalOrdering)
            {
                // Find the parent!
                ListTreeNode<HistoryEvent> parentNode = node.Parent;
                Line parentLine = null;
                if (parentNode != null)
                {
                    parentLine = lines[parentNode];
                }

                // Draw!
                Line linepoints = new Line();
                linepoints._current = ReferenceEquals(node.HistoryEvent, _history?.CurrentEvent);
                linepoints._type = LinePointType.None;
                if (node.HistoryEvent is BookmarkHistoryEvent bookmarkEvent)
                {
                    linepoints._type = bookmarkEvent.Bookmark.System ? LinePointType.SystemBookmark : LinePointType.UserBookmark;
                }
                else if (node.HistoryEvent.Children.Count == 0 || node.HistoryEvent is RootHistoryEvent)
                {
                    linepoints._type = LinePointType.Terminus;
                }

                int maxLeft = 1;
                if (parentLine != null)
                {
                    Point parentPoint = parentLine.LastPoint(); // ._points[parentLine._points.Count - 1];
                    linepoints.Add(parentPoint.X, parentPoint.Y);
                    maxLeft = parentPoint.X;
                }

                // What's our vertical ordering?
                int verticalIndex = verticalOrdering.FindIndex(x => ReferenceEquals(x, node));
                int parentVerticalIndex = verticalOrdering.FindIndex(x => ReferenceEquals(x, parentNode));

                for (int v = parentVerticalIndex + 1; v <= verticalIndex; v++)
                {
                    if (!leftmost.TryGetValue(v, out int left))
                    {
                        left = 1 * _scalingX;
                        leftmost.Add(v, left);
                    }

                    maxLeft = Math.Max(maxLeft, left);

                    linepoints.Add(maxLeft, v * 2 * _scalingY);

                    leftmost[v] = maxLeft + 2 * _scalingX;
                }

                lines.Add(node, linepoints);
            }

            return lines;
        }

        private void ScheduleUpdateCanvas(NotifyPositionChangedEventArgs<HistoryEvent> changeArgs)
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
                UpdateCanvasListTree(changeArgs);
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
            };

            timer.Start();
        }

        private void UpdateCanvasListTree(NotifyPositionChangedEventArgs<HistoryEvent> changeArgs)
        {
            Dictionary<ListTreeNode<HistoryEvent>, Line> lines = DrawLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            Dictionary<ListTreeNode<HistoryEvent>, Line> newNodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, Line>();
            Dictionary<Line, BranchShapes> newLinesToShapes = new Dictionary<Line, BranchShapes>();

            int reused = 0;
            double radius = 0.5 * _scalingX;

            foreach (KeyValuePair<ListTreeNode<HistoryEvent>, Line> kvp in lines)
            {
                ListTreeNode<HistoryEvent> node = kvp.Key;
                Line line = kvp.Value;

                bool current = ReferenceEquals(_history?.CurrentEvent, node.HistoryEvent);

                // Check the old ones!
                if (_nodesToLines.TryGetValue(node, out Line oldLine))
                {
                    if (line.IsSame(oldLine))
                    {
                        // Just use the old one
                        reused++;
                        newNodesToLines.Add(node, oldLine);
                        newLinesToShapes.Add(oldLine, _linesToBranchShapes[oldLine]);
                        continue;
                    }
                }

                // Ensure lines are never "on top" of dots.
                Point lastPoint = line.LastPoint();

                Polyline polyline = CreatePolyline(line);
                Ellipse circle = CreateCircle(lastPoint, radius, current, line._type);

                // Ensure the dot is always "on top".
                Canvas.SetZIndex(polyline, 1);
                Children.Add(polyline);

                if (circle != null)
                {
                    Canvas.SetZIndex(circle, 100);
                    Children.Add(circle);
                }

                Height = _scalingY * 2 * (_listTree?.VerticalOrdering().Count ?? 0);

                BranchShapes bs = new BranchShapes();
                bs.Dot = circle;
                bs.Polyline = polyline;
                newNodesToLines.Add(node, line);
                newLinesToShapes.Add(line, bs);
                //newShapes.Add(node, new Tuple<Line, BranchShapes>(line, bs));
            }

            // Remove any shapes!
            int del = 0;
            foreach (KeyValuePair<ListTreeNode<HistoryEvent>, Line> kvp in _nodesToLines)
            {
                if (newNodesToLines.ContainsValue(kvp.Value))
                {
                    continue;
                }

                if (_linesToBranchShapes.TryGetValue(kvp.Value, out BranchShapes bs))
                {
                    //BranchShapes bs = kvp.Value.Item2;
                    Children.Remove(bs.Dot);
                    Children.Remove(bs.Polyline);

                    _linesToBranchShapes.Remove(kvp.Value);
                    del++;
                }
                else
                {
                    throw new Exception();
                }
            }

            CPvC.Diagnostics.Trace("UpdateCanvas: {0} reused {1} deleted {2} total", reused, del, lines.Count);
            _nodesToLines = newNodesToLines;
            _linesToBranchShapes = newLinesToShapes;
        }

        private Ellipse CreateCircle(Point centre, double radius, bool current, LinePointType type)
        {
            Brush brush;
            switch (type)
            {
                case LinePointType.None:
                    return null;
                case LinePointType.SystemBookmark:
                    brush = Brushes.DarkRed;
                    break;
                case LinePointType.UserBookmark:
                    brush = Brushes.Red;
                    break;
                default:
                    brush = Brushes.DarkBlue;
                    break;
            }

            Ellipse circle = new Ellipse
            {
                Stroke = brush,
                Fill = current ? Brushes.White : brush,
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = true,
                Margin = new Thickness(centre.X - radius, centre.Y - radius, 0, 0),
                Width = 2 * radius,
                Height = 2 * radius
            };

            return circle;
        }

        private Polyline CreatePolyline(Line line)
        {
            Polyline polyline = new Polyline
            {
                StrokeThickness = 2,
                Stroke = Brushes.DarkBlue,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = true
            };

            for (int pindex = 0; pindex < line._points.Count; pindex++)
            {
                polyline.Points.Add(new System.Windows.Point(line._points[pindex].X, line._points[pindex].Y));
            }

            return polyline;
        }

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

        public enum LinePointType
        {
            None,
            Terminus,
            SystemBookmark,
            UserBookmark
        }

        private class Line
        {
            public Line()
            {
                _points = new List<Point>();
                _type = LinePointType.None;
                _current = false;
            }

            public void Start(int x, int y)
            {
                _points.Clear();
                _points.Add(new Point(x, y));
            }

            public void Add(int x, int y)
            {
                if (_points.Any())
                {
                    Point lastPoint = _points[_points.Count - 1];
                    if (_points.Count >= 2)
                    {
                        Point penultimatePoint = _points[_points.Count - 2];

                        if (penultimatePoint.X == lastPoint.X && lastPoint.X == x)
                        {
                            _points[_points.Count - 1] = new Point(x, y);
                            return;
                        }
                    }

                    if (lastPoint.X != x)
                    {
                        _points.Add(new Point(x, y - 1 * _scalingY));
                    }
                }

                _points.Add(new Point(x, y));
            }

            public bool IsSame(Line line)
            {
                if (_current != line._current)
                {
                    return false;
                }

                if (_type != line._type)
                {
                    return false;
                }

                if (_points.Count != line._points.Count)
                {
                    return false;
                }

                for (int i = 0; i < line._points.Count; i++)
                {
                    if (_points[i].X != line._points[i].X ||
                        _points[i].Y != line._points[i].Y)
                    {
                        return false;
                    }
                }

                return true;
            }

            public Point LastPoint()
            {
                return _points[_points.Count - 1];
            }

            public List<Point> _points;
            public LinePointType _type;
            public bool _current;
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

        //private Dictionary<ListTreeNode, Tuple<Line, BranchShapes>> _branchShapes;
        private Dictionary<ListTreeNode<HistoryEvent>, Line> _nodesToLines;
        private Dictionary<Line, BranchShapes> _linesToBranchShapes;

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
