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

        private int _updatePending;
        private PositionChangedEventArgs<HistoryEvent> _updateArgs;

        private const int _scalingX = 8;
        private const int _scalingY = 8;

        public HistoryControl()
        {
            _updatePending = 0;
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
            Dictionary<int, int> leftmost = new Dictionary<int, int>();

            foreach (ListTreeNode<HistoryEvent> node in horizontalOrdering)
            {
                // Find the parent!
                ListTreeNode<HistoryEvent> parentNode = node.Parent;
                Line parentLine = null;
                if (parentNode != null)
                {
                    parentLine = _nodesToLines[parentNode];
                }

                if (!_nodesToLines.TryGetValue(node, out Line line))
                {
                    line = new Line();
                    _nodesToLines.Add(node, line);
                }

                // Draw!

                line.Start();

                //Line linepoints = new Line();
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
                    Point parentPoint = parentLine.LastPoint();
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
            }

            // Remove deleted lines
            List<ListTreeNode<HistoryEvent>> deletedNodes = _nodesToLines.Keys.Where(x => !horizontalOrdering.Contains(x)).ToList();
            foreach (ListTreeNode<HistoryEvent> node in deletedNodes)
            {
                _nodesToLines.Remove(node);
            }
        }

        private void SyncLinesToShapes()
        {
            double radius = 0.5 * _scalingX;

            foreach (KeyValuePair<ListTreeNode<HistoryEvent>, Line> kvp in _nodesToLines)
            {
                ListTreeNode<HistoryEvent> node = kvp.Key;
                Line line = kvp.Value;

                if (!_linesToBranchShapes.TryGetValue(line, out BranchShapes bs))
                {
                    bs = new BranchShapes();
                    _linesToBranchShapes.Add(line, bs);
                }
                else if (bs.LineVersion == line._version)
                {
                    continue;
                }

                Polyline polyline = CreatePolyline(line);
                Ellipse circle = CreateCircle(line.LastPoint(), radius, line._current, line._type);

                // Ensure the dot is always "on top".
                Canvas.SetZIndex(polyline, 1);
                Children.Add(polyline);

                if (circle != null)
                {
                    Canvas.SetZIndex(circle, 100);
                    Children.Add(circle);
                }

                Children.Remove(bs.Dot);
                Children.Remove(bs.Polyline);

                bs.Dot = circle;
                bs.Polyline = polyline;
                bs.LineVersion = line._version;
            }

            // Delete non-existant lines!
            List<Line> deleteLines = new List<Line>();
            foreach (Line line in _linesToBranchShapes.Keys)
            {
                if (_nodesToLines.Values.Contains(line))
                {
                    continue;
                }

                deleteLines.Add(line);
            }

            foreach (Line line in deleteLines)
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
            _updateArgs = changeArgs;
            if (Interlocked.Exchange(ref _updatePending, 1) != 0)
            //if (_updatePending)
            {
                //_updateArgs = changeArgs;
                return;
            }

            //_updatePending = 1;

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();

                Stopwatch sw = Stopwatch.StartNew();
                UpdateCanvasListTree(_updateArgs);
                _updateArgs = null;
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);

                Interlocked.Exchange(ref _updatePending, 0);
            };

            timer.Start();
        }

        private void UpdateCanvasListTree(PositionChangedEventArgs<HistoryEvent> changeArgs)
        {
            UpdateLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            SyncLinesToShapes();

            //Dictionary<ListTreeNode<HistoryEvent>, Line> lines = DrawLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            //Dictionary<ListTreeNode<HistoryEvent>, Line> newNodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, Line>();
            //Dictionary<Line, BranchShapes> newLinesToShapes = new Dictionary<Line, BranchShapes>();

            //int reused = 0;
            //double radius = 0.5 * _scalingX;

            //foreach (KeyValuePair<ListTreeNode<HistoryEvent>, Line> kvp in lines)
            //{
            //    ListTreeNode<HistoryEvent> node = kvp.Key;
            //    Line line = kvp.Value;

            //    bool current = ReferenceEquals(_history?.CurrentEvent, node.HistoryEvent);

            //    // Check the old ones!
            //    if (_nodesToLines.TryGetValue(node, out Line oldLine))
            //    {
            //        if (line.IsSame(oldLine))
            //        {
            //            // Just use the old one
            //            reused++;
            //            newNodesToLines.Add(node, oldLine);
            //            newLinesToShapes.Add(oldLine, _linesToBranchShapes[oldLine]);
            //            continue;
            //        }
            //    }

            //    // Ensure lines are never "on top" of dots.
            //    Point lastPoint = line.LastPoint();

            //    Polyline polyline = CreatePolyline(line);
            //    Ellipse circle = CreateCircle(lastPoint, radius, current, line._type);

            //    // Ensure the dot is always "on top".
            //    Canvas.SetZIndex(polyline, 1);
            //    Children.Add(polyline);

            //    if (circle != null)
            //    {
            //        Canvas.SetZIndex(circle, 100);
            //        Children.Add(circle);
            //    }

            //    Height = _scalingY * 2 * (_listTree?.VerticalOrdering().Count ?? 0);

            //    BranchShapes bs = new BranchShapes();
            //    bs.Dot = circle;
            //    bs.Polyline = polyline;
            //    newNodesToLines.Add(node, line);
            //    newLinesToShapes.Add(line, bs);
            //}

            //// Remove any shapes!
            //int del = 0;
            //foreach (KeyValuePair<ListTreeNode<HistoryEvent>, Line> kvp in _nodesToLines)
            //{
            //    if (newNodesToLines.ContainsValue(kvp.Value))
            //    {
            //        continue;
            //    }

            //    if (_linesToBranchShapes.TryGetValue(kvp.Value, out BranchShapes bs))
            //    {
            //        //BranchShapes bs = kvp.Value.Item2;
            //        Children.Remove(bs.Dot);
            //        Children.Remove(bs.Polyline);

            //        _linesToBranchShapes.Remove(kvp.Value);
            //        del++;
            //    }
            //    else
            //    {
            //        throw new Exception();
            //    }
            //}

            //CPvC.Diagnostics.Trace("UpdateCanvas: {0} reused {1} deleted {2} total", reused, del, lines.Count);
            //_nodesToLines = newNodesToLines;
            //_linesToBranchShapes = newLinesToShapes;
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

            int lastX = -1;
            for (int pindex = 0; pindex < line._points.Count; pindex++)
            {
                if (lastX >= 0)
                {
                    polyline.Points.Add(new System.Windows.Point(line._points[pindex].X, line._points[pindex].Y - 1 * _scalingY));
                }
                polyline.Points.Add(new System.Windows.Point(line._points[pindex].X, line._points[pindex].Y));

                lastX = line._points[pindex].X;
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

            public int LineVersion
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
                //if (_points.Count == 0)
                //{
                //    _points.Add(new Point(x, y));
                //    _changed = true;
                //    _currentPointIndex = 1;
                //    return;
                //}
                //else if (_points.Count == 1)
                //{
                //    if (_points[0].X != x || _points[0].Y != y)
                //    {
                //        _changed = true;
                //        _points[0] = new Point(x, y);
                //        _currentPointIndex = 1;
                //        return;
                //    }
                //}

                if (_currentPointIndex < _points.Count)
                {
                    if (_points[_currentPointIndex].X == x && _points[_currentPointIndex].Y == y)
                    {
                        _currentPointIndex++;
                        return;
                    }

                    //_points[_currentPointIndex] = new Point(x, y);
                    //_changed = true;
                    //return;
                }

                _points.Insert(_currentPointIndex, new Point(x, y));
                _currentPointIndex++;
                _changed = true;

                //Point currPoint = _points[_currentPointIndex];

                //if (currPoint.X == x && currPoint.Y == y)
                //{
                //    _currentPointIndex++;
                //    return;
                //}

                //Point prevPoint = _points[_currentPointIndex - 1];
                //if (currPoint.X == prevPoint.X && currPoint.X == x && prevPoint.Y <= y && y < currPoint.Y)
                //{
                //    return;
                //}

                //// Add a new point
                //if (_currentPointIndex >= 2)
                //{
                //    Point prevPrevPoint = _points[_currentPointIndex - 2];

                //    if (prevPrevPoint.X == prevPoint.X && prevPoint.X == x)
                //    {
                //        _points[_currentPointIndex - 1] = new Point(x, y);
                //        _changed = true;
                //        return;
                //    }
                //}

                //if (prevPoint.X != x)
                //{
                //    _points.Insert(_currentPointIndex, new Point(x, y - 1 * _scalingY));
                //    _currentPointIndex++;
                //}

                //_points.Insert(_currentPointIndex, new Point(x, y));
                //_currentPointIndex++;

                //_changed = true;

                //////if (_points.Count)
                ////if (_points.Any())
                ////{
                ////    Point lastPoint = _points[_points.Count - 1];
                ////    if (_points.Count >= 2)
                ////    {
                ////        Point penultimatePoint = _points[_points.Count - 2];

                ////        if (penultimatePoint.X == lastPoint.X && lastPoint.X == x)
                ////        {
                ////            _points[_points.Count - 1] = new Point(x, y);
                ////            return;
                ////        }
                ////    }

                ////    if (lastPoint.X != x)
                ////    {
                ////        _points.Add(new Point(x, y - 1 * _scalingY));
                ////    }
                ////}

                ////_points.Add(new Point(x, y));
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

            public Point LastPoint()
            {
                return _points[_points.Count - 1];
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
