using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public abstract class ListTree<T>
    {
        public ListTree()
        {
            _verticalNodes = new List<ListTreeNode<T>>();
            _horizontalNodes = new List<ListTreeNode<T>>();
            _verticalPositions = new Dictionary<ListTreeNode<T>, int>();
            _horizontalPositions = new Dictionary<ListTreeNode<T>, int>();
            _eventsToNodes = new Dictionary<T, ListTreeNode<T>>();
            _verticalTies = new HashSet<Tuple<T, T>>(); // new Dictionary<T, T>();
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
            node.Data = newHistoryEvent;
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
                ListTreeNode<T> node = parent.Children[childIndex - 1].RightmostDescendent;
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

            int leftmostHorizontalIndex = _horizontalPositions[node];
            int rightmostHorizontalIndex = _horizontalPositions[node.RightmostDescendent];

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
            _eventsToNodes.Remove(child.Data);

            int leftmostHorizontalIndex = _horizontalPositions[child];
            int rightmostHorizontalIndex = _horizontalPositions[child.RightmostDescendent];

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
                if (VerticalSort(_verticalNodes[verticalIndex].Data, node.Data) > 0)
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
                if (HorizontalSort(child.Data, parent.Children[childIndex].Data) < 0)
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

        private bool AdjustHorizontalOrderIfNeeded(ListTreeNode<T> node)
        {
            if (node.Parent == null)
            {
                return false;
            }

            int childIndex = ChildIndex(node);
            if (!HorizontalPositionChanged(node, childIndex))
            {
                return false;
            }

            // Horizontal has changed!
            node.Parent.Children.RemoveAt(childIndex);

            int newChildIndex = 0;
            while (newChildIndex < node.Parent.Children.Count)
            {
                if (HorizontalSort(node.Data, node.Parent.Children[newChildIndex].Data) < 0)
                {
                    break;
                }

                newChildIndex++;
            }

            node.Parent.Children.Insert(newChildIndex, node);

            // Fix up the horizontal ordering!

            // Figure out the right-most descendent...
            int leftmostHorizontalIndex = _horizontalPositions[node];
            int rightmostHorizontalIndex = _horizontalPositions[node.RightmostDescendent];

            // Where is the new leftmost horizontal index?
            int previousHorizontalIndex = _horizontalPositions[node.Parent];
            if (newChildIndex > 0)
            {
                ListTreeNode<T> rightmostDescendentNode = node.Parent.Children[newChildIndex - 1].RightmostDescendent;
                previousHorizontalIndex = _horizontalPositions[rightmostDescendentNode];
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
                if (HorizontalSort(node.Parent.Children[childIndex - 1].Data, node.Data) >= 0)
                {
                    return true;
                }
            }

            if (!horizontalPositionChanged && (childIndex + 1 < node.Parent.Children.Count))
            {
                if (HorizontalSort(node.Data, node.Parent.Children[childIndex + 1].Data) >= 0)
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
                bool previouslyTied = _verticalTies.Any() && _verticalTies.Contains(new Tuple<T, T>(node.Data, _verticalNodes[verticalIndex - 1].Data));
                if (VerticalSort(_verticalNodes[verticalIndex - 1].Data, node.Data) >= 0)
                {
                    return true;
                }

                if (previouslyTied)
                {
                    return true;
                }
            }

            if (verticalIndex + 1 < _verticalNodes.Count)
            {
                bool previouslyTied = _verticalTies.Any() && _verticalTies.Contains(new Tuple<T, T>(node.Data, _verticalNodes[verticalIndex + 1].Data));
                if (VerticalSort(node.Data, _verticalNodes[verticalIndex + 1].Data) >= 0)
                {
                    return true;
                }

                if (previouslyTied)
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
                if (VerticalSort(node.Data, _verticalNodes[newVerticalIndex].Data) < 0)
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

        protected List<ListTreeNode<T>> _verticalNodes;
        protected List<ListTreeNode<T>> _horizontalNodes;

        // Lookup helpers
        protected Dictionary<ListTreeNode<T>, int> _verticalPositions;
        protected Dictionary<ListTreeNode<T>, int> _horizontalPositions;
        protected Dictionary<T, ListTreeNode<T>> _eventsToNodes;

        protected HashSet<Tuple<T, T>> _verticalTies;
    }
}
