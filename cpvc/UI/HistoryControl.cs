﻿using System;
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

                Polyline polyline;
                Ellipse circle;
                if (_linesToBranchShapes.TryGetValue(line, out BranchShapes bs))
                {
                    if (bs.LineVersion == line._version)
                    {
                        continue;
                    }

                    polyline = bs.Polyline;
                    circle = bs.Dot;
                }
                else
                {
                    polyline = CreatePolyline();
                    Canvas.SetZIndex(polyline, 1);
                    Children.Add(polyline);

                    circle = CreateCircle();
                    Canvas.SetZIndex(circle, 100);
                    Children.Add(circle);

                    bs = new BranchShapes(polyline, circle);
                    _linesToBranchShapes.Add(line, bs);
                }

                UpdatePolyline(polyline, line);
                UpdateCircle(circle, line._points.Last(), radius, line._current, line._type);

                bs.LineVersion = line._version;
            }

            // Delete non-existant lines!
            List<Line> deleteLines = _linesToBranchShapes.Keys.Where(x => !_nodesToLines.Values.Contains(x)).ToList();

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
            {
                return;
            }

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();

                Interlocked.Exchange(ref _updatePending, 0);

                Stopwatch sw = Stopwatch.StartNew();
                UpdateCanvasListTree(_updateArgs);
                _updateArgs = null;
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
            };

            timer.Start();
        }

        private void UpdateCanvasListTree(PositionChangedEventArgs<HistoryEvent> changeArgs)
        {
            UpdateLines(changeArgs.HorizontalOrdering, changeArgs.VerticalOrdering);

            SyncLinesToShapes();
        }

        private Ellipse CreateCircle()
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

            return circle;
        }

        private void UpdateCircle(Ellipse circle, Point centre, double radius, bool current, LinePointType type)
        {
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

            circle.Stroke = brush;
            circle.Fill = current ? Brushes.White : brush;
            circle.Margin = new Thickness(centre.X - radius, centre.Y - radius, 0, 0);
            circle.Width = 2 * radius;
            circle.Height = 2 * radius;
            circle.Visibility = (type == LinePointType.None) ? Visibility.Collapsed : Visibility.Visible;
        }

        private Polyline CreatePolyline()
        {
            Polyline polyline = new Polyline
            {
                StrokeThickness = 2,
                Stroke = Brushes.DarkBlue,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = true
            };

            return polyline;
        }

        private void UpdatePolyline(Polyline polyline, Line line)
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

            int pp = 0;
            int lastX = -1;
            for (int pindex = 0; pindex < line._points.Count; pindex++)
            {
                Point point = line._points[pindex];
                if (lastX >= 0)
                {
                    AddPoint(polyline.Points, pp, point.X, point.Y - 1 * _scalingY);
                    pp++;
                }

                AddPoint(polyline.Points, pp, point.X, point.Y);
                pp++;

                lastX = line._points[pindex].X;
            }

            // Trim any extra points.
            while (polyline.Points.Count > pp)
            {
                polyline.Points.RemoveAt(pp);
            }
        }

        private class BranchShapes
        {
            public BranchShapes(Polyline polyline, Ellipse dot)
            {
                Polyline = polyline;
                Dot = dot;
                LineVersion = -1;
            }

            public Polyline Polyline
            {
                get;
            }

            public Ellipse Dot
            {
                get;
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