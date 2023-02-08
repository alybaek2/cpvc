using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CPvC
{
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
            _firstGet = true;
            _tempPoints = new List<System.Windows.Point>();
        }

        private bool _firstGet;
        private const int _scalingX = 8;
        private const int _scalingY = 8;
        private const double _dotRadius = 0.5 * _scalingX;

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

            if (_tempPoints.Count <= _currentPointIndex)
            {
                _tempPoints.Add(new System.Windows.Point(_scalingX * x, _scalingY * y));
            }
            else
            {
                _tempPoints.Insert(_currentPointIndex, new System.Windows.Point(_scalingX * x, _scalingY * y));
            }

            //OnPropertyChanged(nameof(Points));
        }

        public void End()
        {
            if (_currentPointIndex != _points.Count)
            {
                _changed = true;
                _points.RemoveRange(_currentPointIndex, _points.Count - _currentPointIndex);

                while (_tempPoints.Count > _currentPointIndex)
                {
                    _tempPoints.RemoveAt(_currentPointIndex);
                }
            }

            if (_changed)
            {
                _tempPoints = _points.Select(p => new System.Windows.Point(_scalingX * p.X, _scalingY * p.Y)).ToList();
                //PointCollection pc = new PointCollection(_points.Select(p => new System.Windows.Point(_scalingX * p.X, _scalingY * p.Y)));
                //_shapePoints = pc;
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
                if (_firstGet)
                {
                    //_firstGet = false;
                    _shapePoints = new PointCollection(_tempPoints);
                    //_shapePoints = new PointCollection(_shapePoints);
                }
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
        private List<System.Windows.Point> _tempPoints;

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

    public class HistoryViewModel : INotifyPropertyChanged
    {
        private HistoryViewModel(History history)
        {
            _history = history;
            _listTree = new HistoryListTree(history);
            _listTree.PositionChanged += ListTree_PositionChanged;
            _nodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel>();

            _globalMaxX = 0;

            _lines = new ObservableCollection<HistoryLineViewModel>();
            BindingOperations.EnableCollectionSynchronization(_lines, _lines);
        }
        private void ListTree_PositionChanged(object sender, PositionChangedEventArgs<HistoryEvent> e)
        {
            UpdateLines(e.HorizontalOrdering, e.VerticalOrdering);
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void UpdateLines(List<ListTreeNode<HistoryEvent>> horizontalOrdering, List<ListTreeNode<HistoryEvent>> verticalOrdering)
        {
            Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel> newNodesToLines = new Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel>();
            Dictionary<int, int> maxXPerY = new Dictionary<int, int>();

            int globalMaxX = 0;
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
                    lock (_lines)
                    {
                        _lines.Add(line);
                    }
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
                    globalMaxX = Math.Max(globalMaxX, x);
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

                    globalMaxX = Math.Max(globalMaxX, x);

                    maxXPerY[y] = x + 2;
                }

                line.End();

                newNodesToLines.Add(node, line);
            }

            foreach (HistoryLineViewModel vm in _nodesToLines.Values)
            {
                if (!newNodesToLines.Values.Contains(vm))
                {
                    lock (_lines)
                    {
                        _lines.Remove(vm);
                    }
                }
            }

            _nodesToLines = newNodesToLines;

            _globalMaxX = globalMaxX;
            OnPropertyChanged(nameof(Width));
        }

        static HistoryViewModel()
        {
            _viewModels = new ConditionalWeakTable<History, HistoryViewModel>();
        }

        static public HistoryViewModel GetViewModel(History history)
        {
            lock (_viewModels)
            {
                if (_viewModels.TryGetValue(history, out HistoryViewModel viewModel))
                {
                    return viewModel;
                }

                viewModel = new HistoryViewModel(history);
                _viewModels.Add(history, viewModel);

                return viewModel;
            }
        }

        public int Width
        {
            get
            {
                return (_globalMaxX + 1) / 2;
            }
        }

        public History History
        {
            get
            {
                return _history;
            }
        }

        public ObservableCollection<HistoryLineViewModel> Lines
        {
            get
            {
                return _lines;
            }
        }

        private int _globalMaxX;
        private History _history;
        private Dictionary<ListTreeNode<HistoryEvent>, HistoryLineViewModel> _nodesToLines;

        private ObservableCollection<HistoryLineViewModel> _lines;

        private HistoryListTree _listTree;
        public event PropertyChangedEventHandler PropertyChanged;

        static private ConditionalWeakTable<History, HistoryViewModel> _viewModels;
    }
}
