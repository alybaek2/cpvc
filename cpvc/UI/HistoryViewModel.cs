using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;

namespace CPvC
{
    public enum LinePointType
    {
        None,
        Action,
        SystemBookmark,
        UserBookmark
    }

    public enum SelectionMode
    {
        New,
        Toggle
    }

    public enum SelectionState
    {
        None,
        Node,
        Branch,
        NodeAndBranch
    }

    public class HistoryLineViewModel : INotifyPropertyChanged
    {
        public HistoryLineViewModel(HistoryViewModel historyViewModel, HistoryEvent node)
        {
            _points = new List<Point>();
            _type = LinePointType.None;
            _current = false;
            _version = 0;
            _currentPointIndex = 0;
            _changed = false;
            _node = node;
            _selectionState = SelectionState.None;
            _historyViewModel = historyViewModel;
        }

        private HistoryEvent _node;

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
                OnPropertyChanged(nameof(PolyLinePoints));
                OnPropertyChanged(nameof(Center));

                _version++;
            }
        }

        public void Select(SelectionMode mode)
        {
            _historyViewModel.Select(mode, this);
        }

        public void SelectBranch()
        {
            _historyViewModel.SelectBranch(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public HistoryEvent Node
        {
            get
            {
                return _node;
            }
        }

        public System.Windows.Point Center
        {
            get
            {
                return new System.Windows.Point(_points.Last().X, _points.Last().Y);
            }
        }

        public PointCollection PolyLinePoints
        {
            get
            {
                return new PointCollection(_points.Select(p => new System.Windows.Point(p.X, p.Y)));
            }
        }

        public bool LineSelected
        {
            get
            {
                return _selectionState == SelectionState.Branch || _selectionState == SelectionState.NodeAndBranch;
            }
        }

        public SelectionState SelectionState
        {
            get
            {
                return _selectionState;
            }

            set
            {
                if (value == _selectionState)
                {
                    return;
                }

                _selectionState = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LineSelected));
            }
        }

        public Point LastPoint
        {
            get
            {
                return _points.Last();
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
        private SelectionState _selectionState;
        private HistoryViewModel _historyViewModel;

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class Point
    {
        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }

    public class HistoryViewModel : INotifyPropertyChanged
    {
        private HistoryViewModel(History history)
        {
            _history = history;
            _listTree = new HistoryEventOrdering(history);
            _listTree.OrderingChanged += ListTree_PositionChanged;
            _nodesToLines = new Dictionary<HistoryEvent, HistoryLineViewModel>();

            _globalMaxX = 0;

            _lines = new ObservableCollection<HistoryLineViewModel>();
            BindingOperations.EnableCollectionSynchronization(_lines, _lines);

            _selectedBookmarks = new HashSet<HistoryLineViewModel>();
            _selectedBranches = new HashSet<HistoryLineViewModel>();

            UpdateLines(_listTree.HorizontalInterestingEvents);
        }

        private void ListTree_PositionChanged(object sender, PositionChangedEventArgs<HistoryEvent> e)
        {
            UpdateLines(e.HorizontalInterestingEvents);
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void UpdateLines(List<InterestingEvent> horizontalInterestingEvents)
        {
            Dictionary<HistoryEvent, HistoryLineViewModel> newNodesToLines = new Dictionary<HistoryEvent, HistoryLineViewModel>();
            Dictionary<int, int> maxXPerY = new Dictionary<int, int>();

            int globalMaxX = 0;
            foreach (InterestingEvent interestingEvent in horizontalInterestingEvents)
            {
                HistoryEvent node = interestingEvent.HistoryEvent;
                InterestingEvent parentEvent = interestingEvent.Parent;

                HistoryLineViewModel parentLine = null;
                if (parentEvent != null)
                {
                    parentLine = newNodesToLines[parentEvent.HistoryEvent];
                }

                if (!_nodesToLines.TryGetValue(node, out HistoryLineViewModel line))
                {
                    line = new HistoryLineViewModel(this, node);
                    lock (_lines)
                    {
                        _lines.Add(line);
                        OnPropertyChanged(nameof(Height));
                    }
                }

                // Draw!
                line.Start();

                bool current = interestingEvent.IsCurrent;

                line.Current = current;
                if (node is RootHistoryEvent)
                {
                    line.Type = LinePointType.None;
                }
                else if (node is BookmarkHistoryEvent bookmarkEvent)
                {
                    line.Type = bookmarkEvent.Bookmark.System ? LinePointType.SystemBookmark : LinePointType.UserBookmark;
                }
                else if (node.Children.Count == 0 || node is RootHistoryEvent || current)
                {
                    line.Type = LinePointType.Action;
                }
                else
                {
                    line.Type = LinePointType.None;
                }

                int previousX = -1;
                int x = 1;
                if (parentLine != null)
                {
                    Point parentPoint = parentLine.LastPoint;
                    line.Add(parentPoint.X, parentPoint.Y);
                    x = parentPoint.X;
                    previousX = x;
                    globalMaxX = Math.Max(globalMaxX, x);
                }

                // What's our vertical ordering?
                int verticalIndex = interestingEvent.VerticalIndex;
                int parentVerticalIndex = parentEvent?.VerticalIndex ?? -1;

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
                        line.Add(x, y * 2);
                    }

                    line.Add(x, y * 2 + 1);
                    previousX = x;

                    globalMaxX = Math.Max(globalMaxX, x);

                    maxXPerY[y] = x + 2;
                }

                line.End();

                newNodesToLines.Add(node, line);
            }

            foreach (HistoryLineViewModel viewModel in _nodesToLines.Values)
            {
                if (!newNodesToLines.Values.Contains(viewModel))
                {
                    lock (_lines)
                    {
                        _lines.Remove(viewModel);
                        OnPropertyChanged(nameof(Height));
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
                if (!_viewModels.TryGetValue(history, out HistoryViewModel viewModel))
                {
                    viewModel = new HistoryViewModel(history);
                    _viewModels.Add(history, viewModel);
                }

                return viewModel;
            }
        }

        public void DeleteSelectedBranches()
        {
            foreach (HistoryLineViewModel historyLineViewModel in _selectedBranches)
            {
                if (historyLineViewModel.Node.IsEqualToOrAncestorOf(_history.CurrentEvent))
                {
                    // Can't delete a branch if it contains the current history node!
                    continue;
                }

                _history.DeleteBranch(historyLineViewModel.Node);
            }

            ClearSelection();
        }

        public void DeleteSelectedBookmarks()
        {
            foreach (HistoryLineViewModel historyLineViewModel in _selectedBookmarks)
            {
                _history.DeleteBookmark(historyLineViewModel.Node);
            }

            ClearSelection();
        }

        public BookmarkHistoryEvent SelectedBookmark
        {
            get
            {
                if (_selectedBookmarks.Count == 1)
                {
                    return _selectedBookmarks.First().Node as BookmarkHistoryEvent;
                }

                return null;
            }
        }

        public void ClearSelection()
        {
            foreach (HistoryLineViewModel viewModel in _lines)
            {
                viewModel.SelectionState = SelectionState.None;
            }

            _selectedBookmarks.Clear();
            _selectedBranches.Clear();
        }

        public void SelectBranch(HistoryLineViewModel historyLineViewModel)
        {
            foreach (HistoryLineViewModel viewModel in _lines)
            {
                if (historyLineViewModel.Node.IsEqualToOrAncestorOf(viewModel.Node))
                {
                    viewModel.SelectionState = SelectionState.NodeAndBranch;
                }
            }

            _selectedBranches.Add(historyLineViewModel);
        }

        public void SelectBookmark(HistoryLineViewModel historyLineViewModel)
        {
            ClearSelection();

            foreach (HistoryLineViewModel viewModel in _lines)
            {
                viewModel.SelectionState = SelectionState.None;
            }

            _selectedBookmarks.Add(historyLineViewModel);
            historyLineViewModel.SelectionState = SelectionState.Node;
        }

        public void SelectBookmarkToggle(HistoryLineViewModel historyLineViewModel)
        {
            switch (historyLineViewModel.SelectionState)
            {
                case SelectionState.None:
                    historyLineViewModel.SelectionState = SelectionState.Node;
                    break;
                case SelectionState.Node:
                    historyLineViewModel.SelectionState = SelectionState.None;
                    break;
                case SelectionState.Branch:
                    historyLineViewModel.SelectionState = SelectionState.NodeAndBranch;
                    break;
                case SelectionState.NodeAndBranch:
                    historyLineViewModel.SelectionState = SelectionState.Branch;
                    break;
            }
        }

        public void Select(SelectionMode mode, HistoryLineViewModel lineViewModel)
        {
            switch (mode)
            {
                case SelectionMode.New:
                    foreach (HistoryLineViewModel viewModel in _lines)
                    {
                        viewModel.SelectionState = SelectionState.None;
                    }

                    lineViewModel.SelectionState = SelectionState.NodeAndBranch;
                    break;
                case SelectionMode.Toggle:
                    SelectionState newState = SelectionState.None;
                    switch (lineViewModel.SelectionState)
                    {
                        case SelectionState.None:
                            newState = SelectionState.Node;
                            break;
                    }

                    lineViewModel.SelectionState = newState;
                    break;
            }
        }

        public int Width
        {
            get
            {
                return _globalMaxX + 1;
            }
        }

        public int Height
        {
            get
            {
                return _lines.Count * 2;
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
        private Dictionary<HistoryEvent, HistoryLineViewModel> _nodesToLines;

        private ObservableCollection<HistoryLineViewModel> _lines;

        private HistoryEventOrdering _listTree;
        public event PropertyChangedEventHandler PropertyChanged;

        static private ConditionalWeakTable<History, HistoryViewModel> _viewModels;

        private HashSet<HistoryLineViewModel> _selectedBookmarks;
        private HashSet<HistoryLineViewModel> _selectedBranches;
    }
}
