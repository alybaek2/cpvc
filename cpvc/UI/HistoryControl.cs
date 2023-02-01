using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace CPvC
{
    public class HistoryControl : Canvas
    {
        private History _history;

        private HistoryListTree _listTree;

        private PositionChangedEventArgs<HistoryEvent> _updateArgs;

        private const int _scalingX = 8;
        private const int _scalingY = 8;

        private const double _dotRadius = 0.5 * _scalingX;

        public HistoryControl()
        {
            _updateArgs = null;

            _linesToBranchShapes = new Dictionary<Line, BranchShapes>();
            _nodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, Line>();

            DataContextChanged += HistoryControl_DataContextChanged;
        }


        private void HistoryControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
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

                    PositionChangedEventArgs<HistoryEvent> changeArgs = new PositionChangedEventArgs<HistoryEvent>(_listTree.HorizontalOrdering(), _listTree.VerticalOrdering());

                    ScheduleUpdateCanvas(changeArgs);
                }

            }
        }

        private void ListTree_PositionChanged(object sender, PositionChangedEventArgs<HistoryEvent> e)
        {
            ScheduleUpdateCanvas(e);
        }

        private void UpdateLines(List<ListTreeNode<HistoryEvent>> horizontalOrdering, List<ListTreeNode<HistoryEvent>> verticalOrdering)
        {
            Dictionary<ListTreeNode<HistoryEvent>, Line> newNodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, Line>();
            Dictionary<int, int> leftmost = new Dictionary<int, int>();

            foreach (ListTreeNode<HistoryEvent> node in horizontalOrdering)
            {
                // Find the parent!
                ListTreeNode<HistoryEvent> parentNode = node.Parent;
                Line parentLine = null;
                if (parentNode != null)
                {
                    parentLine = newNodesToLines[parentNode];
                }

                if (!_nodesToLines.TryGetValue(node, out Line line))
                {
                    line = new Line();
                }

                // Draw!

                line.Start();

                // Need to set _changed to true if the following two things are different!
                bool current = ReferenceEquals(node.Data, _history?.CurrentEvent);
                if (line._current != current)
                {
                    line._changed = true;
                }
                line._current = current;

                LinePointType oldType = line._type;
                line._type = LinePointType.None;
                if (node.Data is BookmarkHistoryEvent bookmarkEvent)
                {
                    line._type = bookmarkEvent.Bookmark.System ? LinePointType.SystemBookmark : LinePointType.UserBookmark;
                }
                else if (node.Data.Children.Count == 0 || node.Data is RootHistoryEvent)
                {
                    line._type = LinePointType.Terminus;
                }

                if (oldType != line._type)
                {
                    line._changed = true;
                }
                
                int maxLeft = 1;
                if (parentLine != null)
                {
                    Point parentPoint = parentLine._points.Last();
                    line.Add(parentPoint.X, parentPoint.Y);
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

                    line.Add(maxLeft, v * 2 * _scalingY);

                    leftmost[v] = maxLeft + 2 * _scalingX;
                }

                line.End();

                newNodesToLines.Add(node, line);
            }

            _nodesToLines = newNodesToLines;
        }

        private void SyncLinesToShapes()
        {
            HashSet<Line> oldLines = new HashSet<Line>(_linesToBranchShapes.Keys);

            foreach (KeyValuePair<ListTreeNode<HistoryEvent>, Line> kvp in _nodesToLines)
            {
                Line line = kvp.Value;

                oldLines.Remove(line);

                if (!_linesToBranchShapes.TryGetValue(line, out BranchShapes branchShapes))
                {
                    branchShapes = CreateShapes();
                    _linesToBranchShapes.Add(line, branchShapes);
                }
                else if (branchShapes.LineVersion == line._version)
                {
                    continue;
                }

                branchShapes.Update(line);
            }

            // Delete non-existant lines!
            foreach (Line line in oldLines)
            {
                if (_linesToBranchShapes.TryGetValue(line, out BranchShapes bs))
                {
                    Children.Remove(bs.Dot);
                    Children.Remove(bs.Polyline);
                    _linesToBranchShapes.Remove(line);
                }
            }

            Height = _scalingY * 2 * (_listTree?.VerticalOrdering().Count ?? 0);
        }

        private void ScheduleUpdateCanvas(PositionChangedEventArgs<HistoryEvent> changeArgs)
        {
            CPvC.Diagnostics.Trace("Setting updateARgs to something!");
            if (Interlocked.Exchange(ref _updateArgs, changeArgs) != null)
            {
                return;
            }

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();

                Stopwatch sw = Stopwatch.StartNew();
                UpdateCanvasListTree();
                CPvC.Diagnostics.Trace("Setting updateARgs to null");
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
            };

            timer.Start();
        }

        private void UpdateCanvasListTree()
        {
            PositionChangedEventArgs<HistoryEvent> changeArgs = Interlocked.Exchange(ref _updateArgs, null);

            UpdateLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            SyncLinesToShapes();
        }


        private BranchShapes CreateShapes()
        {
            BranchShapes branchShapes = new BranchShapes();
            Canvas.SetZIndex(branchShapes.Polyline, 1);
            Children.Add(branchShapes.Polyline);
            Canvas.SetZIndex(branchShapes.Dot, 100);
            Children.Add(branchShapes.Dot);

            return branchShapes;
        }

        private class BranchShapes
        {
            public BranchShapes()
            {
                Ellipse circle = new Ellipse
                {
                    Stroke = Brushes.DarkBlue,
                    Fill = Brushes.DarkBlue,
                    StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = true,
                    Margin = new Thickness(0, 0, 0, 0),
                    Width = 0,
                    Height = 0,
                    Visibility = Visibility.Collapsed
                };

                Polyline polyline = new Polyline
                {
                    StrokeThickness = 2,
                    Stroke = Brushes.DarkBlue,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = true
                };

                Polyline = polyline;
                Dot = circle;
                LineVersion = -1;
            }

            public Polyline Polyline { get; }
            public Ellipse Dot { get; }
            public int LineVersion { get; private set; }

            public void Update(Line line)
            {
                UpdatePolyline(line);
                UpdateCircle(line);
                LineVersion = line._version;
            }

            private void UpdateCircle(Line line)
            {
                Point centre = line._points.Last();
                LinePointType type = line._type;

                Brush brush;
                switch (type)
                {
                    case LinePointType.SystemBookmark:
                        brush = Brushes.DarkRed;
                        break;
                    case LinePointType.UserBookmark:
                        brush = Brushes.Crimson;
                        break;
                    default:
                        brush = Brushes.DarkBlue;
                        break;
                }

                Dot.Stroke = brush;
                Dot.Fill = line._current ? Brushes.White : brush;
                Dot.Margin = new Thickness(centre.X - _dotRadius, centre.Y - _dotRadius, 0, 0);
                Dot.Width = 2 * _dotRadius;
                Dot.Height = 2 * _dotRadius;
                Dot.Visibility = (type == LinePointType.None) ? Visibility.Collapsed : Visibility.Visible;
            }

            private void UpdatePolyline(Line line)
            {
                void AddPoint(PointCollection pointCollection, int index, int x, int y)
                {
                    if (index < pointCollection.Count)
                    {
                        if (pointCollection[index].X != x || pointCollection[index].Y != y)
                        {
                            pointCollection[index] = new System.Windows.Point(x, y);
                        }
                    }
                    else
                    {
                        pointCollection.Add(new System.Windows.Point(x, y));
                    }
                }

                int addedPointsCount = 0;
                int lastX = -1;
                for (int pindex = 0; pindex < line._points.Count; pindex++)
                {
                    Point point = line._points[pindex];
                    if (lastX >= 0 && lastX != point.X)
                    {
                        AddPoint(Polyline.Points, addedPointsCount, point.X, point.Y - 1 * _scalingY);
                        addedPointsCount++;
                    }

                    AddPoint(Polyline.Points, addedPointsCount, point.X, point.Y);
                    addedPointsCount++;

                    lastX = point.X;
                }

                // Trim any extra points.
                while (Polyline.Points.Count > addedPointsCount)
                {
                    Polyline.Points.RemoveAt(addedPointsCount);
                }
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
                _version = 0;
                _currentPointIndex = 0;
                _changed = false;
            }

            public void Start()
            {
                _currentPointIndex = 0;
                _changed = false;
            }

            public void Add(int x, int y)
            {
                if (_currentPointIndex < _points.Count)
                {
                    if (_points[_currentPointIndex].X == x && _points[_currentPointIndex].Y == y)
                    {
                        _currentPointIndex++;
                        return;
                    }
                }

                _points.Insert(_currentPointIndex, new Point(x, y));
                _currentPointIndex++;
                _changed = true;
            }

            public void End()
            {
                if (_currentPointIndex != _points.Count)
                {
                    _changed = true;
                    _points.RemoveRange(_currentPointIndex, _points.Count - _currentPointIndex);
                }

                if (_changed)
                {
                    _version++;
                }
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

            public List<Point> _points;
            public LinePointType _type;
            public bool _current;
            public int _version;
            private int _currentPointIndex;
            public bool _changed;
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

        private Dictionary<ListTreeNode<HistoryEvent>, Line> _nodesToLines;
        private Dictionary<Line, BranchShapes> _linesToBranchShapes;
    }
}
