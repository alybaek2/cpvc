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
        private readonly HistoryEventOrderings _previousOrderings;

        private ListTree _listTree;

        private Dictionary<HistoryEvent, Polyline> _polylines;
        private Dictionary<HistoryEvent, Ellipse> _circles;

        private bool _updatePending;

        public HistoryControl2()
        {
            _orderings = new HistoryEventOrderings();
            //_orderings.VerticalOrdering().ListChanged += VerticalListChanged;
            //_orderings.HorizontalOrdering().ListChanged += HorizontalListChanged;
            //_orderings.PositionChanged += OrderingsPositionChanged;

            _previousOrderings = null;

            _updatePending = false;
            _polylines = new Dictionary<HistoryEvent, Polyline>();
            _circles = new Dictionary<HistoryEvent, Ellipse>();

            _branches = new List<BranchInfo>();
            _branchesMap = new Dictionary<HistoryEvent, BranchInfo>();

            _rows = new List<Row>(); //  new OrderedList<ulong, OldRow>();
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

                //InitRows();

                if (_history == null)
                {
                    _listTree = null;
                }
                else
                {
                    _listTree = new ListTree(_history.RootEvent);

                    lock (_listTree)
                    {
                        List<HistoryEvent> nodes = new List<HistoryEvent>();
                        nodes.AddRange(_history.RootEvent.Children);

                        while (nodes.Any())
                        {
                            HistoryEvent historyEvent = nodes[0];
                            nodes.RemoveAt(0);

                            HistoryChangedEventArgs args = new HistoryChangedEventArgs(_history, historyEvent, HistoryChangedAction.Add);
                            UpdateListTree(args);


                            nodes.AddRange(historyEvent.Children);
                        }
                    }
                }

            }

            ScheduleUpdateCanvas();
        }

        public void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {
            lock (_orderings)
            {
                bool changed = false;
                if (_orderings.Process(args.HistoryEvent, args.Action))
                {
                    ScheduleUpdateCanvas();
                    changed = true;
                }

            }

            UpdateListTree(args);
        }

        static private bool InterestingEvent(HistoryEvent historyEvent)
        {
            if (historyEvent is RootHistoryEvent ||
                historyEvent is BookmarkHistoryEvent ||
                historyEvent.Children.Count != 1)
            {
                return true;
            }

            return false;
        }

        private void UpdateListTree(HistoryChangedEventArgs args)
        {
            if (_listTree == null)
            {
                return;
            }

            lock (_listTree)
            {
                switch (args.Action)
                {
                    case HistoryChangedAction.Add:
                        {
                            HistoryEvent parent = args.HistoryEvent.Parent;
                            bool isParentInteresting = InterestingEvent(parent);
                            ListTreeNode parentNode = _listTree.GetNode(parent);
                            bool wasParentInteresting = parentNode != null;
                            bool isNodeInteresting = InterestingEvent(args.HistoryEvent);

                            if (wasParentInteresting && !isParentInteresting)
                            {
                                // Remove the parent node, or change it!
                                if (isNodeInteresting)
                                {
                                    // Change it!
                                    _listTree.Update(parent, args.HistoryEvent);
                                }
                                else
                                {
                                    // Todo: Delete it!
                                }
                            }
                            else if ((wasParentInteresting && isParentInteresting) || (!wasParentInteresting && !isParentInteresting))
                            {
                                // Just add the node!
                                HistoryEvent interestingParent = parent;
                                while (!InterestingEvent(interestingParent))
                                {
                                    interestingParent = interestingParent.Parent;
                                }

                                parentNode = _listTree.GetNode(interestingParent);
                                //ListTreeNode node = new ListTreeNode(args.HistoryEvent);

                                _listTree.Add(parentNode, args.HistoryEvent);
                            }
                            else if (!wasParentInteresting && isParentInteresting)
                            {
                                // Add the parent... it has become interesting!
                                HistoryEvent interestingGrandParent = parent.Parent;
                                while (!InterestingEvent(interestingGrandParent))
                                {
                                    interestingGrandParent = interestingGrandParent.Parent;
                                }

                                ListTreeNode grandparentNode = _listTree.GetNode(interestingGrandParent);
                                //parentNode = new ListTreeNode(parent);
                                parentNode = _listTree.Add(grandparentNode, parent);

                                // There must be one descendent of grandparent that should now be moved to be a child of parent!

                                // Now add the node itself.
                                //ListTreeNode node = new ListTreeNode(args.HistoryEvent);

                                _listTree.Add(parentNode, args.HistoryEvent);
                            }
                            else if (!wasParentInteresting && !isParentInteresting)
                            {
                                // Just add the node!
                                HistoryEvent interestingParent = parent;
                                while (!InterestingEvent(interestingParent))
                                {
                                    interestingParent = interestingParent.Parent;
                                }

                                parentNode = _listTree.GetNode(interestingParent);
                                //ListTreeNode node = new ListTreeNode(args.HistoryEvent);

                                _listTree.Add(parentNode, args.HistoryEvent);
                            }

                            _listTree.DebugDump();
                        }
                        break;
                    case HistoryChangedAction.UpdateCurrent:
                        {
                            ListTreeNode node = _listTree.GetNode(args.HistoryEvent);

                            _listTree.Update(node);
                        }
                        break;
                    case HistoryChangedAction.DeleteBranch:
                        {
                            ListTreeNode node = _listTree.GetNode(args.HistoryEvent);

                            _listTree.RemoveRecursive(node);
                        }
                        break;
                    case HistoryChangedAction.DeleteBookmark:
                        {
                            ListTreeNode node = _listTree.GetNode(args.HistoryEvent);

                            _listTree.RemoveNonRecursive(node);
                        }
                        break;
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
                //UpdateCanvas();
                UpdateCanvasListTree();
                sw.Stop();

                CPvC.Diagnostics.Trace("Update items took {0}ms", sw.ElapsedMilliseconds);
            };

            timer.Start();


            //Dispatcher.BeginInvoke(new Action(() => {
            //    UpdateCanvas();
            //}), null);
        }

        private void UpdateCanvasListTree()
        {
            if (_listTree == null)
            {
                return;
            }

            lock (_listTree)
            {
                Children.Clear();

                List<ListTreeNode> nodes = new List<ListTreeNode>();
                nodes.Add(_listTree.Root);

                while (nodes.Any())
                {
                    ListTreeNode node = nodes[0];
                    nodes.RemoveAt(0);

                    System.Drawing.Point position = _listTree.GetPosition(node);

                    bool filled = _history.CurrentEvent != node.HistoryEvent;
                    Ellipse circle = new Ellipse
                    {
                        Stroke = Brushes.DarkBlue,
                        Fill = filled ? Brushes.DarkBlue : Brushes.White,
                        StrokeThickness = 2,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        UseLayoutRounding = true,
                        Margin = new Thickness((position.X + 0.5) * 16 - 5, (position.Y + 0.5) * 16 - 5, 0, 0),
                        Width = 10,
                        Height = 10
                    };

                    // Ensure the dot is always "on top".
                    Canvas.SetZIndex(circle, 100);

                    Children.Add(circle);

                    if (node.Parent != null)
                    {
                        System.Drawing.Point parentPosition = _listTree.GetPosition(node.Parent);

                        Line line = new Line
                        {
                            X1 = 16 * (position.X + 0.5),
                            Y1 = 16 * (position.Y + 0.5),
                            X2 = 16 * (parentPosition.X + 0.5),
                            Y2 = 16 * (parentPosition.Y + 0.5),
                            StrokeThickness = 2,
                            Stroke = Brushes.DarkBlue,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            UseLayoutRounding = true
                        };

                        Children.Add(line);

                        // Ensure lines are never "on top" of dots.
                        Canvas.SetZIndex(line, 1);
                    }

                    nodes.AddRange(node.Children);
                }
            }
        }

        private void UpdateCanvas()
        {
            lock (_orderings)
            {
                Children.Clear();
                ObservableList<HistoryEvent> horizontalEvents = _orderings.HorizontalOrdering();

                HashSet<HistoryEvent> branchesToDelete = new HashSet<HistoryEvent>(_branchesMap.Keys);

                // Create some branches!
                _branches.Clear();
                _branchesMap.Clear();
                for (int i = 0; i < horizontalEvents.Count; i++)
                {
                    HistoryEvent horizontalEvent = horizontalEvents[i];
                    HistoryEvent parent = _orderings.ParentEvent(horizontalEvent);
                    int parentVertical = (parent != null) ? _orderings.VerticalPosition(parent) : -1;

                    if (!_branchesMap.TryGetValue(horizontalEvents[i], out BranchInfo branch))
                    {
                        branch = new BranchInfo(null, i, _orderings.VerticalPosition(horizontalEvent), horizontalEvent);
                    }

                    // If we go through in order, we should always have the parent before the child.
                    BranchInfo parentBranch = null;
                    HistoryEvent parentEvent = _orderings.ParentEvent(horizontalEvents[i]);
                    if (parentEvent != null && !_branchesMap.TryGetValue(parentEvent, out parentBranch))
                    {
                        throw new Exception("Should have parent branch!");
                    }

                    branch._x = i;
                    branch._y = _orderings.VerticalPosition(horizontalEvent);
                    branch._parent = parentBranch;


                    _branchesMap.Add(horizontalEvent, branch);
                    branchesToDelete.Remove(horizontalEvent);

                    //BranchInfo branch = new BranchInfo(null, i, _orderings.VerticalPosition(horizontalEvent), horizontalEvent);

                    _branches.Add(branch);
                }

                for (int b = 0; b < _branches.Count; b++)
                {
                    HistoryEvent horizontalEvent = _branches[b]._historyEvent;
                    HistoryEvent parent = _orderings.ParentEvent(horizontalEvent);

                    if (parent != null)
                    {
                        _branches[b]._parent = _branches.Find(p => ReferenceEquals(p._historyEvent, parent));

                    }

                    bool filled = _history.CurrentEvent != horizontalEvent;
                    Ellipse circle = new Ellipse
                    {
                        Stroke = Brushes.DarkBlue,
                        Fill = filled ? Brushes.DarkBlue : Brushes.White,
                        StrokeThickness = 2,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                        UseLayoutRounding = true,
                        Margin = new Thickness((_branches[b]._x + 0.5) * 16 - 5, (_branches[b]._y + 0.5) * 16 - 5, 0, 0),
                        Width = 10,
                        Height = 10
                    };

                    // Ensure the dot is always "on top".
                    Canvas.SetZIndex(circle, 100);

                    Children.Add(circle);

                    if (_branches[b]._parent != null)
                    {
                        Line line = new Line
                        {
                            X1 = 16 * (_branches[b]._x + 0.5),
                            Y1 = 16 * (_branches[b]._y + 0.5),
                            X2 = 16 * (_branches[b]._parent._x + 0.5),
                            Y2 = 16 * (_branches[b]._parent._y + 0.5),
                            StrokeThickness = 2,
                            Stroke = Brushes.DarkBlue,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            VerticalAlignment = VerticalAlignment.Top,
                            UseLayoutRounding = true
                        };

                        Children.Add(line);

                        // Ensure lines are never "on top" of dots.
                        Canvas.SetZIndex(line, 1);
                    }
                }

                //for (int h = 0; h < horizontalEvents.Count; h++)
                //{
                //    HistoryEvent horizontalEvent = horizontalEvents[h];
                //    HistoryEvent parent = _orderings.ParentEvent(horizontalEvent);
                //    int parentVertical = (parent != null) ? _orderings.VerticalPosition(parent) : -1;

                //    int vertical = _orderings.VerticalPosition(horizontalEvent);

                //    bool filled = _history.CurrentEvent != horizontalEvent;
                //    if (!_circles.TryGetValue(horizontalEvent, out Ellipse circle))
                //    {
                //        circle = new Ellipse
                //        {
                //            Stroke = Brushes.DarkBlue,
                //            Fill = filled ? Brushes.DarkBlue : Brushes.White,
                //            StrokeThickness = 2,
                //            HorizontalAlignment = HorizontalAlignment.Left,
                //            VerticalAlignment = VerticalAlignment.Top,
                //            UseLayoutRounding = true,
                //            Margin = new Thickness(h * 16, vertical * 16, 0, 0),
                //            Width = 10,
                //            Height = 10
                //        };

                //        _circles.Add(horizontalEvent, circle);
                //        Children.Add(circle);
                //    }
                //    else
                //    {
                //        circle.Margin = new Thickness(h * 16, vertical * 16, 0, 0);
                //        circle.Fill = filled ? Brushes.DarkBlue : Brushes.White;
                //    }

                //    //if (!_polylines.TryGetValue(horizontalEvent, out Polyline polyline))
                //    //{
                //    //    polyline = new Polyline
                //    //    {
                //    //        StrokeThickness = 2,
                //    //        Stroke = Brushes.DarkBlue,
                //    //        HorizontalAlignment = HorizontalAlignment.Left,
                //    //        VerticalAlignment = VerticalAlignment.Top,
                //    //        //Points = { new Point(200, 200), new Point(240, 240) },
                //    //        UseLayoutRounding = true
                //    //    };

                //    //    _polylines.Add(horizontalEvent, polyline);
                //    //    Children.Add(polyline);

                //    //    Canvas.SetZIndex(polyline, 1);

                //    //}

                //    //int p = 0;
                //    //for (int v = parentVertical + 1; v <= vertical; v++)
                //    //{
                //    //    if (p < polyline.Points.Count)
                //    //    {
                //    //        polyline.Points[p] = new Point(h * 16, v * 16);
                //    //        Diagnostics.Trace("Inserting point with {0},{1}", h * 16, v * 16);
                //    //    }
                //    //    else
                //    //    {
                //    //        polyline.Points.Add(new Point(h * 16, v * 16));
                //    //        Diagnostics.Trace("Adding point with {0},{1}", h * 16, v * 16);
                //    //    }

                //    //    p++;
                //    //}

                //}

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

        public void InitRows()
        {
            _rows.Clear();

            ObservableList<HistoryEvent> historyEvents = _orderings.HorizontalOrdering();

            foreach(HistoryEvent historyEvent in historyEvents)
            {
                Add(historyEvent);
            }
        }

        public void Dump()
        {
            string str = "";
            for (int r = _rows.Count - 1; r >= 0; r--)
            {
                Row row = _rows[r];

                string line = "";

                foreach (Cell c in row.Cells)
                {
                    // Pad out to X!
                    while (line.Length < c.X * 10)
                    {
                        line += " ";
                    }


                    if (c.HistoryEvent != row.HistoryEvent)
                    {
                        line += String.Format("| ({0})  ", c.HistoryEvent.Id);
                    }
                    else if (c.HistoryEvent.Children.Count == 0)
                    {
                        line += String.Format("o ({0})  ", c.HistoryEvent.Id);
                    }
                    else
                    {
                        line += String.Format("+ ({0})  ", c.HistoryEvent.Id);
                    }
                }

                str += line + "\n";
            }

            CPvC.Diagnostics.Trace(str);
        }

        public void RemoveRecursive(HistoryEvent historyEvent)
        {
            List<HistoryEvent> descendents = new List<HistoryEvent>();
            descendents.Add(historyEvent);

            while (descendents.Any())
            {
                HistoryEvent he = descendents[0];
                descendents.RemoveAt(0);

                // Since the history event may no longer be in _orderings, we should really be searching
                // _rows for history events to delete.
                //if (_orderings.Contains(he))
                {
                    Remove(he);
                }

                descendents.AddRange(he.Children);
            }
        }

        public bool Remove(HistoryEvent historyEvent)
        {
            int index = _rows.FindIndex(r => ReferenceEquals(r.HistoryEvent, historyEvent));
            if (index != -1)
            {
                _rows.RemoveAt(index);
                index--;

                // Now remove any "passthroughs"...
                int cellIndex = -1;
                while ((cellIndex = _rows[index].Cells.FindIndex(c => ReferenceEquals(c.HistoryEvent, historyEvent))) != -1)
                {
                    _rows[index].Cells.RemoveAt(cellIndex);
                    index--;
                }

                return true;
            }

            return false;
        }

        public void AddRecursive(HistoryEvent historyEvent)
        {
            List<HistoryEvent> descendents = new List<HistoryEvent>();
            descendents.Add(historyEvent);

            while (descendents.Any())
            {
                HistoryEvent he = descendents[0];
                descendents.RemoveAt(0);

                if (_orderings.VerticalPosition(he) != -1)
                {
                    Add(he);
                }

                descendents.AddRange(he.Children);
            }
        }

        public void Add(HistoryEvent historyEvent)
        {
            int index = _rows.FindIndex(r => ReferenceEquals(r.HistoryEvent, historyEvent));
            if (index != -1)
            {
                // Already exists!
                return;
            }

            // Figure out the vertical position!
            int verticalPosition = _orderings.VerticalPosition(historyEvent);
            if (verticalPosition == -1)
            {
                return;
            }

            // Insert a new row!
            Row row = new Row(historyEvent);
            _rows.Insert(verticalPosition, row);

            if (verticalPosition < _rows.Count - 1)
            {
                Row previousRow = _rows[verticalPosition + 1];
                for (int c = 0; c < previousRow.Cells.Count; c++)
                {
                    if (!ReferenceEquals(row.HistoryEvent, previousRow.Cells[c].HistoryEvent))
                    {
                        row.Cells.Add(new Cell(previousRow.Cells[c]));
                    }
                }

            }
            else
            {
                //row.Cells.Add(new Cell(historyEvent, 0));
            }

            // Add passthroughs! Todo!
            int parentVerticalIndex = -1;
            HistoryEvent parentHistoryEvent = _orderings.ParentEvent(historyEvent);
            if (parentHistoryEvent != null)
            {
                parentVerticalIndex = _orderings.VerticalPosition(parentHistoryEvent);
            }

            //bool inserted = false;
            ObservableList<HistoryEvent> horizontalOrdering = _orderings.HorizontalOrdering();

            int newCellHorizontalPosition = horizontalOrdering.FindIndex(historyEvent);

            for (int v = parentVerticalIndex + 1; v <= verticalPosition; v++)
            {
                Row vrow = _rows[v];

                int x = 0;
                int h = 0;
                while (h < vrow.Cells.Count)
                {
                    int cellHorizontalPosition = horizontalOrdering.FindIndex(vrow.Cells[h].HistoryEvent);
                    if (newCellHorizontalPosition < cellHorizontalPosition)
                    {
                        //inserted = true;
                        break;
                    }

                    x = vrow.Cells[h].X + 1;
                    h++;
                }

                vrow.Cells.Insert(h, new Cell(historyEvent, x));
                //if (!inserted)
                //{
                //    row.Cells.Add(new Cell(historyEvent, x));
                //}
            }
        }

        private List<Row> _rows;

        private class Row
        {
            public Row(HistoryEvent historyEvent)
            {
                _historyEvent = historyEvent;
                _cells = new List<Cell>();
            }

            public HistoryEvent HistoryEvent
            {
                get
                {
                    return _historyEvent;
                }
            }

            public List<Cell> Cells
            {
                get
                {
                    return _cells;
                }
            }

            private List<Cell> _cells;
            private HistoryEvent _historyEvent;
        }

        private class Cell
        {
            public Cell(HistoryEvent historyEvent, int x)
            {
                _historyEvent = historyEvent;
                _x = x;
                //_terminus = terminus;
            }

            public Cell(Cell other)
            {
                _historyEvent = other._historyEvent;
                _x = other._x;
            }

            public HistoryEvent HistoryEvent
            {
                get
                {
                    return _historyEvent;
                }
            }

            public int X
            {
                get
                {
                    return _x;
                }
            }

            //public bool Terminus
            //{
            //    get
            //    {
            //        return _terminus;
            //    }
            //}

            private HistoryEvent _historyEvent;
            private int _x;
            //private bool _terminus;
        }

        //private OrderedList<UInt64, OldRow> _rows;
        //private Dictionary<HistoryEvent, OrderedList<UInt64, Tuple<HistoryEvent, BranchInfo>>> _rowsByHistoryEvent;

        private class OldRow
        {
            public OldRow()
            {
                _branchBits = new OrderedList<ulong, BranchInfo>();
            }

            public BranchInfo Branch
            {
                get
                {
                    return _branch;
                }
            }

            public OrderedList<UInt64, BranchInfo> BranchBits
            {
                get
                {
                    return _branchBits;
                }
            }

            private BranchInfo _branch;
            private OrderedList<UInt64, BranchInfo> _branchBits;
        }

        private class BranchInfo
        {
            public BranchInfo(BranchInfo parent, int x, int y, HistoryEvent historyEvent)
            {
                _parent = parent;
                _x = x;
                _y = y;
                _historyEvent = historyEvent;
            }

            public BranchInfo _parent;
            public int _x;
            public int _y;
            public HistoryEvent _historyEvent;
        }

        private class OrderedList<V, T> where V : IComparable
        {
            public OrderedList()
            {
                _values = new List<V>();
                _items = new List<T>();
            }

            public int Add(V value, T item)
            {
                int index = Index(item);
                if (index == -1)
                {
                    // Insert
                    for (int i = 0; i < _items.Count; i++)
                    {
                        if (_values[i].CompareTo(value) < 0)
                        {
                            _values.Insert(i, value);
                            _items.Insert(i, item);

                            index = i;
                            break;
                        }
                    }

                    if (index == -1)
                    {
                        index = _items.Count;
                        _values.Add(value);
                        _items.Add(item);
                    }

                    return index;
                }
                else
                {
                    // Update
                    throw new Exception("Adding already existing item!!!");
                }
            }

            public int Update(V value, T item)
            {
                int index = Index(item);
                if (index >= 0)
                {
                    if (_values[index].CompareTo(value) == 0)
                    {
                        // Nothing to change!
                        return index;
                    }

                    _values.RemoveAt(index);
                    _items.RemoveAt(index);

                    return Add(value, item);
                }
                else
                {
                    // Oops!
                    throw new Exception("Updating a nonexitent item!");
                }
            }

            public int Remove(T item)
            {
                int index = Index(item);
                if (index >= 0)
                {
                    _values.RemoveAt(index);
                    _items.RemoveAt(index);
                }

                return index;
            }

            public int Index(T item)
            {
                return _items.FindIndex(i => ReferenceEquals(i, item));
            }

            private List<V> _values;
            private List<T> _items;
        }


        private List<BranchInfo> _branches;
        private Dictionary<HistoryEvent, BranchInfo> _branchesMap;
    }
}
