using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace CPvC
{
    public class HistoryControl : ListView
    {
        private History _history;

        private List<HistoryEvent> _horizontalOrdering;
        private List<HistoryEvent> _verticalOrdering;

        private Dictionary<HistoryEvent, HistoryEvent> _interestingEventParents;

        private ListTree<HistoryEvent> _listTree;
        private Dictionary<HistoryEvent, ListTreeNode<HistoryEvent>> _eventToNodeMap;
        private HistoryViewNodeList _nodeList;

        public static readonly DependencyProperty HistoryProperty =
            DependencyProperty.Register(
                "History",
                typeof(History),
                typeof(HistoryControl),
                new PropertyMetadata(null, PropertyChangedCallback));

        public HistoryControl()
        {
            _horizontalOrdering = new List<HistoryEvent>();
            _verticalOrdering = new List<HistoryEvent>();
            _interestingEventParents = new Dictionary<HistoryEvent, HistoryEvent>();
            _eventToNodeMap = new Dictionary<HistoryEvent, ListTreeNode<HistoryEvent>>();
            _nodeList = new HistoryViewNodeList();
        }

        private static void PropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            HistoryControl userControl = (HistoryControl)dependencyObject;
            userControl.History = (History)args.NewValue;
        }

        public History History
        {
            get
            {
                return (History)GetValue(HistoryProperty);
            }

            set
            {
                History h = History;
                //if (History == value)
                //{
                //    return;
                //}

                _history = value;
                SetValue(HistoryProperty, value);
                //return _history;

                value.Auditors += ProcessHistoryChange;

                //_history = History;
                SetHistory(value);
            }
        }

        public class HistoryOrderingEventArgs : EventArgs
        {
            public HistoryOrderingEventArgs()
            {
            }

            public HistoryEvent VerticalEvent { get; set; }
            public int OldVerticalIndex { get; set; }
            public int NewVerticalIndex { get; set; }

            //public List<HistoryEvent> HorizontalEvents { get; set; }
            public List<int> HorizonalIndices { get; set; }
        }

        public delegate void HistoryOrderingEventHandler(object sender, PromptForBookmarkEventArgs e);

        private class HistoryEventOrdering
        {
            private List<HistoryEvent> _historyEvents;
            private Dictionary<HistoryEvent, HistoryEvent> _historyEventParents;

            private List<HistoryEvent> _verticalOrdering;
            private List<HistoryEvent> _horizontalOrdering;

            private object _lockObject;

            public event HistoryOrderingEventHandler SomethingChanged;

            public HistoryEventOrdering()
            {
                _historyEvents = new List<HistoryEvent>();
                _historyEventParents = new Dictionary<HistoryEvent, HistoryEvent>();

                _verticalOrdering = new List<HistoryEvent>();
                _horizontalOrdering = new List<HistoryEvent>();

                _lockObject = new object();
            }

            public void Initialize(RootHistoryEvent rootHistoryEvent)
            {
                lock (_lockObject)
                {
                    List<HistoryEvent> children = new List<HistoryEvent>();

                    Stack<Tuple<HistoryEvent, HistoryEvent>> events = new Stack<Tuple<HistoryEvent, HistoryEvent>>();

                    _historyEventParents.Clear();
                    _historyEvents.Clear();

                    _verticalOrdering.Clear();
                    _horizontalOrdering.Clear();

                    events.Push(new Tuple<HistoryEvent, HistoryEvent>(rootHistoryEvent, null));
                    while (events.Any())
                    {
                        (HistoryEvent e, HistoryEvent parent) = events.Pop();

                        HistoryEvent newParent = parent;
                        if (InterestingEvent(e))
                        {
                            _horizontalOrdering.Add(e);
                            _verticalOrdering.Add(e);
                            _historyEventParents.Add(e, parent);
                            newParent = e;
                        }

                        children.Clear();
                        children.AddRange(e.Children);
                        children.Sort((x, y) => x.GetMaxDescendentTicks().CompareTo(y.GetMaxDescendentTicks()));

                        foreach (HistoryEvent child in children)
                        {
                            events.Push(new Tuple<HistoryEvent, HistoryEvent>(child, newParent));
                        }
                    }

                    _verticalOrdering.Sort(VerticalSort);
                }
            }

            public void AddHistoryEvent(HistoryEvent historyEvent)
            {
                HistoryEvent parentHistoryEvent = historyEvent.Parent;
                if (_historyEvents.Contains(parentHistoryEvent))
                {
                    // This event may no longer be interesting!
                    if (!InterestingEvent(parentHistoryEvent))
                    {
                        // Not any more!
                    }
                }


                int parentHistoryEventIndex = GetVerticalIndex(parentHistoryEvent);


            }

            public void UpdateHistoryEvent(HistoryEvent historyEvent)
            {

            }

            public void DeleteHistoryEvent(HistoryEvent historyEvent, bool recursive)
            {

            }

            public HistoryEvent GetParentEvent(HistoryEvent historyEvent)
            {
                return _historyEventParents[historyEvent];
            }

            public int GetVerticalIndex(HistoryEvent historyEvent)
            {
                return _verticalOrdering.FindIndex(x => ReferenceEquals(x, historyEvent));
            }

            public int GetHorizontalIndex(HistoryEvent historyEvent)
            {
                return _horizontalOrdering.FindIndex(x => ReferenceEquals(x, historyEvent));
            }
        }

        public void ProcessHistoryChange(HistoryEvent e, HistoryChangedAction action)
        {
            if (!_eventToNodeMap.ContainsKey(e) && action != HistoryChangedAction.Add)
            {
                // Warning! Need to fix this!
                return;
            }

            Update(e);

            switch (action)
            {
                case HistoryChangedAction.Add:
                    {
                        //ListTree<HistoryEvent> parentNode = null;
                        HistoryEvent p = e.Parent;

                        while (!_eventToNodeMap.ContainsKey(p))
                        {
                            p = p.Parent;
                        }


                        _listTree.AddNode(_eventToNodeMap[p], _eventToNodeMap[e]);
                    }
                    break;
                case HistoryChangedAction.DeleteBranch:
                    _listTree.DeleteNode(_eventToNodeMap[e], true);
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    _listTree.DeleteNode(_eventToNodeMap[e], false);
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    _listTree.UpdateNode(_eventToNodeMap[e]);
                    //Update(e);
                    break;
            }
        }

        public void SetHistory(History history)
        {
            //_history = history;
            GenerateTree();
        }

        private int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            return x.GetMaxDescendentTicks().CompareTo(y.GetMaxDescendentTicks());
        }

        public void GenerateTree()
        {
            //Stack<HistoryEvent> events2 = new Stack<HistoryEvent>();
            //events2.




            //ListTreeNode<HistoryEvent> root = new ListTreeNode<HistoryEvent>(_history.RootEvent, null);

            List<HistoryEvent> children = new List<HistoryEvent>();

            Stack<Tuple<HistoryEvent, ListTreeNode<HistoryEvent>>> events = new Stack<Tuple<HistoryEvent, ListTreeNode<HistoryEvent>>>();

            _interestingEventParents.Clear();

            List<HistoryEvent> verticalOrdering = new List<HistoryEvent>();
            List<HistoryEvent> horizontalOrdering = new List<HistoryEvent>();

            //_verticalOrdering.Clear();
            //_horizontalOrdering.Clear();
            _eventToNodeMap = new Dictionary<HistoryEvent, ListTreeNode<HistoryEvent>>();

            ListTreeNode<HistoryEvent> root = null;

            events.Push(new Tuple<HistoryEvent, ListTreeNode<HistoryEvent>>(_history.RootEvent, null)); // root));
            while (events.Any())
            {
                (HistoryEvent e, ListTreeNode<HistoryEvent> parentNode) = events.Pop();

                _nodeList.Add(e);

                ListTreeNode<HistoryEvent> newParent = parentNode;
                if (InterestingEvent(e)) // && !(e is RootHistoryEvent))
                {
                    ListTreeNode<HistoryEvent> node = new ListTreeNode<HistoryEvent>(e, null);
                    node.Parent = newParent;
                    if (newParent != null)
                    {
                        newParent.Children.Add(node);
                    }
                    _eventToNodeMap.Add(e, node);

                    if (e is RootHistoryEvent)
                    {
                        root = node;
                    }

                    horizontalOrdering.Add(e);
                    verticalOrdering.Add(e);
                    _interestingEventParents.Add(e, newParent?.Obj);
                    newParent = node;
                }

                children.Clear();
                children.AddRange(e.Children);
                children.Sort((x, y) => x.GetMaxDescendentTicks().CompareTo(y.GetMaxDescendentTicks()));

                foreach (HistoryEvent child in children)
                {
                    events.Push(new Tuple<HistoryEvent, ListTreeNode<HistoryEvent>>(child, newParent));
                }
            }

            verticalOrdering.Sort(VerticalSort);


            _verticalOrdering = verticalOrdering;
            _horizontalOrdering = horizontalOrdering;


            _listTree = new ListTree<HistoryEvent>(root, VerticalSort, HorizontalSort);


            ////HistoryViewItem vi = new HistoryViewItem(History.RootEvent);
            ////vi.Events.Add(History.RootEvent);
            ////vi.Draw(null, History.RootEvent);
            //Items.Clear();
            ////Items.Add(vi);

            ////foreach (HistoryViewItem hvi in historyItems)
            ////{
            ////    Items.Add(hvi);
            ////}

            //// Draw items to their respective canvasses.
            //HistoryViewItem next = null;
            //for (int i = historyItems.Count - 1; i >= 0; i--)
            //{
            //    HistoryViewItem item = historyItems[i];
            //    Items.Add(item);
            //    item.Draw(next, History.CurrentEvent);

            //    next = item;
            //}

            Action action = new Action(() => SetItems(verticalOrdering, horizontalOrdering));
            Dispatcher.BeginInvoke(action, null);


        }

        public void SetItems(List<HistoryEvent> verticalOrdering, List<HistoryEvent> horizontalOrdering)
        {
            lock (_verticalOrdering)
            {
                lock (_horizontalOrdering)
                {

                    _verticalOrdering = verticalOrdering;
                    _horizontalOrdering = horizontalOrdering;


                    Dictionary<HistoryEvent, int> horizontalLookup = new Dictionary<HistoryEvent, int>();
                    Dictionary<HistoryEvent, int> verticalLookup = new Dictionary<HistoryEvent, int>();

                    for (int i = 0; i < _horizontalOrdering.Count; i++)
                    {
                        horizontalLookup[_horizontalOrdering[i]] = i;
                    }

                    for (int i = 0; i < _verticalOrdering.Count; i++)
                    {
                        verticalLookup[_verticalOrdering[i]] = i;
                    }

                    List<HistoryViewItem> historyItems = new List<HistoryViewItem>();
                    HistoryViewItem previousViewItem = null;

                    for (int v = 0; v < _verticalOrdering.Count; v++)
                    {
                        HistoryViewItem viewItem = new HistoryViewItem(_verticalOrdering[v]);

                        // Add events; either "passthrough" events, or the actual event for this HistoryViewItem.
                        for (int h = 0; h < _horizontalOrdering.Count; h++)
                        {
                            int verticalHorizontalIndex = verticalLookup[_horizontalOrdering[h]];
                            int previousVerticalIndex = -1;

                            if (_interestingEventParents.TryGetValue(_horizontalOrdering[h], out HistoryEvent previousEvent) && previousEvent != null)
                            {
                                previousVerticalIndex = verticalLookup[previousEvent];
                            }

                            if (previousVerticalIndex < v && v <= verticalHorizontalIndex)
                            {
                                int hindex = viewItem.Events.Count;
                                if (previousViewItem != null)
                                {
                                    int prevIndex = previousViewItem.Events.FindIndex(x => x == _horizontalOrdering[h]);
                                    if (prevIndex == -1)
                                    {
                                        prevIndex = previousViewItem.Events.FindIndex(x => x == previousEvent);
                                    }

                                    if (prevIndex != -1)
                                    {
                                        if (hindex < prevIndex)
                                        {
                                            for (int g = 0; g < prevIndex - hindex; g++)
                                            {
                                                viewItem.Events.Add(null);
                                            }
                                        }
                                    }
                                }

                                viewItem.Events.Add(_horizontalOrdering[h]);
                            }
                        }

                        historyItems.Add(viewItem);

                        previousViewItem = viewItem;
                    }

                    //HistoryViewItem vi = new HistoryViewItem(History.RootEvent);
                    //vi.Events.Add(History.RootEvent);
                    //vi.Draw(null, History.RootEvent);
                    Items.Clear();
                    //Items.Add(vi);

                    //foreach (HistoryViewItem hvi in historyItems)
                    //{
                    //    Items.Add(hvi);
                    //}

                    // Draw items to their respective canvasses.
                    HistoryViewItem next = null;
                    for (int i = historyItems.Count - 1; i >= 0; i--)
                    {
                        HistoryViewItem item = historyItems[i];
                        Items.Add(item);
                        item.Draw(next, _history.CurrentEvent);

                        next = item;
                    }
                }
            }
        }

        public bool Update(HistoryEvent e)
        {
            if (!InterestingEvent(e))
            {
                return false;
            }

            lock (_verticalOrdering)
            {
                lock (_horizontalOrdering)
                {
                    // Has the vertical ordering changed?
                    int verticalPosition = _verticalOrdering.FindIndex(x => x == e);
                    if (verticalPosition == -1)
                    {
                        // Should really notify open history events and pass a paramter saying if its open or not?

                        GenerateTree();
                        return true;
                        //throw new Exception("No vertical position found");
                    }

                    bool change = false;
                    if (verticalPosition >= 1)
                    {
                        HistoryEvent previousEvent = _verticalOrdering[verticalPosition - 1];
                        if (VerticalSort(previousEvent, e) > 0)
                        {
                            change = true;
                        }
                    }

                    if (verticalPosition < (_verticalOrdering.Count - 1))
                    {
                        HistoryEvent previousEvent = _verticalOrdering[verticalPosition + 1];
                        if (VerticalSort(previousEvent, e) < 0)
                        {
                            change = true;
                        }
                    }

                    if (!change)
                    {
                        return false;
                    }

                    GenerateTree();
                }
            }

            return true;
        }

        static private int VerticalSort(HistoryEvent x, HistoryEvent y)
        {
            if (x.Ticks < y.Ticks)
            {
                return -1;
            }
            else if (x.Ticks > y.Ticks)
            {
                return 1;
            }
            else
            {
                if (x.IsEqualToOrAncestorOf(y))
                {
                    return -1;
                }
                else if (y.IsEqualToOrAncestorOf(x))
                {
                    return 1;
                }
            }

            return 0;
        }

        static private bool InterestingEvent(HistoryEvent historyEvent)
        {
            bool interested = false;
            if (historyEvent is RootHistoryEvent)
            {
                interested = true;
            }
            else if (historyEvent is BookmarkHistoryEvent)
            {
                interested = true;
            }
            else if (historyEvent.Children.Count != 1)
            {
                interested = true;
            }

            return interested;
        }

    }
}
