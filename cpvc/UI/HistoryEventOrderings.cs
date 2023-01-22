using System;
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
    public class ListTreeNode<T>
    {
        public ListTreeNode(T historyEvent)
        {
            HistoryEvent = historyEvent;
            Children = new List<ListTreeNode<T>>();
        }

        public ListTreeNode<T> Parent
        {
            get;
            set;
        }

        public List<ListTreeNode<T>> Children
        {
            get;
        }

        public T HistoryEvent
        {
            get;
            set;
        }
    }

    public abstract class ListTree<T>
    {
        public ListTree()
        {
            _verticalNodes = new List<ListTreeNode<T>>();
            _horizontalNodes = new List<ListTreeNode<T>>();
            _verticalPositions = new Dictionary<ListTreeNode<T>, int>();
            _horizontalPositions = new Dictionary<ListTreeNode<T>, int>();
            _eventsToNodes = new Dictionary<T, ListTreeNode<T>>();
        }

        public ListTreeNode<T> Root
        {
            get;
            protected set;
        }

        protected void InitRoot(T rootData)
        {
            Root = new ListTreeNode<T>(rootData);
            _eventsToNodes.Add(rootData, Root);
            _verticalNodes.Add(Root);
            _horizontalNodes.Add(Root);
            _verticalPositions.Add(Root, 0);
            _horizontalPositions.Add(Root, 0);
        }

        public System.Drawing.Point GetPosition(ListTreeNode<T> node)
        {
            return new System.Drawing.Point(_horizontalPositions[node], _verticalPositions[node]);
        }

        public ListTreeNode<T> GetNode(T historyEvent)
        {
            if (_eventsToNodes.TryGetValue(historyEvent, out ListTreeNode<T> node))
            {
                return node;
            }

            return null;
        }

        public bool Update(T oldHistoryEvent, T newHistoryEvent)
        {
            ListTreeNode<T> node = GetNode(oldHistoryEvent);

            _eventsToNodes.Remove(oldHistoryEvent);
            node.HistoryEvent = newHistoryEvent;
            bool changed = Update(node);
            _eventsToNodes.Add(newHistoryEvent, node);

            return changed;
        }

        public List<ListTreeNode<T>> HorizontalOrdering()
        {
            return _horizontalNodes;
        }

        public List<ListTreeNode<T>> VerticalOrdering()
        {
            return _verticalNodes;
        }

        protected int GetHorizontalInsertionIndex(ListTreeNode<T> parent, int childIndex)
        {
            // Insert into horizontal events!
            int previousHorizontalIndex = _horizontalPositions[parent];
            if (childIndex > 0)
            {
                // Find the "right"-most descendent of the previous child!
                ListTreeNode<T> node = RightmostDescendent(parent.Children[childIndex - 1]);
                previousHorizontalIndex = _horizontalPositions[node];
            }

            return previousHorizontalIndex + 1;
        }

        private int ChildPosition(ListTreeNode<T> parent, ListTreeNode<T> child)
        {
            return parent.Children.FindIndex(x => ReferenceEquals(x, child));
        }

        public ListTreeNode<T> InsertNewParent(ListTreeNode<T> node, T historyEvent)
        {
            if (_eventsToNodes.ContainsKey(historyEvent))
            {
                throw new Exception("History event already exists in the list tree!");
            }

            ListTreeNode<T> newParentNode = new ListTreeNode<T>(historyEvent);
            ListTreeNode<T> oldParentNode = node.Parent;

            // Remove the node from the old parent.
            int oldParentChildIndex = ChildPosition(oldParentNode, node);
            oldParentNode.Children.RemoveAt(oldParentChildIndex);
            node.Parent = null;

            int childIndex = GetChildIndex(oldParentNode, newParentNode);
            int newHorizontalIndex = GetHorizontalInsertionIndex(oldParentNode, childIndex);
            ListTreeNode<T> rightmostDescendentNode = RightmostDescendent(node);

            int leftmostHorizontalIndex = _horizontalPositions[node];
            int rightmostHorizontalIndex = _horizontalPositions[rightmostDescendentNode];

            // Shift the horizontal indexes over...
            MoveHorizontal(leftmostHorizontalIndex, newHorizontalIndex, rightmostHorizontalIndex - leftmostHorizontalIndex + 1);
            _horizontalNodes.Insert(newHorizontalIndex, newParentNode);
            oldParentNode.Children.Insert(childIndex, newParentNode);
            newParentNode.Parent = oldParentNode;

            // Add the node as a child of the new parent
            newParentNode.Children.Add(node);
            node.Parent = newParentNode;

            _eventsToNodes.Add(historyEvent, newParentNode);
            int verticalIndex = GetVerticalIndex(newParentNode);
            _verticalNodes.Insert(verticalIndex, newParentNode);

            RefreshHorizontalPositions(newHorizontalIndex);
            RefreshVerticalPositions(verticalIndex);

            return newParentNode;
        }

        private void MoveHorizontal(int oldIndex, int newIndex, int count)
        {
            if (oldIndex == newIndex)
            {
                return;
            }

            int refreshIndex;
            if (oldIndex < newIndex)
            {
                refreshIndex = oldIndex;
                
                while (count > 0)
                {
                    ListTreeNode<T> temp = _horizontalNodes[oldIndex];
                    _horizontalNodes.Insert(newIndex, temp);
                    _horizontalNodes.RemoveAt(oldIndex);

                    count--;
                }
            }
            else
            {
                refreshIndex = newIndex;

                while (count > 0)
                {
                    ListTreeNode<T> temp = _horizontalNodes[oldIndex];
                    _horizontalNodes.RemoveAt(oldIndex);
                    _horizontalNodes.Insert(newIndex, temp);
                    oldIndex++;
                    newIndex++;

                    count--;
                }
            }

            RefreshHorizontalPositions(refreshIndex);

        }

        public void RemoveRecursive(ListTreeNode<T> child)
        {
            ListTreeNode<T> parent = child.Parent;

            // Find the child!
            int childIndex = ChildIndex(child);
            parent.Children.RemoveAt(childIndex);
            child.Parent = null;
            _eventsToNodes.Remove(child.HistoryEvent);


            // Figure out the right-most descendent...
            ListTreeNode<T> node = RightmostDescendent(child);

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

            RefreshVerticalPositions(childVertical);

            // Remove these from the horizontal orderings...
            _horizontalNodes.RemoveRange(leftmostHorizontalIndex, rightmostHorizontalIndex - leftmostHorizontalIndex + 1);

            RefreshHorizontalPositions(leftmostHorizontalIndex);
        }

        public void RemoveNonRecursive(ListTreeNode<T> node)
        {
            if (node.Parent == null)
            {
                throw new ArgumentException("Can't remove the root node!", nameof(node));
            }

            // Todo!

            // Remove the node, and reinsert its children to node's parent.
        }

        public bool Update(ListTreeNode<T> node)
        {
            // The sort order may have changed!! Check it!
            bool verticalChanged = AdjustVerticalOrderIfNeeded(node);
            bool horizontalChanged = AdjustHorizontalOrderIfNeeded(node);

            // If the vertical position changed, this could possibly affact the max descendent
            // ticks of all our ancestor nodes. Since that could affect the horizontal position
            // of those nodes, check them!
            if (verticalChanged)
            {
                ListTreeNode<T> parent = node.Parent;
                while (parent != null)
                {
                    AdjustHorizontalOrderIfNeeded(parent);
                    parent = parent.Parent;
                }
            }

            return verticalChanged || horizontalChanged;
        }

        protected void RefreshHorizontalPositions(int index)
        {
            for (int i = index; i < _horizontalNodes.Count; i++)
            {
                _horizontalPositions[_horizontalNodes[i]] = i;
            }
        }

        protected void RefreshVerticalPositions(int index)
        {
            for (int i = index; i < _verticalNodes.Count; i++)
            {
                _verticalPositions[_verticalNodes[i]] = i;
            }
        }

        protected int GetVerticalIndex(ListTreeNode<T> node)
        {
            // This could be more efficient!
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

        protected int GetChildIndex(ListTreeNode<T> parent, ListTreeNode<T> child)
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

        private int ChildIndex(ListTreeNode<T> node)
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

        private ListTreeNode<T> RightmostDescendent(ListTreeNode<T> node)
        {
            while (node.Children.Count > 0)
            {
                node = node.Children[node.Children.Count - 1];
            }

            return node;
        }

        private bool AdjustHorizontalOrderIfNeeded(ListTreeNode<T> node)
        {
            if (node.Parent == null)
            {
                return false;
            }

            int childIndex = ChildIndex(node);

            bool horizontalPositionChanged = HorizontalPositionChanged(node, childIndex);
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
            ListTreeNode<T> node2 = RightmostDescendent(node);
            int leftmostHorizontalIndex = _horizontalPositions[node];
            int rightmostHorizontalIndex = _horizontalPositions[node2];

            // Where is the new leftmost horizontal index?
            int previousHorizontalIndex = _horizontalPositions[node.Parent];
            if (newChildIndex > 0)
            {
                ListTreeNode<T> r = RightmostDescendent(node.Parent.Children[newChildIndex - 1]);
                previousHorizontalIndex = _horizontalPositions[r];
            }

            // Move them!
            MoveHorizontal(leftmostHorizontalIndex, previousHorizontalIndex + 1, rightmostHorizontalIndex - leftmostHorizontalIndex + 1);

            return true;
        }

        private bool HorizontalPositionChanged(ListTreeNode<T> node, int childIndex)
        {
            bool horizontalPositionChanged = false;
            if (childIndex > 0)
            {
                if (HorizontalSort(node.Parent.Children[childIndex - 1].HistoryEvent, node.HistoryEvent) >= 0)
                {
                    return true;
                }
            }

            if (!horizontalPositionChanged && (childIndex + 1 < node.Parent.Children.Count))
            {
                if (HorizontalSort(node.HistoryEvent, node.Parent.Children[childIndex + 1].HistoryEvent) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool VerticalPositionChanged(ListTreeNode<T> node, int verticalIndex)
        {
            if (verticalIndex > 0)
            {
                if (VerticalSort(_verticalNodes[verticalIndex - 1].HistoryEvent, node.HistoryEvent) >= 0)
                {
                    return true;
                }
            }

            if (verticalIndex + 1 < _verticalNodes.Count)
            {
                if (VerticalSort(node.HistoryEvent, _verticalNodes[verticalIndex + 1].HistoryEvent) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private bool AdjustVerticalOrderIfNeeded(ListTreeNode<T> node)
        {
            int verticalIndex = _verticalPositions[node];
            bool verticalPositionChanged = VerticalPositionChanged(node, verticalIndex);
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

            return true;
        }

        protected abstract int HorizontalSort(T x, T y);

        protected abstract int VerticalSort(T x, T y);

        //public void DebugDump()
        //{
        //    CPvC.Diagnostics.Trace("Horizontal/Vertical tree:\n");

        //    for (int v = 0; v < _verticalNodes.Count; v++)
        //    {
        //        int h = _horizontalPositions[_verticalNodes[v]];

        //        string str = new string(' ', h * 2);

        //        str += String.Format("{0}", _verticalNodes[v].HistoryEvent.Id);

        //        CPvC.Diagnostics.Trace(str);
        //    }

        //    CPvC.Diagnostics.Trace("Tree hierarchy:\n");

        //    List<ListTreeNode> nodes = new List<ListTreeNode>();
        //    nodes.Add(Root);
        //    while (nodes.Any())
        //    {
        //        ListTreeNode node = nodes[0];
        //        nodes.RemoveAt(0);

        //        int indent = 0;
        //        ListTreeNode p = node.Parent;
        //        while (p != null)
        //        {
        //            p = p.Parent;
        //            indent++;
        //        }

        //        string str = new string(' ', indent * 2);
        //        str += String.Format("{0}", node.HistoryEvent.Id);

        //        CPvC.Diagnostics.Trace(str);

        //        indent++;

        //        nodes.InsertRange(0, node.Children);
        //    }
        //}

        protected List<ListTreeNode<T>> _verticalNodes;
        protected List<ListTreeNode<T>> _horizontalNodes;

        // Lookup helpers
        protected Dictionary<ListTreeNode<T>, int> _verticalPositions;
        protected Dictionary<ListTreeNode<T>, int> _horizontalPositions;
        protected Dictionary<T, ListTreeNode<T>> _eventsToNodes;
    }

    public class HistoryListTree : ListTree<HistoryEvent>
    {
        public HistoryListTree(History history)
        {
            SetHistory(history);
        }

        //public ListTreeNode<HistoryEvent> Root
        //{
        //    get
        //    {
        //        return Root;
        //    }

        //    protected set
        //    {
        //        Root = value;
        //    }
        //}

        //public System.Drawing.Point GetPosition(ListTreeNode<HistoryEvent> node)
        //{
        //    return GetPosition(node);
        //}

        public void SetHistory(History history)
        {
            if (_history != null)
            {
                _history.Auditors -= ProcessHistoryChange;
            }

            _history = history;

            if (_history != null)
            {
                Init();
                _history.Auditors += ProcessHistoryChange;
            }
        }

        private void Init()
        {
            //_listTree = new ListTree<HistoryEvent>(_history.RootEvent);

            InitRoot(_history.RootEvent);

            List<HistoryEvent> nodes = new List<HistoryEvent>();
            nodes.AddRange(_history.RootEvent.Children);

            while (nodes.Any())
            {
                HistoryEvent historyEvent = nodes[0];
                nodes.RemoveAt(0);

                AddEventToListTree(historyEvent);

                nodes.AddRange(historyEvent.Children);
            }
        }

        public void ProcessHistoryChange(object sender, HistoryChangedEventArgs args)
        {

            NotifyPositionChangedEventArgs<HistoryEvent> changeArgs = UpdateListTree(args);
            if (changeArgs != null)
            {
                PositionChanged?.Invoke(this, changeArgs);
            }
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

        private ListTreeNode<HistoryEvent> Add(ListTreeNode<HistoryEvent> parent, HistoryEvent historyEvent)
        {
            ListTreeNode<HistoryEvent> child = new ListTreeNode<HistoryEvent>(historyEvent);

            // Check first if child already exists in the tree! And that parent exists in the tree!

            // Insert into children of parent first!
            // But! We must either add a new child, or inject this child inbetween the parent and an existing child!
            // Which case are we?

            // This class should not be doing this! HistoryControl2 should be... ListTree should be made to be generic at some point!
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
                ListTreeNode<HistoryEvent> descendentChild = parent.Children[descendentChildIndex];
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
            int newHorizontalIndex = GetHorizontalInsertionIndex(parent, childIndex);

            _horizontalNodes.Insert(newHorizontalIndex, child);

            RefreshHorizontalPositions(newHorizontalIndex);

            // Insert vertically!
            int verticalIndex = GetVerticalIndex(child);
            _verticalNodes.Insert(verticalIndex, child);

            RefreshVerticalPositions(verticalIndex);


            // Raise some kind of event here!


            return child;
        }



        private bool AddEventToListTree(HistoryEvent historyEvent)
        {
            HistoryEvent parentHistoryEvent = historyEvent.Parent;
            ListTreeNode<HistoryEvent> parentNode = GetNode(parentHistoryEvent);
            bool wasParentInteresting = parentNode != null;
            bool isParentInteresting = InterestingEvent(parentHistoryEvent);
            ListTreeNode<HistoryEvent> node = GetNode(historyEvent);

            if (node != null)
            {
                // The node is already in the tree... we shouldn't be trying to add it!
                throw new Exception("Node was already in the tree.");
            }

            if (!InterestingEvent(historyEvent))
            {
                return false;
            }

            bool add = true;

            // First, check if the parent's interestingness has changed from false to true.
            if (!wasParentInteresting && isParentInteresting)
            {
                // Need to add the parent!

                // But first, find the child who will share this new parent!
                ListTreeNode<HistoryEvent> cousinNode = null;
                HistoryEvent he = parentHistoryEvent;
                while (true)
                {
                    he = he.Children[0];
                    cousinNode = GetNode(he);
                    if (cousinNode != null)
                    {
                        break;
                    }
                }

                parentNode = InsertNewParent(cousinNode, parentHistoryEvent);
            }
            else if (!wasParentInteresting && !isParentInteresting)
            {
                // Work our way up the tree to find who should be our parent!
                HistoryEvent he = parentHistoryEvent;
                while (true)
                {
                    ListTreeNode<HistoryEvent> n = GetNode(he);
                    if (n != null)
                    {
                        parentNode = n;
                        break;
                    }

                    he = he.Parent;
                }
            }
            else if (wasParentInteresting && !isParentInteresting)
            {
                // Replace the parent node with the child!
                Update(parentHistoryEvent, historyEvent);
                add = false;

                // Todo: Notify!
            }
            else if (wasParentInteresting && isParentInteresting)
            {
                // Nothing to do! Just add the new node!
            }

            if (add && InterestingEvent(historyEvent))
            {
                Add(parentNode, historyEvent);

                // Todo: Notify!
            }

            return true;
        }

        private NotifyPositionChangedEventArgs<HistoryEvent> UpdateListTree(HistoryChangedEventArgs args)
        {
            //if (_listTree == null)
            //{
            //    return null;
            //}

            bool changed = false;

            //lock (_listTree)
            {
                switch (args.Action)
                {
                    case HistoryChangedAction.Add:
                        {
                            changed = AddEventToListTree(args.HistoryEvent);
                        }
                        break;
                    case HistoryChangedAction.UpdateCurrent:
                        {
                            ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                            changed = Update(node);
                        }
                        break;
                    case HistoryChangedAction.DeleteBranch:
                        {
                            ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                            RemoveRecursive(node);

                            changed = true;
                        }
                        break;
                    case HistoryChangedAction.DeleteBookmark:
                        {
                            ListTreeNode<HistoryEvent> node = GetNode(args.HistoryEvent);

                            RemoveNonRecursive(node);

                            changed = true;
                        }
                        break;
                }
            }

            if (changed)
            {
                NotifyPositionChangedEventArgs<HistoryEvent> changeArgs = new NotifyPositionChangedEventArgs<HistoryEvent>(HorizontalOrdering(), VerticalOrdering());

                return changeArgs;
            }

            return null;
        }

        protected override int HorizontalSort(HistoryEvent x, HistoryEvent y)
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

        protected override int VerticalSort(HistoryEvent x, HistoryEvent y)
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

            return x.Id.CompareTo(y.Id);
        }


        //public List<ListTreeNode<HistoryEvent>> HorizontalOrdering
        //{
        //    get
        //    {
        //        return HorizontalOrdering();
        //    }
        //}

        //public List<ListTreeNode<HistoryEvent>> VerticalOrdering
        //{
        //    get
        //    {
        //        return VerticalOrdering();
        //    }
        //}

        public event NotifyPositionChangedEventHandler<HistoryEvent> PositionChanged;

        private History _history;
        //private ListTree<HistoryEvent> _listTree;
    }

    public enum NotifyListChangedAction
    {
        Added,
        Moved,
        Replaced,
        Removed,
        Cleared
    }

    public class NotifyPositionChangedEventArgs<T> : EventArgs
    {
        public NotifyPositionChangedEventArgs(List<ListTreeNode<T>> horizontalOrdering, List<ListTreeNode<T>> verticalOrdering)
        {
            HorizontalOrdering = horizontalOrdering;
            VerticalOrdering = verticalOrdering;
        }

        public List<ListTreeNode<T>> HorizontalOrdering { get; }
        public List<ListTreeNode<T>> VerticalOrdering { get; }
    }

    public delegate void NotifyPositionChangedEventHandler<T>(object sender, NotifyPositionChangedEventArgs<T> e);



}
