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

        //private readonly HistoryEventOrderings _orderings;
        //private readonly HistoryEventOrderings _previousOrderings;

        private HistoryListTree _listTree;

        //private Dictionary<HistoryEvent, Polyline> _polylines;
        //private Dictionary<HistoryEvent, Ellipse> _circles;

        private bool _updatePending;

        public HistoryControl2()
        {
            //_orderings = new HistoryEventOrderings();
            //_orderings.VerticalOrdering().ListChanged += VerticalListChanged;
            //_orderings.HorizontalOrdering().ListChanged += HorizontalListChanged;
            //_orderings.PositionChanged += OrderingsPositionChanged;

            //_previousOrderings = null;

            _updatePending = false;
            //_polylines = new Dictionary<HistoryEvent, Polyline>();
            //_circles = new Dictionary<HistoryEvent, Ellipse>();

            //_branches = new List<BranchInfo>();
            //_branchesMap = new Dictionary<HistoryEvent, BranchInfo>();

            //_oldHorizontalOrdering = new List<HistoryEvent>();
            //_oldVerticalOrdering = new List<HistoryEvent>();
            //_shapes = new Dictionary<HistoryEvent, BranchShapes>();

            _branchLines = new Dictionary<History, BranchLine>();

            //_rows = new List<Row>(); //  new OrderedList<ulong, OldRow>();
            DataContextChanged += HistoryControl2_DataContextChanged;
        }



        private void HistoryControl2_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (ReferenceEquals(e.OldValue, e.NewValue))
            {
                return;
            }

            //History oldHistory = (History)e.OldValue;
            //if (oldHistory != null)
            //{
            //    oldHistory.Auditors -= ProcessHistoryChange;
            //}

            History newHistory = (History)e.NewValue;
            //if (newHistory != null)
            //{
            //    newHistory.Auditors += ProcessHistoryChange;
            //}

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

                    //lock (_listTree)
                    //{
                    //    List<HistoryEvent> nodes = new List<HistoryEvent>();
                    //    nodes.AddRange(_history.RootEvent.Children);

                    //    while (nodes.Any())
                    //    {
                    //        HistoryEvent historyEvent = nodes[0];
                    //        nodes.RemoveAt(0);

                    //        HistoryChangedEventArgs args = new HistoryChangedEventArgs(_history, historyEvent, HistoryChangedAction.Add);
                    //        UpdateListTree(args);


                    //        nodes.AddRange(historyEvent.Children);
                    //    }
                    //}
                }

            }
        }

        private void ListTree_PositionChanged(object sender, NotifyPositionChangedEventArgs e)
        {
            ScheduleUpdateCanvas(e);
        }

        //public void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        //{
        //    //lock (_orderings)
        //    //{
        //    //    bool changed = false;
        //    //    if (_orderings.Process(args.HistoryEvent, args.Action))
        //    //    {
        //    //        ScheduleUpdateCanvas();
        //    //        changed = true;
        //    //    }

        //    //}

        //    bool changed = UpdateListTree(args);
        //    if (changed)
        //    {
        //        ScheduleUpdateCanvas();
        //    }
        //}

        //static private bool InterestingEvent(HistoryEvent historyEvent)
        //{
        //    if (historyEvent is RootHistoryEvent ||
        //        historyEvent is BookmarkHistoryEvent ||
        //        historyEvent.Children.Count != 1)
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        //private ListTreeNode ParentNode(HistoryEvent historyEvent)
        //{
        //    HistoryEvent interestingParent = historyEvent; //.Parent;
        //    ListTreeNode parentNode = null;
        //    do //while (parentNode == null)
        //    {
        //        //parentNode = parentNode.Parent;
        //        interestingParent = interestingParent.Parent;

        //        parentNode = _listTree.GetNode(interestingParent);
        //    }
        //    while (parentNode == null);

        //    //ListTreeNode parentNode = _listTree.GetNode(interestingParent);

        //    return parentNode;
        //}

        //private bool AddEventToListTree(HistoryEvent historyEvent)
        //{
        //    HistoryEvent parentHistoryEvent = historyEvent.Parent;
        //    ListTreeNode parentNode = _listTree.GetNode(parentHistoryEvent);
        //    bool wasParentInteresting = parentNode != null;
        //    bool isParentInteresting = InterestingEvent(parentHistoryEvent);
        //    ListTreeNode node = _listTree.GetNode(historyEvent);

        //    if (node != null)
        //    {
        //        // The node is already in the tree... we shouldn't be trying to add it!
        //        throw new Exception("Node was already in the tree.");
        //    }

        //    if (!InterestingEvent(historyEvent))
        //    {
        //        return false;
        //    }

        //    bool add = true;

        //    // First, check if the parent's interestingness has changed from false to true.
        //    if (!wasParentInteresting && isParentInteresting)
        //    {
        //        // Need to add the parent!

        //        // But first, find the child who will share this new parent!
        //        ListTreeNode cousinNode = null;
        //        HistoryEvent he = parentHistoryEvent;
        //        while (true)
        //        {
        //            he = he.Children[0];
        //            cousinNode = _listTree.GetNode(he);
        //            if (cousinNode != null)
        //            {
        //                break;
        //            }
        //        }

        //        //ListTreeNode cousinNode = parentNode.Children[0];


        //        parentNode = _listTree.InsertNewParent(cousinNode, parentHistoryEvent);
        //    }
        //    else if (!wasParentInteresting && !isParentInteresting)
        //    {
        //        // Work our way up the tree to find who should be our parent!
        //        HistoryEvent he = parentHistoryEvent;
        //        while (true)
        //        {
        //            ListTreeNode n = _listTree.GetNode(he);
        //            if (n != null)
        //            {
        //                parentNode = n;
        //                break;
        //            }

        //            he = he.Parent;
        //        }
        //    }
        //    else if (wasParentInteresting && !isParentInteresting)
        //    {
        //        // Replace the parent node with the child!
        //        _listTree.Update(parentHistoryEvent, historyEvent);
        //        add = false;
        //    }
        //    else if (wasParentInteresting && isParentInteresting)
        //    {
        //        // Nothing to do! Just add the new node!
        //    }

        //    if (add && InterestingEvent(historyEvent))
        //    {
        //        _listTree.Add(parentNode, historyEvent);
        //    }

        //    return true;
        //}

        //private bool UpdateListTree(HistoryChangedEventArgs args)
        //{
        //    if (_listTree == null)
        //    {
        //        return false;
        //    }

        //    bool changed = false;

        //    lock (_listTree)
        //    {
        //        switch (args.Action)
        //        {
        //            case HistoryChangedAction.Add:
        //                {
        //                    changed = AddEventToListTree(args.HistoryEvent);

        //                    //HistoryEvent parent = args.HistoryEvent.Parent;
        //                    //bool isParentInteresting = InterestingEvent(parent);
        //                    //ListTreeNode parentNode = _listTree.GetNode(parent);
        //                    //bool wasParentInteresting = parentNode != null;
        //                    //bool isNodeInteresting = InterestingEvent(args.HistoryEvent);
        //                    //ListTreeNode node = _listTree.GetNode(args.HistoryEvent);
        //                    //bool wasNodeInteresting = node != null;

        //                    //// Probably should deal with the case when the node isn't interesting but it was... need to delete!

        //                    //if (wasNodeInteresting)
        //                    //{
        //                    //    // This should be impossible!
        //                    //    throw new Exception("Node was already in the tree.");
        //                    //}

        //                    //if (!wasParentInteresting && isParentInteresting)
        //                    //{
        //                    //    // This will happen if a history event was added to a parent which previously had only one child.
        //                    //    changed = _listTree.Update(args.HistoryEvent, parent);

        //                    //}
        //                    //else
        //                    //{
        //                    //    // Remove it!
        //                    //    _listTree.RemoveNonRecursive(node);
        //                    //}

        //                    //if (!wasNodeInteresting && isNodeInteresting)
        //                    //{
        //                    //    if (wasParentInteresting && !isParentInteresting)
        //                    //    {
        //                    //        changed = _listTree.Update(parent, args.HistoryEvent);
        //                    //    }
        //                    //}


        //                    //if (wasParentInteresting && !isParentInteresting)
        //                    //{
        //                    //    // Remove the parent node, or change it!
        //                    //    if (isNodeInteresting)
        //                    //    {
        //                    //        // Change it!
        //                    //        changed = _listTree.Update(parent, args.HistoryEvent);
        //                    //    }
        //                    //    else
        //                    //    {
        //                    //        // Todo: Delete it!
        //                    //    }
        //                    //}
        //                    //else if ((wasParentInteresting && isParentInteresting) || (!wasParentInteresting && !isParentInteresting))
        //                    //{
        //                    //    if (isNodeInteresting)
        //                    //    {
        //                    //        // Just add the node!
        //                    //        parentNode = ParentNode(args.HistoryEvent); // parent;
        //                    //        //while (!InterestingEvent(interestingParent))
        //                    //        //{
        //                    //        //    interestingParent = interestingParent.Parent;
        //                    //        //}

        //                    //        //parentNode = _listTree.GetNode(interestingParent);
        //                    //        //ListTreeNode node = new ListTreeNode(args.HistoryEvent);

        //                    //        _listTree.Add(parentNode, args.HistoryEvent);
        //                    //        changed = true;
        //                    //    }
        //                    //}
        //                    //else if (!wasParentInteresting && isParentInteresting)
        //                    //{
        //                    //    // Add the parent... it has become interesting!
        //                    //    //HistoryEvent interestingGrandParent = parent.Parent;
        //                    //    //while (!InterestingEvent(interestingGrandParent))
        //                    //    //{
        //                    //    //    interestingGrandParent = interestingGrandParent.Parent;
        //                    //    //}

        //                    //    //ListTreeNode grandparentNode = _listTree.GetNode(interestingGrandParent);
        //                    //    ListTreeNode grandparentNode = ParentNode(parent.Parent);
        //                    //    parentNode = _listTree.Add(grandparentNode, parent);

        //                    //    if (isNodeInteresting)
        //                    //    {
        //                    //        // There must be one descendent of grandparent that should now be moved to be a child of parent!

        //                    //        // Now add the node itself.
        //                    //        //ListTreeNode node = new ListTreeNode(args.HistoryEvent);

        //                    //        _listTree.Add(parentNode, args.HistoryEvent);

        //                    //        changed = true;
        //                    //    }
        //                    //}
        //                    //else if (!wasParentInteresting && !isParentInteresting)
        //                    //{
        //                    //    if (isNodeInteresting)
        //                    //    {
        //                    //        // Just add the node!
        //                    //        HistoryEvent interestingParent = parent;
        //                    //        while (!InterestingEvent(interestingParent))
        //                    //        {
        //                    //            interestingParent = interestingParent.Parent;
        //                    //        }

        //                    //        parentNode = _listTree.GetNode(interestingParent);
        //                    //        //ListTreeNode node = new ListTreeNode(args.HistoryEvent);

        //                    //        _listTree.Add(parentNode, args.HistoryEvent);

        //                    //        changed = true;
        //                    //    }
        //                    //}

        //                    //_listTree.DebugDump();
        //                }
        //                break;
        //            case HistoryChangedAction.UpdateCurrent:
        //                {
        //                    ListTreeNode node = _listTree.GetNode(args.HistoryEvent);

        //                    changed = _listTree.Update(node);
        //                }
        //                break;
        //            case HistoryChangedAction.DeleteBranch:
        //                {
        //                    ListTreeNode node = _listTree.GetNode(args.HistoryEvent);

        //                    _listTree.RemoveRecursive(node);

        //                    changed = true;
        //                }
        //                break;
        //            case HistoryChangedAction.DeleteBookmark:
        //                {
        //                    ListTreeNode node = _listTree.GetNode(args.HistoryEvent);

        //                    _listTree.RemoveNonRecursive(node);

        //                    changed = true;
        //                }
        //                break;
        //        }
        //    }


        //    return changed;
        //}

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
                    line.Points.Add(parentPoint);
                }

                for (int v = parentPoint.Y + 1; v <= verticalIndex; v++)
                {
                    if (!leftmost.TryGetValue(v, out int left))
                    {
                        left = 0;
                        leftmost.Add(v, left);
                    }

                    line.Points.Add(new Point(left, v));

                    leftmost[v] = left + 1;
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

                //ScheduleUpdateCanvas();
            };

            timer.Start();


            //Dispatcher.BeginInvoke(new Action(() => {
            //    UpdateCanvas();
            //}), null);
        }

        //private List<HistoryEvent> _oldHorizontalOrdering;
        //private List<HistoryEvent> _oldVerticalOrdering;
        //private Dictionary<HistoryEvent, BranchShapes> _shapes;

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

                foreach (Point p in line.Points)
                {
                    double x = 16 * (p.X + 0.5);
                    double y = 16 * (p.Y + 0.5);
                    polyline.Points.Add(new System.Windows.Point(x, y));
                }

                Children.Add(polyline);

                // Ensure lines are never "on top" of dots.
                Canvas.SetZIndex(polyline, 1);

                Point lastPoint = line.Points[line.Points.Count - 1];

                bool filled = !ReferenceEquals(_history?.CurrentEvent, node.HistoryEvent);
                Ellipse circle = new Ellipse
                {
                    Stroke = Brushes.DarkBlue,
                    Fill = filled ? Brushes.DarkBlue : Brushes.White,
                    StrokeThickness = 2,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    UseLayoutRounding = true,
                    Margin = new Thickness((lastPoint.X + 0.5) * 16 - 5, (lastPoint.Y + 0.5) * 16 - 5, 0, 0),
                    Width = 10,
                    Height = 10
                };

                // Ensure the dot is always "on top".
                Canvas.SetZIndex(circle, 100);

                Children.Add(circle);
            }
        }

        private Dictionary<History, BranchLine> _branchLines;

        //private class Row
        //{
        //    public Row(HistoryEvent historyEvent)
        //    {
        //        _historyEvent = historyEvent;
        //        _cells = new List<Cell>();
        //    }

        //    public HistoryEvent HistoryEvent
        //    {
        //        get
        //        {
        //            return _historyEvent;
        //        }
        //    }

        //    public List<Cell> Cells
        //    {
        //        get
        //        {
        //            return _cells;
        //        }
        //    }

        //    private List<Cell> _cells;
        //    private HistoryEvent _historyEvent;
        //}

        //private class Cell
        //{
        //    public Cell(HistoryEvent historyEvent, int x)
        //    {
        //        _historyEvent = historyEvent;
        //        _x = x;
        //        //_terminus = terminus;
        //    }

        //    public Cell(Cell other)
        //    {
        //        _historyEvent = other._historyEvent;
        //        _x = other._x;
        //    }

        //    public HistoryEvent HistoryEvent
        //    {
        //        get
        //        {
        //            return _historyEvent;
        //        }
        //    }

        //    public int X
        //    {
        //        get
        //        {
        //            return _x;
        //        }
        //    }

        //    //public bool Terminus
        //    //{
        //    //    get
        //    //    {
        //    //        return _terminus;
        //    //    }
        //    //}

        //    private HistoryEvent _historyEvent;
        //    private int _x;
        //    //private bool _terminus;
        //}

        //private OrderedList<UInt64, OldRow> _rows;
        //private Dictionary<HistoryEvent, OrderedList<UInt64, Tuple<HistoryEvent, BranchInfo>>> _rowsByHistoryEvent;

        //private class OldRow
        //{
        //    public OldRow()
        //    {
        //        _branchBits = new OrderedList<ulong, BranchInfo>();
        //    }

        //    public BranchInfo Branch
        //    {
        //        get
        //        {
        //            return _branch;
        //        }
        //    }

        //    public OrderedList<UInt64, BranchInfo> BranchBits
        //    {
        //        get
        //        {
        //            return _branchBits;
        //        }
        //    }

        //    private BranchInfo _branch;
        //    private OrderedList<UInt64, BranchInfo> _branchBits;
        //}

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

        //private class BranchInfo
        //{
        //    public BranchInfo(BranchInfo parent, int x, int y, HistoryEvent historyEvent)
        //    {
        //        _parent = parent;
        //        _x = x;
        //        _y = y;
        //        _historyEvent = historyEvent;
        //    }

        //    public BranchInfo _parent;
        //    public int _x;
        //    public int _y;
        //    public HistoryEvent _historyEvent;
        //}

        //private class OrderedList<V, T> where V : IComparable
        //{
        //    public OrderedList()
        //    {
        //        _values = new List<V>();
        //        _items = new List<T>();
        //    }

        //    public int Add(V value, T item)
        //    {
        //        int index = Index(item);
        //        if (index == -1)
        //        {
        //            // Insert
        //            for (int i = 0; i < _items.Count; i++)
        //            {
        //                if (_values[i].CompareTo(value) < 0)
        //                {
        //                    _values.Insert(i, value);
        //                    _items.Insert(i, item);

        //                    index = i;
        //                    break;
        //                }
        //            }

        //            if (index == -1)
        //            {
        //                index = _items.Count;
        //                _values.Add(value);
        //                _items.Add(item);
        //            }

        //            return index;
        //        }
        //        else
        //        {
        //            // Update
        //            throw new Exception("Adding already existing item!!!");
        //        }
        //    }

        //    public int Update(V value, T item)
        //    {
        //        int index = Index(item);
        //        if (index >= 0)
        //        {
        //            if (_values[index].CompareTo(value) == 0)
        //            {
        //                // Nothing to change!
        //                return index;
        //            }

        //            _values.RemoveAt(index);
        //            _items.RemoveAt(index);

        //            return Add(value, item);
        //        }
        //        else
        //        {
        //            // Oops!
        //            throw new Exception("Updating a nonexitent item!");
        //        }
        //    }

        //    public int Remove(T item)
        //    {
        //        int index = Index(item);
        //        if (index >= 0)
        //        {
        //            _values.RemoveAt(index);
        //            _items.RemoveAt(index);
        //        }

        //        return index;
        //    }

        //    public int Index(T item)
        //    {
        //        return _items.FindIndex(i => ReferenceEquals(i, item));
        //    }

        //    private List<V> _values;
        //    private List<T> _items;
        //}


        //private List<BranchInfo> _branches;
        //private Dictionary<HistoryEvent, BranchInfo> _branchesMap;
    }
}
