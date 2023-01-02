﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace CPvC
{
    public class ListTreeNode
    {
        public ListTreeNode(HistoryEvent historyEvent)
        {
            HistoryEvent = historyEvent;
            Children = new List<ListTreeNode>();
        }

        public ListTreeNode Parent
        {
            get;
            set;
        }

        public List<ListTreeNode> Children
        {
            get;
        }

        public HistoryEvent HistoryEvent
        {
            get;
            set;
        }
    }

    public class ListTree
    {
        public ListTree(RootHistoryEvent rootEvent)
        {
            _verticalNodes = new List<ListTreeNode>();
            _horizontalNodes = new List<ListTreeNode>();
            _verticalPositions = new Dictionary<ListTreeNode, int>();
            _horizontalPositions = new Dictionary<ListTreeNode, int>();
            _eventsToNodes = new Dictionary<HistoryEvent, ListTreeNode>();

            Root = new ListTreeNode(rootEvent);
            _eventsToNodes.Add(rootEvent, Root);
            _verticalNodes.Add(Root);
            _horizontalNodes.Add(Root);
            _verticalPositions.Add(Root, 0);
            _horizontalPositions.Add(Root, 0);
        }

        public ListTreeNode Root
        {
            get;
        }

        public System.Drawing.Point GetPosition(ListTreeNode node)
        {
            return new System.Drawing.Point(_horizontalPositions[node], _verticalPositions[node]);
        }

        public ListTreeNode GetNode(HistoryEvent historyEvent)
        {
            if (_eventsToNodes.TryGetValue(historyEvent, out ListTreeNode node))
            {
                return node;
            }

            return null;
        }

        public void Update(HistoryEvent oldHistoryEvent, HistoryEvent newHistoryEvent)
        {
            ListTreeNode node = GetNode(oldHistoryEvent);

            //if (ReferenceEquals(node.HistoryEvent, newHistoryEvent))
            //{
            //    return;
            //}



            _eventsToNodes.Remove(oldHistoryEvent);
            node.HistoryEvent = newHistoryEvent;
            Update(node);
            _eventsToNodes.Add(newHistoryEvent, node);
        }

        public ListTreeNode Add(ListTreeNode parent, HistoryEvent historyEvent)
        {
            ListTreeNode child = new ListTreeNode(historyEvent);

            // Check first if child already exists in the tree! And that parent exists in the tree!

            // Insert into children of parent first!
            // But! We must either add a new child, or inject this child inbetween the parent and an existing child!
            // Which case are we?
            int descendentChildIndex = 0;
            while (descendentChildIndex < parent.Children.Count)
            {
                if (child.HistoryEvent.IsEqualToOrAncestorOf(parent.Children[descendentChildIndex].HistoryEvent))
                {
                    break;
                }

                descendentChildIndex++;
            }

            int childIndex = 0;
            if (descendentChildIndex < parent.Children.Count)
            {
                ListTreeNode descendentChild = parent.Children[descendentChildIndex];
                childIndex = descendentChildIndex;

                child.Children.Add(descendentChild);
                descendentChild.Parent = child;

                parent.Children.RemoveAt(descendentChildIndex);
            }
            else
            {
                childIndex = GetChildIndex(parent, child);
            }

            parent.Children.Insert(childIndex, child);
            child.Parent = parent;
            _eventsToNodes.Add(child.HistoryEvent, child);

            // Insert into horizontal events!
            int previousHorizontalIndex = _horizontalPositions[parent];
            if (childIndex > 0)
            {
                // Find the "right"-most descendent of the previous child!
                ListTreeNode node = RightmostDescendent(parent.Children[childIndex - 1]);
                previousHorizontalIndex = _horizontalPositions[node];
            }

            _horizontalNodes.Insert(previousHorizontalIndex + 1, child);

            RefreshHorizontalPositions(previousHorizontalIndex + 1);

            // Insert vertically!
            int verticalIndex = GetVerticalIndex(child);
            _verticalNodes.Insert(verticalIndex, child);

            RefreshVerticalPositions(verticalIndex);


            // Raise some kind of event here!


            return child;
        }

        public void RemoveRecursive(ListTreeNode child)
        {
            ListTreeNode parent = child.Parent;

            // Find the child!
            int childIndex = 0;
            while (childIndex < parent.Children.Count)
            {
                if (ReferenceEquals(parent.Children[childIndex], child))
                {
                    break;
                }

                childIndex++;
            }

            parent.Children.RemoveAt(childIndex);
            child.Parent = null;
            _eventsToNodes.Remove(child.HistoryEvent);


            // Figure out the right-most descendent...
            ListTreeNode node = RightmostDescendent(child);
            //while (node.Children.Count > 0)
            //{
            //    node = node.Children[node.Children.Count - 1];
            //}

            int leftmostHorizontalIndex = _horizontalPositions[child];
            int rightmostHorizontalIndex = _horizontalPositions[node];

            // Before removing these from the horizontal ordering, use this to delete from the vertical first!
            int childVertical = _verticalPositions[child];

            for (int i = leftmostHorizontalIndex; i <= rightmostHorizontalIndex; i++)
            {
                _verticalNodes.Remove(_horizontalNodes[i]);
                _verticalPositions.Remove(_horizontalNodes[i]);

                _horizontalPositions.Remove(_horizontalNodes[i]);
            }

            // Bump up all the existing horizontal indexes first!
            RefreshVerticalPositions(childVertical);
            //for (int i = childVertical; i < _verticalNodes.Count; i++)
            //{
            //    _verticalPositions[_verticalNodes[i]] = i;
            //}


            // Remove these from the horizontal orderings...
            _horizontalNodes.RemoveRange(leftmostHorizontalIndex, rightmostHorizontalIndex - leftmostHorizontalIndex + 1);

            RefreshHorizontalPositions(leftmostHorizontalIndex);
            //// Bump up all the existing horizontal indexes first!
            //for (int i = leftmostHorizontalIndex; i < _horizontalNodes.Count; i++)
            //{
            //    _horizontalPositions[_horizontalNodes[i]] = i;
            //}
        }

        public void RemoveNonRecursive(ListTreeNode child)
        {
            // Todo!
        }

        public void Update(ListTreeNode node)
        {
            // The sort order may have changed!! Check it!
            bool verticalChanged = AdjustVerticalOrderIfNeeded(node);
            bool horizontalChanged = AdjustHorizontalOrderIfNeeded(node);

            if (verticalChanged)
            {
                ListTreeNode parent = node.Parent;
                while (parent != null)
                {
                    AdjustHorizontalOrderIfNeeded(parent);
                    parent = parent.Parent;
                }
            }

            if (verticalChanged || horizontalChanged)
            {
                DebugDump();
            }
        }

        private void RefreshHorizontalPositions(int index)
        {
            for (int i = index; i < _horizontalNodes.Count; i++)
            {
                _horizontalPositions[_horizontalNodes[i]] = i;
            }
        }

        private void RefreshVerticalPositions(int index)
        {
            for (int i = index; i < _verticalNodes.Count; i++)
            {
                _verticalPositions[_verticalNodes[i]] = i;
            }
        }

        private int GetVerticalIndex(ListTreeNode node)
        {
            int verticalIndex = 0;
            while (verticalIndex < _verticalNodes.Count)
            {
                if (VerticalSort(_verticalNodes[verticalIndex].HistoryEvent, node.HistoryEvent) > 0)
                {
                    break;
                }

                verticalIndex++;
            }

            return verticalIndex;
        }

        private int GetChildIndex(ListTreeNode parent, ListTreeNode child)
        {
            int childIndex = 0;
            while (childIndex < parent.Children.Count)
            {
                if (HorizontalSort(child.HistoryEvent, parent.Children[childIndex].HistoryEvent) < 0)
                {
                    break;
                }

                childIndex++;
            }

            return childIndex;
        }

        private int ChildIndex(ListTreeNode node)
        {
            int childIndex = 0;
            while (childIndex < node.Parent.Children.Count)
            {
                if (ReferenceEquals(node, node.Parent.Children[childIndex]))
                {
                    return childIndex;
                }

                childIndex++;
            }

            return -1;
        }

        private ListTreeNode RightmostDescendent(ListTreeNode node)
        {
            while (node.Children.Count > 0)
            {
                node = node.Children[node.Children.Count - 1];
            }

            return node;
        }

        private bool AdjustHorizontalOrderIfNeeded(ListTreeNode node)
        {
            if (node.Parent == null)
            {
                return false;
            }

            int childIndex = ChildIndex(node);
            //while (childIndex < node.Parent.Children.Count)
            //{
            //    if (ReferenceEquals(node, node.Parent.Children[childIndex]))
            //    {
            //        break;
            //    }

            //    childIndex++;
            //}

            bool horizontalPositionChanged = false;
            if (childIndex > 0)
            {
                if (HorizontalSort(node.Parent.Children[childIndex - 1].HistoryEvent, node.HistoryEvent) >= 0)
                {
                    horizontalPositionChanged = true;
                }
            }

            if (!horizontalPositionChanged && (childIndex + 1 < node.Parent.Children.Count))
            {
                if (HorizontalSort(node.HistoryEvent, node.Parent.Children[childIndex + 1].HistoryEvent) >= 0)
                {
                    horizontalPositionChanged = true;
                }
            }

            if (!horizontalPositionChanged)
            {
                return false;
            }

            // Horizontal has changed!
            node.Parent.Children.RemoveAt(childIndex);

            int newChildIndex = 0;
            while (newChildIndex < node.Parent.Children.Count)
            {
                if (HorizontalSort(node.HistoryEvent, node.Parent.Children[newChildIndex].HistoryEvent) < 0)
                {
                    break;
                }

                newChildIndex++;
            }

            node.Parent.Children.Insert(newChildIndex, node);

            // Fix up the horizontal ordering!

            // Figure out the right-most descendent...
            ListTreeNode node2 = RightmostDescendent(node);
            //while (node2.Children.Count > 0)
            //{
            //    node2 = node2.Children[node2.Children.Count - 1];
            //}

            int leftmostHorizontalIndex = _horizontalPositions[node];
            int rightmostHorizontalIndex = _horizontalPositions[node2];

            // Where is the new leftmost horizontal index?
            int previousHorizontalIndex = _horizontalPositions[node.Parent];
            if (newChildIndex > 0)
            {
                ListTreeNode r = RightmostDescendent(node.Parent.Children[newChildIndex - 1]);
                //while (r.Children.Count > 0)
                //{
                //    r = r.Children[r.Children.Count - 1];
                //}

                previousHorizontalIndex = _horizontalPositions[r];
            }

            // Move them!
            if (previousHorizontalIndex < leftmostHorizontalIndex)
            {
                int oldIndex = leftmostHorizontalIndex;
                int newIndex = previousHorizontalIndex + 1;

                while (oldIndex <= rightmostHorizontalIndex)
                {
                    ListTreeNode temp = _horizontalNodes[oldIndex];
                    _horizontalNodes.RemoveAt(oldIndex);
                    _horizontalNodes.Insert(newIndex, temp);
                    oldIndex++;
                    newIndex++;
                }

                RefreshHorizontalPositions(previousHorizontalIndex + 1);
            }
            else
            {
                int oldIndex = leftmostHorizontalIndex;
                int newIndex = previousHorizontalIndex + 1;
                int count = rightmostHorizontalIndex - leftmostHorizontalIndex + 1;

                while (count > 0)
                {
                    ListTreeNode temp = _horizontalNodes[oldIndex];
                    _horizontalNodes.Insert(newIndex, temp);
                    _horizontalNodes.RemoveAt(oldIndex);
                    //oldIndex++;
                    //newIndex++;
                }

                RefreshHorizontalPositions(leftmostHorizontalIndex);
            }

            return true;
        }

        private bool AdjustVerticalOrderIfNeeded(ListTreeNode node)
        {
            int verticalIndex = _verticalPositions[node];

            bool verticalPositionChanged = false;
            if (verticalIndex > 0)
            {
                if (VerticalSort(_verticalNodes[verticalIndex - 1].HistoryEvent, node.HistoryEvent) >= 0)
                {
                    verticalPositionChanged = true;
                }
            }

            if (!verticalPositionChanged && (verticalIndex + 1 < _verticalNodes.Count))
            {
                if (VerticalSort(node.HistoryEvent, _verticalNodes[verticalIndex + 1].HistoryEvent) >= 0)
                {
                    verticalPositionChanged = true;
                }
            }

            if (!verticalPositionChanged)
            {
                return false;
            }

            // Vertical has changed!
            _verticalNodes.RemoveAt(verticalIndex);

            int newVerticalIndex = 0;
            while (newVerticalIndex < _verticalNodes.Count)
            {
                if (VerticalSort(node.HistoryEvent, _verticalNodes[newVerticalIndex].HistoryEvent) < 0)
                {
                    break;
                }

                newVerticalIndex++;
            }

            _verticalNodes.Insert(newVerticalIndex, node);

            RefreshVerticalPositions(Math.Min(verticalIndex, newVerticalIndex));

            //// Bump up all the existing horizontal indexes first!
            //for (int i = newVerticalIndex; i < _verticalNodes.Count; i++)
            //{
            //    _verticalPositions[_verticalNodes[i]] = i;
            //}

            return true;
        }

        private int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            UInt64 yMax = y.GetMaxDescendentTicks();
            UInt64 xMax = x.GetMaxDescendentTicks();
            int result = yMax.CompareTo(xMax);

            if (result != 0)
            {
                return result;
            }

            return y.Id.CompareTo(x.Id);
        }

        private int VerticalSort(HistoryEvent x, HistoryEvent y)
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
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x.IsEqualToOrAncestorOf(y))
                {
                    return -1;
                }
                else if (y.IsEqualToOrAncestorOf(x))
                {
                    return 1;
                }
            }

            //_verticalTies.Add(new Tuple<HistoryEvent, HistoryEvent>(x, y));

            return x.Id.CompareTo(y.Id);
        }

        public void DebugDump()
        {
            CPvC.Diagnostics.Trace("Horizontal/Vertical tree:\n");

            for (int v = 0; v < _verticalNodes.Count; v++)
            {
                int h = _horizontalPositions[_verticalNodes[v]];

                string str = new string(' ', h * 2);

                str += String.Format("{0}", _verticalNodes[v].HistoryEvent.Id);

                CPvC.Diagnostics.Trace(str);
            }

            CPvC.Diagnostics.Trace("Tree hierarchy:\n");

            List<ListTreeNode> nodes = new List<ListTreeNode>();
            nodes.Add(Root);
            while (nodes.Any())
            {
                ListTreeNode node = nodes[0];
                nodes.RemoveAt(0);

                int indent = 0;
                ListTreeNode p = node.Parent;
                while (p != null)
                {
                    p = p.Parent;
                    indent++;
                }

                string str = new string(' ', indent * 2);
                str += String.Format("{0}", node.HistoryEvent.Id);

                CPvC.Diagnostics.Trace(str);

                indent++;

                nodes.InsertRange(0, node.Children);
            }
        }

        private List<ListTreeNode> _verticalNodes;
        private List<ListTreeNode> _horizontalNodes;

        // Lookup helpers
        private Dictionary<ListTreeNode, int> _verticalPositions;
        private Dictionary<ListTreeNode, int> _horizontalPositions;
        private Dictionary<HistoryEvent, ListTreeNode> _eventsToNodes;
    }


    public enum NotifyListChangedAction
    {
        Added,
        Moved,
        Replaced,
        Removed,
        Cleared
    }

    public class NotifyPositionChangedEventArgs : EventArgs
    {
        public NotifyPositionChangedEventArgs(NotifyListChangedAction action, int oldHorizontalIndex, int newHorizontalIndex, int oldVerticalIndex, int newVerticalIndex, HistoryEvent oldItem, HistoryEvent newItem, HistoryEvent oldInterestingParent, HistoryEvent newInterestingParent)
        {
            Action = action;
            OldHorizontalIndex = oldHorizontalIndex;
            NewHorizontalIndex = newHorizontalIndex;
            OldVerticalIndex = oldVerticalIndex;
            NewVerticalIndex = newVerticalIndex;
            OldItem = oldItem;
            NewItem = newItem;
            OldInterestingParent = oldInterestingParent;
            NewInterestingParent = newInterestingParent;
        }

        public NotifyListChangedAction Action { get; }
        public int OldHorizontalIndex { get; }
        public int NewHorizontalIndex { get; }
        public int OldVerticalIndex { get; }
        public int NewVerticalIndex { get; }
        public HistoryEvent OldItem { get; }
        public HistoryEvent NewItem { get; }
        public HistoryEvent OldInterestingParent { get; }
        public HistoryEvent NewInterestingParent { get; }
    }

    public delegate void NotifyPositionChangedEventHandler(object sender, NotifyPositionChangedEventArgs e);


    public class NotifyListChangedEventArgs : EventArgs
    {
        //public NotifyListChangedEventArgs(NotifyListChangedAction action)
        //{
        //    Action = action;
        //}

        static public NotifyListChangedEventArgs Added(int index, object item)
        {
            return new NotifyListChangedEventArgs(NotifyListChangedAction.Added, -1, null, index, item);
        }

        static public NotifyListChangedEventArgs Moved(int oldIndex, object oldItem, int newIndex, object newItem)
        {
            return new NotifyListChangedEventArgs(NotifyListChangedAction.Moved, oldIndex, oldItem, newIndex, newItem);
        }

        static public NotifyListChangedEventArgs Replaced(int index, object oldItem, object newItem)
        {
            return new NotifyListChangedEventArgs(NotifyListChangedAction.Replaced, index, oldItem, index, newItem);
        }

        static public NotifyListChangedEventArgs Removed(int index, object item)
        {
            return new NotifyListChangedEventArgs(NotifyListChangedAction.Removed, index, item, -1, null);
        }

        static public NotifyListChangedEventArgs Cleared()
        {
            return new NotifyListChangedEventArgs(NotifyListChangedAction.Removed, -1, null, -1, null);
        }

        private NotifyListChangedEventArgs(NotifyListChangedAction action, int oldIndex, object oldItem, int newIndex, object newItem)
        {
            Action = action;
            OldIndex = oldIndex;
            OldItem = oldItem;
            NewIndex = newIndex;
            NewItem = newItem;
        }

        public NotifyListChangedAction Action { get; }
        public int OldIndex { get; }
        public int NewIndex { get; }
        public object OldItem { get; }
        public object NewItem { get; }
    }

    public delegate void NotifyListChangedEventHandler(object sender, NotifyListChangedEventArgs e);



    public class ObservableList<T> : IEnumerable<T>
    {
        public ObservableList()
        {
            _list = new List<T>();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public T GetAt(int index)
        {
            return _list[index];
        }

        public void SetAt(int index, T t)
        {
            _list[index] = t;

            NotifyListChangedEventArgs args = NotifyListChangedEventArgs.Added(index, t);
            ListChanged?.Invoke(this, args);
        }

        public T this[int key]
        {
            get => GetAt(key);
            set => SetAt(key, value);
        }

        public void Add(T t)
        {
            _list.Add(t);

            NotifyListChangedEventArgs args = NotifyListChangedEventArgs.Added(_list.Count - 1, t);
            ListChanged?.Invoke(this, args);
        }

        public void Insert(int index, T t)
        {
            _list.Insert(index, t);

            NotifyListChangedEventArgs args = NotifyListChangedEventArgs.Added(index, t);
            ListChanged?.Invoke(this, args);
        }

        public int FindIndex(T t)
        {
            return _list.FindIndex(x => ReferenceEquals(x, t));
        }

        public void Remove(T t)
        {
            int index = FindIndex(t);
            if (index == -1)
            {
                return;
            }

            RemoveAt(index);
        }

        public void RemoveAt(int index)
        {
            NotifyListChangedEventArgs args = NotifyListChangedEventArgs.Removed(index, _list[index]);
            _list.RemoveAt(index);

            ListChanged?.Invoke(this, args);
        }

        public void Sort(Comparison<T> comparison)
        {
            _list.Sort(comparison);

            // Not ideal!
            NotifyListChangedEventArgs args = NotifyListChangedEventArgs.Cleared();
            ListChanged?.Invoke(this, args);

            for (int i = 0; i < _list.Count; i++)
            {
                args = NotifyListChangedEventArgs.Added(i, _list[i]);
                ListChanged?.Invoke(this, args);
            }
        }

        public void Clear()
        {
            _list.Clear();

            NotifyListChangedEventArgs args = NotifyListChangedEventArgs.Cleared();
            ListChanged?.Invoke(this, args);
        }

        public bool Contains(T t)
        {
            return _list.Contains(t);
        }

        public int Count
        {
            get
            {
                return _list.Count;
            }
        }

        public event NotifyListChangedEventHandler ListChanged;

        private List<T> _list;
    }

    public class HistoryEventOrderings
    {
        public HistoryEventOrderings()
        {
            _history = null;
            //_fullRefresh = false;
            _items = null;
            _horizontalEvents = new ObservableList<HistoryEvent>();
            _verticalEvents = new ObservableList<HistoryEvent>();
            _verticalTies = new HashSet<Tuple<HistoryEvent, HistoryEvent>>();
        }

        public ObservableList<HistoryEvent> HorizontalOrdering()
        {
            return _horizontalEvents;
        }

        public ObservableList<HistoryEvent> VerticalOrdering()
        {
            return _verticalEvents;
        }

        public bool Contains(HistoryEvent historyEvent)
        {
            return _verticalPosition.ContainsKey(historyEvent);
        }

        public HistoryEvent ParentEvent(HistoryEvent historyEvent)
        {
            if (!_interestingParentEvents.ContainsKey(historyEvent))
            {
                string y = "";
            }

            return _interestingParentEvents[historyEvent];
        }

        public int VerticalPosition(HistoryEvent historyEvent)
        {
            return _verticalPosition[historyEvent];
        }

        public void SetHistory(History history)
        {
            _history = history;
            //_fullRefresh = true;
            _items = new List<HistoryViewItem>();

            Init(history);
        }

        private void CheckForHorizontalChanges(HistoryEvent historyEvent)
        {
            HashSet<HistoryEvent> eventsToInvalidate = new HashSet<HistoryEvent>();

            while (historyEvent?.Parent != null)
            {
                List<HistoryEvent> siblings = historyEvent.Parent.Children;

                int index = siblings.FindIndex(x => ReferenceEquals(x, historyEvent));
                if (index < 0)
                {
                    throw new Exception("Event could not be found in its parent's children!");
                }

                bool moved = false;
                //int v = _verticalPosition[historyEvent];
                if (index < siblings.Count - 1)
                {
                    if (HorizontalSort(historyEvent, siblings[index + 1]) >= 0)
                    {
                        moved = true;
                    }
                }

                if (!moved && index > 0)
                {
                    if (HorizontalSort(siblings[index - 1], historyEvent) >= 0)
                    {
                        moved = true;
                    }
                }

                if (moved)
                {
                    // Figure out all the history events that have moved in the list of siblings.
                    // Set all of their corresponding HistoryViewItems as "needs redraw" and update
                    // their Events. Include all of their descendents too.
                    List<HistoryEvent> sortedSiblings = new List<HistoryEvent>(siblings);
                    sortedSiblings.Sort((x, y) => HorizontalSort(x, y));

                    for (int i = 0; i < siblings.Count; i++)
                    {
                        if (!ReferenceEquals(siblings[i], sortedSiblings[i]))
                        {
                            eventsToInvalidate.Add(siblings[i]);

                            // Add all descendents too!
                            List<HistoryEvent> descendents = new List<HistoryEvent>();
                            descendents.Add(siblings[i]);
                            while (descendents.Any())
                            {
                                HistoryEvent h = descendents[0];
                                descendents.RemoveAt(0);

                                eventsToInvalidate.Add(h);

                                descendents.AddRange(h.Children);
                            }
                        }
                    }
                }

                historyEvent = historyEvent.Parent;
            }

            foreach (HistoryViewItem item in _items)
            {
                if (eventsToInvalidate.Contains(item.HistoryEvent))
                {
                    item.Invalidate();
                }
            }
        }

        private bool VerticalPositionChanged(HistoryEvent historyEvent)
        {
            // Has "interestingness" changed?
            if (InterestingEvent(historyEvent))
            {
                if (!_verticalEvents.Contains(historyEvent))
                {
                    return true;
                }
            }
            else
            {
                return _verticalEvents.Contains(historyEvent);
            }

            int v = _verticalPosition[historyEvent];
            if (v < _verticalEvents.Count - 1)
            {
                if (VerticalSort(historyEvent, _verticalEvents[v + 1]) >= 0)
                {
                    return true;
                }

                // Need to find a more elegant solution to "ties" than this. Perhaps create my own sorting mechanism that records ties?
                Tuple<HistoryEvent, HistoryEvent> pair = new Tuple<HistoryEvent, HistoryEvent>(historyEvent, _verticalEvents[v + 1]);
                if (_verticalTies.Contains(pair))
                {
                    _verticalTies.Remove(pair);
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pairReverse = new Tuple<HistoryEvent, HistoryEvent>(_verticalEvents[v + 1], historyEvent);
                if (_verticalTies.Contains(pairReverse))
                {
                    _verticalTies.Remove(pairReverse);
                    return true;
                }
            }

            if (v > 0)
            {
                if (VerticalSort(_verticalEvents[v - 1], historyEvent) >= 0)
                {
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pair = new Tuple<HistoryEvent, HistoryEvent>(_verticalEvents[v - 1], historyEvent);
                if (_verticalTies.Contains(pair))
                {
                    _verticalTies.Remove(pair);
                    return true;
                }

                Tuple<HistoryEvent, HistoryEvent> pairReverse = new Tuple<HistoryEvent, HistoryEvent>(historyEvent, _verticalEvents[v - 1]);
                if (_verticalTies.Contains(pairReverse))
                {
                    _verticalTies.Remove(pairReverse);
                    return true;
                }
            }

            return false;
        }

        private void CalculateHorizontalEvent()
        {
            Init(_history);

            return;

            _horizontalEvents.Clear();
            List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();

            horizontalEvents.Add(_history.RootEvent);
            while (horizontalEvents.Any())
            {
                HistoryEvent horizontalEvent = horizontalEvents[0];
                horizontalEvents.RemoveAt(0);

                if (InterestingEvent(horizontalEvent))
                {
                    _horizontalEvents.Add(horizontalEvent);
                }

                horizontalEvents.AddRange(horizontalEvent.Children);
            }

            _interestingParentEvents.Clear();
            foreach (HistoryEvent historyEvent in _verticalEvents)
            {
                HistoryEvent parentEvent = historyEvent.Parent;
                while (parentEvent != null && !_verticalEvents.Contains(parentEvent))
                {
                    parentEvent = parentEvent.Parent;
                }

                _interestingParentEvents.Add(historyEvent, parentEvent);
            }
        }

        private void Add2(HistoryEvent historyEvent)
        {
            HistoryEvent parentEvent = historyEvent.Parent;
            bool parentWasInteresting = _verticalPosition.TryGetValue(parentEvent, out int parentVerticalPosition);
            bool parentIsInteresting = InterestingEvent(parentEvent);

            // Consider each possibility...
            if (parentWasInteresting && parentIsInteresting)
            {
                // Parent's interestingness hasn't changed. Just add the new history event if it's interesting!

            }
            else if (parentWasInteresting && !parentIsInteresting)
            {
                // Parent was interesting, but no longer is. Replace the parent with the child!
                bool verticalPositionUnchanged = false;
                if (parentVerticalPosition >= _verticalEvents.Count - 1 || VerticalSort(historyEvent, _verticalEvents[parentVerticalPosition + 1]) <= 0)
                {
                    if (parentVerticalPosition <= 0 || VerticalSort(_verticalEvents[parentVerticalPosition - 1], historyEvent) <= 0)
                    {
                        verticalPositionUnchanged = true;
                    }
                }

            }
            else if (!parentWasInteresting && parentIsInteresting)
            {
                // Parent wasn't interesing, but now is. Add it and historyEvent.

            }
            else if (!parentWasInteresting && !parentIsInteresting)
            {
                // This shouldn't happen, but just add the historyEvent as in the first case above.

            }

        }

        private void Add(HistoryEvent historyEvent)
        {
            HistoryEvent parentEvent = historyEvent.Parent;

            if (_verticalPosition.TryGetValue(parentEvent, out int parentVerticalPosition))
            {
                bool parentIsInteresting = InterestingEvent(parentEvent);
                bool interesting = InterestingEvent(historyEvent);
                if (!parentIsInteresting && interesting)
                {
                    bool verticalPositionUnchanged = false;
                    if (parentVerticalPosition >= _verticalEvents.Count - 1 || VerticalSort(historyEvent, _verticalEvents[parentVerticalPosition + 1]) <= 0)
                    {
                        if (parentVerticalPosition <= 0 || VerticalSort(_verticalEvents[parentVerticalPosition - 1], historyEvent) <= 0)
                        {
                            verticalPositionUnchanged = true;
                        }
                    }

                    if (verticalPositionUnchanged)
                    {
                        //foreach (HistoryViewItem item in _items)
                        {
                            //if (ReferenceEquals(item.HistoryEvent, parentEvent))
                            //{
                            //    item.HistoryEvent = historyEvent;

                            _verticalEvents[parentVerticalPosition] = historyEvent;
                            _verticalPosition.Remove(parentEvent);
                            _verticalPosition.Add(historyEvent, parentVerticalPosition);

                            int hpos = _horizontalEvents.FindIndex(parentEvent);
                            if (hpos == -1)
                            {
                                throw new Exception("Should have been here!");
                            }

                            _horizontalEvents[hpos] = historyEvent;

                            NotifyPositionChangedEventArgs args = new NotifyPositionChangedEventArgs(
                                NotifyListChangedAction.Replaced,
                                -1,
                                _horizontalEvents.FindIndex(historyEvent),
                                -1,
                                VerticalPosition(historyEvent),
                                parentEvent,
                                historyEvent,
                                ParentEvent(parentEvent),
                                ParentEvent(parentEvent));

                            HistoryEvent interestingParentEvent = _interestingParentEvents[parentEvent];
                            _interestingParentEvents.Add(historyEvent, interestingParentEvent);
                            _interestingParentEvents.Remove(parentEvent);

                            PositionChanged?.Invoke(this, args);

                            return;
                            //}
                        }
                    }
                }

                // Remove parent from vertical if it's no longer interesting!
                if (!parentIsInteresting)
                {
                    _verticalEvents.RemoveAt(parentVerticalPosition);
                    _verticalPosition.Remove(parentEvent);
                    _horizontalEvents.Remove(parentEvent);
                    _interestingParentEvents.Remove(parentEvent);
                }
            }

            // Add to vertical
            bool inserted = false;
            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                if (VerticalSort(historyEvent, _verticalEvents[v]) < 0)
                {
                    _verticalEvents.Insert(v, historyEvent);
                    _verticalPosition[historyEvent] = v;

                    // SHove all the rest down a spot!
                    for (int j = v + 1; j < _verticalEvents.Count; j++)
                    {
                        _verticalPosition[_verticalEvents[j]] = j;
                    }

                    inserted = true;
                    break;
                }
            }


            if (!inserted)
            {
                _verticalEvents.Add(historyEvent);
                _verticalPosition[historyEvent] = _verticalEvents.Count - 1;
            }

            CalculateHorizontalEvent();

            //_fullRefresh = true;
        }

        public List<HistoryViewItem> UpdateItems()
        {
            //if (!_fullRefresh)
            //{
            //    return _items;
            //}

            // Do a full refresh!
            Stopwatch sw = Stopwatch.StartNew();
            Init(_history);
            sw.Stop();

            CPvC.Diagnostics.Trace("Orderings Init took {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();
            List<HistoryViewItem> historyItems = _verticalEvents.Select(x => new HistoryViewItem(x)).ToList();

            List<HistoryEvent> horizontalEvents = new List<HistoryEvent>();
            horizontalEvents.Add(_history.RootEvent);

            while (horizontalEvents.Any())
            {
                HistoryEvent horizontalEvent = horizontalEvents[0];
                horizontalEvents.RemoveAt(0);

                if (InterestingEvent(horizontalEvent))
                {
                    _horizontalEvents.Add(horizontalEvent);

                    int v = _verticalPosition[horizontalEvent];

                    HistoryEvent parentEvent = _interestingParentEvents[horizontalEvent];
                    int parentVerticalPosition = parentEvent != null ? _verticalPosition[parentEvent] : -1;
                    int parentHorizontalPosition = parentEvent != null ? historyItems[parentVerticalPosition].Events.FindIndex(x => ReferenceEquals(x, parentEvent)) : 0;

                    // "Draw" the history event from pv + 1 to v
                    for (int d = parentVerticalPosition + 1; d <= v; d++)
                    {
                        HistoryViewItem historyViewItem = historyItems[d];

                        // Pad out the Events to ensure the line connecting us to our parent never moves to the left.... it just looks better that way!
                        for (int padIndex = historyViewItem.Events.Count; padIndex < parentHorizontalPosition; padIndex++)
                        {
                            historyViewItem.Events.Add(null);
                        }

                        historyViewItem.Events.Add(horizontalEvent);
                        parentHorizontalPosition = Math.Max(parentHorizontalPosition, historyViewItem.Events.Count - 1);
                    }
                }

                List<HistoryEvent> children = horizontalEvent.Children;
                children.Sort((x, y) => HorizontalSort(x, y));
                horizontalEvents.InsertRange(0, children);
            }

            //foreach (HistoryEvent horizontalEvent in _horizontalEvents)
            //{
            //    int v = _verticalPosition[horizontalEvent];

            //    HistoryEvent parentEvent = _interestingParentEvents[horizontalEvent];
            //    int parentVerticalPosition = parentEvent != null ? _verticalPosition[parentEvent] : -1;
            //    int parentHorizontalPosition = parentEvent != null ? historyItems[parentVerticalPosition].Events.FindIndex(x => ReferenceEquals(x, parentEvent)) : 0;

            //    // "Draw" the history event from pv + 1 to v
            //    for (int d = parentVerticalPosition + 1; d <= v; d++)
            //    {
            //        HistoryViewItem historyViewItem = historyItems[d];

            //        // Pad out the Events to ensure the line connecting us to our parent never moves to the left.... it just looks better that way!
            //        for (int padIndex = historyViewItem.Events.Count; padIndex < parentHorizontalPosition; padIndex++)
            //        {
            //            historyViewItem.Events.Add(null);
            //        }

            //        historyViewItem.Events.Add(horizontalEvent);
            //        parentHorizontalPosition = Math.Max(parentHorizontalPosition, historyViewItem.Events.Count - 1);
            //    }
            //}

            sw.Stop();
            CPvC.Diagnostics.Trace("Orderings Draw and padding took {0}ms", sw.ElapsedMilliseconds);

            _items = historyItems;
            //_fullRefresh = false;

            return historyItems;
        }

        public bool Process(HistoryEvent e, HistoryChangedAction action)
        {
            //if (_fullRefresh)
            //{
            //    // No need to check, we're refreshing the tree anyway!
            //    return false;
            //}

            switch (action)
            {
                case HistoryChangedAction.Add:
                    Add(e);
                    break;
                case HistoryChangedAction.DeleteBranch:
                case HistoryChangedAction.DeleteBookmark:
                    //_fullRefresh = true;
                    break;
                case HistoryChangedAction.SetCurrent:
                    CPvC.Diagnostics.Trace("[Process] SetCurrent... doing full refresh!");
                    //_fullRefresh = true;
                    break;
                case HistoryChangedAction.UpdateCurrent:
                    {
                        bool verticalPositionChanged = VerticalPositionChanged(e);
                        if (verticalPositionChanged)
                        {
                            _verticalEvents.Sort((x, y) => VerticalSort(x, y));

                            _verticalPosition.Clear();
                            for (int i = 0; i < _verticalEvents.Count; i++)
                            {
                                HistoryEvent h = _verticalEvents[i];
                                _verticalPosition[h] = i;
                            }

                            CalculateHorizontalEvent();

                            //CheckForHorizontalChanges(e);
                        }
                        else
                        {
                            //if (_verticalTies.Contains(e))
                            //{
                            //    // Check e's neighbours.
                            //}
                            // Even if vertical position hasn't changed, the horizontal position might have!
                            // if two siblings have the same MaxDescendentTicks, but sibling 1 was places vertically higher,
                            // and to the right of sibling 2, then sibling 1 updates so it should be placed to the left, it
                            // wont in this case since the vertical position didn't change. or does that mean that two siblings that are 
                            // tied should be placed to the left if it's also placed higher vertically?
                            return false;
                        }

                        //_fullRefresh = verticalPositionChanged;
                    }
                    break;
            }

            return true;

            //return _fullRefresh;
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

        private int HorizontalSort(HistoryEvent x, HistoryEvent y)
        {
            UInt64 yMax = y.GetMaxDescendentTicks();
            UInt64 xMax = x.GetMaxDescendentTicks();
            int result = yMax.CompareTo(xMax);

            if (result != 0)
            {
                return result;
            }

            return y.Id.CompareTo(x.Id);
        }

        private int VerticalSort(HistoryEvent x, HistoryEvent y)
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
                if (ReferenceEquals(x, y))
                {
                    return 0;
                }
                else if (x.IsEqualToOrAncestorOf(y))
                {
                    return -1;
                }
                else if (y.IsEqualToOrAncestorOf(x))
                {
                    return 1;
                }
            }

            _verticalTies.Add(new Tuple<HistoryEvent, HistoryEvent>(x, y));

            return x.Id.CompareTo(y.Id);
        }

        public HistoryEvent GetTile(int horizontalIndex, int verticalIndex)
        {
            HistoryEvent h = _horizontalEvents[horizontalIndex];
            HistoryEvent parent = null;
            if (!_interestingParentEvents.TryGetValue(h, out parent))
            {
                parent = null;
            }

            int parentVertical = (parent == null) ? -1 : _verticalPosition[parent];

            if (parentVertical < verticalIndex && verticalIndex <= _verticalPosition[h])
            {
                return h;
            }

            return null;
        }

        private void Init(History history)
        {
            _interestingParentEvents = new Dictionary<HistoryEvent, HistoryEvent>();
            _horizontalEvents.Clear();
            //_horizontalEvents = new List<HistoryEvent>();

            _verticalEvents.Clear();
            //_verticalEvents = new List<HistoryEvent>();
            List<HistoryEvent> children = new List<HistoryEvent>();

            //_horizontalPosition = new Dictionary<HistoryEvent, int>();
            _verticalPosition = new Dictionary<HistoryEvent, int>();

            if (history == null)
            {
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();
            Dictionary<HistoryEvent, UInt64> maxDescendentTicks = new Dictionary<HistoryEvent, UInt64>();

            Stack<HistoryEvent> stack = new Stack<HistoryEvent>();

            stack.Push(history.RootEvent);
            while (stack.Any())
            {
                HistoryEvent top = stack.Peek();

                bool allChildrenDone = true;
                UInt64 max = top.Ticks;
                foreach (HistoryEvent child in top.Children)
                {
                    if (maxDescendentTicks.TryGetValue(child, out UInt64 ticks))
                    {
                        if (ticks > max)
                        {
                            max = ticks;
                        }
                    }
                    else
                    {
                        stack.Push(child);
                        allChildrenDone = false;
                        break;
                    }
                }

                if (allChildrenDone)
                {
                    maxDescendentTicks.Add(top, max);
                    stack.Pop();
                }
            }

            int CachedHorizontalSort(HistoryEvent x, HistoryEvent y)
            {
                int result = maxDescendentTicks[y].CompareTo(maxDescendentTicks[x]);
                if (result != 0)
                {
                    return result;
                }

                return y.Id.CompareTo(x.Id);
            }

            sw.Stop();
            CPvC.Diagnostics.Trace("Caching max descendent ticks took {0}ms", sw.ElapsedMilliseconds);



            List<HistoryEvent> historyEvents = new List<HistoryEvent>();
            historyEvents.Add(history.RootEvent);

            sw.Reset();
            //Stopwatch sw = Stopwatch.StartNew();

            //int i = 0;

            while (historyEvents.Any())
            {
                HistoryEvent historyEvent = historyEvents[0];
                historyEvents.RemoveAt(0);

                children.Clear();
                children.AddRange(historyEvent.Children);
                children.Sort((x, y) => CachedHorizontalSort(x, y));

                if (InterestingEvent(historyEvent))
                {
                    _verticalEvents.Add(historyEvent);
                    _horizontalEvents.Add(historyEvent);
                }
                else
                {
                    //horizontalEvents.RemoveAt(i);
                    //i--;
                }
                //else

                historyEvents.InsertRange(0, children);

                //i++;
            }

            sw.Stop();

            CPvC.Diagnostics.Trace("Traversing tree took {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            // Figure out parents.
            foreach (HistoryEvent historyEvent in _verticalEvents)
            {
                HistoryEvent parentEvent = historyEvent.Parent;
                while (parentEvent != null && !_verticalEvents.Contains(parentEvent))
                {
                    parentEvent = parentEvent.Parent;
                }

                _interestingParentEvents.Add(historyEvent, parentEvent);
            }

            sw.Stop();

            CPvC.Diagnostics.Trace("Calculating parents took {0}ms", sw.ElapsedMilliseconds);

            sw.Reset();
            sw.Start();

            _verticalEvents.Sort((x, y) => VerticalSort(x, y));

            sw.Stop();

            CPvC.Diagnostics.Trace("Sorting vertically took {0}ms", sw.ElapsedMilliseconds);

            PositionChanged?.Invoke(this, new NotifyPositionChangedEventArgs(NotifyListChangedAction.Cleared, -1, -1, -1, -1, null, null, null, null));

            for (int v = 0; v < _verticalEvents.Count; v++)
            {
                HistoryEvent he = _verticalEvents[v];
                _verticalPosition.Add(he, v);

                NotifyPositionChangedEventArgs args = new NotifyPositionChangedEventArgs(NotifyListChangedAction.Added, -1, _horizontalEvents.FindIndex(he), -1, VerticalPosition(he), null, he, null, ParentEvent(he));

                PositionChanged?.Invoke(this, args);
            }

            //for (int h = 0; h < _horizontalEvents.Count; h++)
            //{
            //    _horizontalPosition.Add(_horizontalEvents[h], h);
            //}
        }

        private History _history;
        private Dictionary<HistoryEvent, HistoryEvent> _interestingParentEvents;
        private ObservableList<HistoryEvent> _horizontalEvents;
        private ObservableList<HistoryEvent> _verticalEvents;
        //private Dictionary<HistoryEvent, int> _horizontalPosition;
        //private ConditionalWeakTable<HistoryEvent, List>
        private Dictionary<HistoryEvent, int> _verticalPosition;

        //private bool _fullRefresh;
        private List<HistoryViewItem> _items;
        private HashSet<Tuple<HistoryEvent, HistoryEvent>> _verticalTies;

        public event NotifyPositionChangedEventHandler PositionChanged;
    }
}