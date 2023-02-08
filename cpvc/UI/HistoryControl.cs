using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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

            _linesToBranchShapes = new Dictionary<HistoryLineViewModel, BranchShapes>();
            _nodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel>();

            //DataContextChanged += HistoryControl_DataContextChanged;
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
            Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel> newNodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel>();
            Dictionary<int, int> maxXPerY = new Dictionary<int, int>();

            foreach (ListTreeNode<HistoryEvent> node in horizontalOrdering)
            {
                // Find the parent!
                ListTreeNode<HistoryEvent> parentNode = node.Parent;
                HistoryLineViewModel parentLine = null;
                if (parentNode != null)
                {
                    parentLine = newNodesToLines[parentNode];
                }

                if (!_nodesToLines.TryGetValue(node, out HistoryLineViewModel line))
                {
                    line = new HistoryLineViewModel();
                }

                // Draw!
                line.Start();

                // Need to set _changed to true if the following two things are different!
                line.Current = ReferenceEquals(node.Data, _history?.CurrentEvent);
                if (node.Data is RootHistoryEvent)
                {
                    line.Type = LinePointType.None;
                }
                else if (node.Data is BookmarkHistoryEvent bookmarkEvent)
                {
                    line.Type = bookmarkEvent.Bookmark.System ? LinePointType.SystemBookmark : LinePointType.UserBookmark;
                }
                else if (node.Data.Children.Count == 0 || node.Data is RootHistoryEvent)
                {
                    line.Type = LinePointType.Terminus;
                }
                else
                {
                    line.Type = LinePointType.None;
                }

                int previousX = -1;
                int x = 1;
                if (parentLine != null)
                {
                    Point parentPoint = parentLine.Points.Last();
                    line.Add(parentPoint.X, parentPoint.Y);
                    x = parentPoint.X;
                    previousX = x;
                }

                // What's our vertical ordering?
                int verticalIndex = verticalOrdering.FindIndex(n => ReferenceEquals(n, node));
                int parentVerticalIndex = verticalOrdering.FindIndex(n => ReferenceEquals(n, parentNode));

                for (int y = parentVerticalIndex + 1; y <= verticalIndex; y++)
                {
                    if (!maxXPerY.TryGetValue(y, out int maxX))
                    {
                        maxX = 1;
                        maxXPerY.Add(y, maxX);
                    }

                    x = Math.Max(x, maxX);

                    // If the x position has shifted, draw over to below the point, then up to the point. This looks nicer!
                    if (previousX >= 0 && previousX != x)
                    {
                        line.Add(x, 2 * y - 1);
                    }

                    line.Add(x, y * 2);
                    previousX = x;

                    maxXPerY[y] = x + 2;
                }

                line.End();

                newNodesToLines.Add(node, line);
            }

            _nodesToLines = newNodesToLines;
        }

        private void SyncLinesToShapes()
        {
            HashSet<HistoryLineViewModel> oldLines = new HashSet<HistoryLineViewModel>(_linesToBranchShapes.Keys);

            int maxX = 0;

            foreach (KeyValuePair<ListTreeNode<HistoryEvent>, HistoryLineViewModel> kvp in _nodesToLines)
            {
                HistoryLineViewModel line = kvp.Value;

                oldLines.Remove(line);

                if (!_linesToBranchShapes.TryGetValue(line, out BranchShapes branchShapes))
                {
                    branchShapes = CreateShapes();
                    _linesToBranchShapes.Add(line, branchShapes);
                    //_history._lines.Add(line);
                }
                else if (branchShapes.LineVersion == line._version)
                {
                    maxX = Math.Max(maxX, line.Points.Last().X);

                    continue;
                }

                branchShapes.Update(line);

                maxX = Math.Max(maxX, line.Points.Last().X);
            }

            // Delete non-existant lines!
            foreach (HistoryLineViewModel line in oldLines)
            {
                if (_linesToBranchShapes.TryGetValue(line, out BranchShapes bs))
                {
                    Children.Remove(bs.Dot);
                    Children.Remove(bs.Polyline);
                    _linesToBranchShapes.Remove(line);
                    //_history._lines.Remove(line);
                }
            }

            Height = _scalingY * 2 * (_listTree?.VerticalOrdering().Count ?? 0);
            Width = (maxX + 1) * _scalingX;
        }

        private void ScheduleUpdateCanvas(PositionChangedEventArgs<HistoryEvent> changeArgs)
        {
            if (Interlocked.Exchange(ref _updateArgs, changeArgs) != null)
            {
                return;
            }

            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            timer.Tick += (sender, args) =>
            {
                timer.Stop();

                PositionChangedEventArgs<HistoryEvent> updateArgs = Interlocked.Exchange(ref _updateArgs, null);
                UpdateLines(updateArgs.HorizontalOrdering, updateArgs.VerticalOrdering);
                SyncLinesToShapes();
            };

            timer.Start();
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

            public void Update(HistoryLineViewModel line)
            {
                UpdatePolyline(line);
                UpdateCircle(line);
                LineVersion = line._version;
            }

            private void UpdateCircle(HistoryLineViewModel line)
            {
                Point centre = line.Points.Last();
                LinePointType type = line.Type;

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
                Dot.Fill = line.Current ? Brushes.White : brush;
                Dot.Margin = new Thickness((centre.X * _scalingX) - _dotRadius, (centre.Y * _scalingY) - _dotRadius, 0, 0);
                Dot.Width = 2 * _dotRadius;
                Dot.Height = 2 * _dotRadius;
                Dot.Visibility = (type == LinePointType.None) ? Visibility.Collapsed : Visibility.Visible;
            }

            private void UpdatePolyline(HistoryLineViewModel line)
            {
                int addedPointsCount = 0;
                foreach (Point point in line.Points) // int pindex = 0; pindex < line._points.Count; pindex++)
                {
                    //Point point = line._points[pindex];
                    int scaledX = point.X * _scalingX;
                    int scaledY = point.Y * _scalingY;
                    if (addedPointsCount < Polyline.Points.Count)
                    {
                        if (Polyline.Points[addedPointsCount].X != scaledX || Polyline.Points[addedPointsCount].Y != scaledY)
                        {
                            Polyline.Points[addedPointsCount] = new System.Windows.Point(scaledX, scaledY);
                            
                        }
                    }
                    else
                    {
                        Polyline.Points.Add(new System.Windows.Point(scaledX, scaledY));
                    }

                    addedPointsCount++;
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

        public class HistoryLineViewModel : INotifyPropertyChanged
        {
            public HistoryLineViewModel()
            {
                _points = new List<Point>();
                _type = LinePointType.None;
                _current = false;
                _version = 0;
                _currentPointIndex = 0;
                _changed = false;
                _shapePoints = new PointCollection();
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

                if (_shapePoints.Count <= _currentPointIndex)
                {
                    _shapePoints.Add(new System.Windows.Point(_scalingX * x, _scalingY * y));
                }
                else
                {
                    _shapePoints.Insert(_currentPointIndex, new System.Windows.Point(_scalingX * x, _scalingY * y));
                }

                //OnPropertyChanged(nameof(Points));
            }

            public void End()
            {
                if (_currentPointIndex != _points.Count)
                {
                    _changed = true;
                    _points.RemoveRange(_currentPointIndex, _points.Count - _currentPointIndex);

                    while (_shapePoints.Count > _currentPointIndex)
                    {
                        _shapePoints.RemoveAt(_currentPointIndex);
                    }
                }

                if (_changed)
                {
                    PointCollection pc = new PointCollection(_points.Select(p => new System.Windows.Point(_scalingX * p.X, _scalingY * p.Y)));
                    _shapePoints = pc;
                    OnPropertyChanged(nameof(ShapePoints));

                    _version++;
                }

                OnPropertyChanged(nameof(DotMargin));
                //OnPropertyChanged(nameof(ShapePoints));
            }

            protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public Thickness DotMargin
            {
                get
                {
                    return new Thickness(_points.Last().X * _scalingX - _dotRadius, _points.Last().Y * _scalingY - _dotRadius, 0, 0);
                }
            }

            public PointCollection ShapePoints
            {
                get
                {
                    return _shapePoints;
                }
            }

            public ReadOnlyCollection<Point> Points
            {
                get
                {
                    return _points.AsReadOnly();
                }
            }

            public LinePointType Type
            {
                get
                {
                    return _type;
                }

                set
                {
                    if (_type == value)
                    {
                        return;
                    }

                    _type = value;
                    _changed = true;

                    OnPropertyChanged();
                }
            }

            public bool Current
            {
                get
                {
                    return _current;
                }

                set
                {
                    if (_current == value)
                    {
                        return;
                    }

                    _current = value;
                    _changed = true;

                    OnPropertyChanged();
                }
            }

            private List<Point> _points;
            private LinePointType _type;
            private bool _current;
            public int _version;
            private int _currentPointIndex;
            public bool _changed;
            private PointCollection _shapePoints;

            public event PropertyChangedEventHandler PropertyChanged;
        }

        public class Point : INotifyPropertyChanged
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }

            protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }

            public event PropertyChangedEventHandler PropertyChanged;
        }

        private Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel> _nodesToLines;
        private Dictionary<HistoryLineViewModel, BranchShapes> _linesToBranchShapes;
    }
}
