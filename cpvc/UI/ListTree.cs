using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ListTreeEventArgs<T> : EventArgs
    {
        public ListTreeEventArgs()
        {
        }

        public ListTreeNode<T> VerticalEvent { get; set; }
        public int OldVerticalIndex { get; set; }
        public int NewVerticalIndex { get; set; }

        public int NewHorizontalIndex { get; set; }

        //public List<ListTreeNode<T>> HorizontalEvents { get; set; }
        public List<int> HorizontalIndices { get; set; }

        public List<int> VerticalIndicesDeleted { get; set; }
        public int HorizontalDeleteStart { get; set; }
        public int HorizontalDeleteCount { get; set; }
        public List<ListTreeNode<T>> HorizontalOrdering { get; set; }
    }

    public delegate void ListTreeEventHandler<T>(object sender, ListTreeEventArgs<T> e);

    public class ListTreeNode<T>
    {
        public ListTreeNode(T obj, ListTreeNode<T> parent)
        {
            Obj = obj;
            Parent = parent;
            Children = new List<ListTreeNode<T>>();
        }

        public T Obj { get; set; }
        public ListTreeNode<T> Parent { get; set; }
        public List<ListTreeNode<T>> Children { get; set; }

        private T _object;
    }

    public delegate int Compare<T>(T x, T y);

    public class ListTree<T>
    {
        public ListTree(ListTreeNode<T> root, Compare<T> verticalComparitor, Compare<T> horizontalComparitor)
        {
            Root = root;

            _nodes = new HashSet<ListTreeNode<T>>();

            _verticalOrdering = new List<ListTreeNode<T>>();
            _horizontalOrdering = new List<ListTreeNode<T>>();

            //_verticalOrdering2 = new ObservableCollection<ListTreeNode<T>>();
            //_horizontalOrdering2 = new ObservableCollection<ListTreeNode<T>>();

            //VerticalOrdering = new ReadOnlyObservableCollection<ListTreeNode<T>>(_verticalOrdering2);
            //HorizontalOrdering = new ReadOnlyObservableCollection<ListTreeNode<T>>(_horizontalOrdering2);
            _verticalComparitor = verticalComparitor;
            _horizontalComparitor = horizontalComparitor;

            _lockObject = new object();
            Initialize();
        }

        public ListTreeNode<T> Root { get; }

        public event ListTreeEventHandler<T> SomethingChanged;
        private object _lockObject;

        private HashSet<ListTreeNode<T>> _nodes;

        private List<ListTreeNode<T>> _verticalOrdering;
        private List<ListTreeNode<T>> _horizontalOrdering;

        private Compare<T> _verticalComparitor;
        private Compare<T> _horizontalComparitor;

        //private ObservableCollection<ListTreeNode<T>> _verticalOrdering2;
        //private ObservableCollection<ListTreeNode<T>> _horizontalOrdering2;

        //public ReadOnlyObservableCollection<ListTreeNode<T>> VerticalOrdering { get; }
        //public ReadOnlyObservableCollection<ListTreeNode<T>> HorizontalOrdering { get; }

        private void Initialize()
        {
            lock (_lockObject)
            {
                List<ListTreeNode<T>> children = new List<ListTreeNode<T>>();

                Stack<ListTreeNode<T>> events = new Stack<ListTreeNode<T>>();

                _nodes.Clear();

                _verticalOrdering.Clear();
                _horizontalOrdering.Clear();
                //_verticalOrdering2.Clear();
                //_horizontalOrdering2.Clear();

                events.Push(Root);
                while (events.Any())
                {
                    ListTreeNode<T> e = events.Pop();

                    _horizontalOrdering.Add(e);
                    _verticalOrdering.Add(e);
                    //_horizontalOrdering2.Add(e);
                    //_verticalOrdering2.Add(e);

                    children.Clear();
                    children.AddRange(e.Children);
                    children.Sort((x, y) => _horizontalComparitor(x.Obj, y.Obj));

                    foreach (ListTreeNode<T> child in children)
                    {
                        events.Push(child);
                    }
                }

                _verticalOrdering.Sort((x, y) => _verticalComparitor(x.Obj, y.Obj));
            }
        }

        public void AddNode(ListTreeNode<T> parent, ListTreeNode<T> node)
        {
            if (node.Children.Any())
            {
                throw new Exception("can't add nodes with children!");
            }

            // Find vertical index
            int newVerticalIndex = -1;
            for (int i = 0; i < _verticalOrdering.Count; i++)
            {
                if (_verticalComparitor(node.Obj, _verticalOrdering[i].Obj) < 0)
                {
                    newVerticalIndex = i;
                    break;
                }
            }

            if (newVerticalIndex == -1)
            {
                newVerticalIndex = _verticalOrdering.Count;
                _verticalOrdering.Add(node);
            }
            else
            {
                _verticalOrdering.Insert(newVerticalIndex, node);
            }


            // Find horizontal index
            int insertAfter = -1;
            for (int i = 0; i < parent.Children.Count; i++)
            {
                if (_horizontalComparitor(node.Obj, parent.Children[i].Obj) < 0)
                {
                    insertAfter = i - 1;
                    break;
                }
            }

            int insertAt = -1;
            if (insertAfter == -1)
            {
                insertAt = _horizontalOrdering.FindIndex(x => ReferenceEquals(x, parent));
                insertAt++;
            }
            else
            {
                ListTreeNode<T> descendent = parent.Children[insertAfter];
                while (descendent.Children.Any())
                {
                    descendent = descendent.Children[descendent.Children.Count - 1];
                }

                insertAt = _horizontalOrdering.FindIndex(x => ReferenceEquals(x, descendent)) + 1;
            }

            _horizontalOrdering.Insert(insertAt, node);
            parent.Children.Insert(insertAfter + 1, node);
            node.Parent = parent;


            // Raise an event!
            ListTreeEventArgs<T> args = new ListTreeEventArgs<T>();
            args.OldVerticalIndex = -1;
            args.NewVerticalIndex = newVerticalIndex;
            args.VerticalEvent = node;
            args.NewHorizontalIndex = insertAt;

            // Cheat for now. Not optimal! Should figure out the set of nodes whose horizontal ordering has changed.
            //args.HorizontalIndices = Enumerable.Range(0, _verticalOrdering.Count).ToList();


            SomethingChanged?.Invoke(this, args);

        }

        public void UpdateNode(ListTreeNode<T> node)
        {
            bool verticalOrderingChanged = false;

            // Check if the vertical ordering has changed...
            int nodeVerticalIndex = GetVerticalIndex(node);
            if (nodeVerticalIndex > 0)
            {
                // Compare to the previous...
                if (_verticalComparitor(node.Obj, _verticalOrdering[0].Obj) < 0)
                {
                    verticalOrderingChanged = true;
                }
            }

            if (!verticalOrderingChanged && nodeVerticalIndex < (_verticalOrdering.Count - 1))
            {
                // Compare to the next...
                if (_verticalComparitor(_verticalOrdering[nodeVerticalIndex + 1].Obj, node.Obj) < 0)
                {
                    verticalOrderingChanged = true;
                }
            }

            if (verticalOrderingChanged)
            {
                // Figure out the new verticalIndex...
                _verticalOrdering.RemoveAt(nodeVerticalIndex);

                int newVerticalIndex = -1;
                for (int i = 0; i < _verticalOrdering.Count; i++)
                {
                    if (_verticalComparitor(node.Obj, _verticalOrdering[i].Obj) < 0)
                    {
                        newVerticalIndex = i;
                        break;
                    }
                }

                if (newVerticalIndex == -1)
                {
                    newVerticalIndex = _verticalOrdering.Count;
                    _verticalOrdering.Add(node);
                }
                else
                {
                    _verticalOrdering.Insert(newVerticalIndex, node);
                }

                // Figure out which nodes have had their horizontal ordering changed...

                // Raise an event!
                ListTreeEventArgs<T> args = new ListTreeEventArgs<T>();
                args.OldVerticalIndex = nodeVerticalIndex;
                args.NewVerticalIndex = newVerticalIndex;
                args.VerticalEvent = node;

                // Cheat for now. Not optimal! Should figure out the set of nodes whose horizontal ordering has changed.
                args.HorizontalIndices = Enumerable.Range(0, _verticalOrdering.Count).ToList();


                SomethingChanged?.Invoke(this, args);
            }
        }

        public void DeleteNode(ListTreeNode<T> node, bool recursive)
        {
            if (!recursive)
            {
                List<ListTreeNode<T>> children = node.Children;
                node.Children = null;

                ListTreeNode<T> parent = node.Parent;

                parent.Children.Remove(node);
                parent.Children.AddRange(children);

                int nodeVerticalIndex = GetVerticalIndex(node);

                _verticalOrdering.Remove(node);

                // Re-evaluate horizontal ordering
                Stack<ListTreeNode<T>> events = new Stack<ListTreeNode<T>>();

                List<ListTreeNode<T>> horizontalOrdering = new List<ListTreeNode<T>>();

                events.Push(Root);
                while (events.Any())
                {
                    ListTreeNode<T> e = events.Pop();

                    horizontalOrdering.Add(e);

                    children.Clear();
                    children.AddRange(e.Children);
                    children.Sort((x, y) => _horizontalComparitor(x.Obj, y.Obj));

                    foreach (ListTreeNode<T> child in children)
                    {
                        events.Push(child);
                    }
                }

                // Raise an event!
                ListTreeEventArgs<T> args = new ListTreeEventArgs<T>();
                args.OldVerticalIndex = nodeVerticalIndex;
                args.NewVerticalIndex = -1;
                args.VerticalEvent = node;
                args.HorizontalOrdering = new List<ListTreeNode<T>>(horizontalOrdering);

                // Cheat for now. Not optimal! Should figure out the set of nodes whose horizontal ordering has changed.
                args.HorizontalIndices = Enumerable.Range(0, _verticalOrdering.Count).ToList();

                _horizontalOrdering = horizontalOrdering;

                SomethingChanged?.Invoke(this, args);
            }
            else
            {
                // Simpler case... just remove the node and all subnodes. Sorting doesn't need to be reevaluated.
                // Though horizontal positions can change.
                ListTreeNode<T> firstDescendent = node;
                while (firstDescendent.Children.Any())
                {
                    firstDescendent = firstDescendent.Children[0];
                }

                ListTreeNode<T> lastDescendent = node;
                while (lastDescendent.Children.Any())
                {
                    lastDescendent = lastDescendent.Children[lastDescendent.Children.Count - 1];
                }

                int firstHorizontalIndex = _horizontalOrdering.FindIndex(x => ReferenceEquals(x, firstDescendent));
                int lastHorizontalIndex = _horizontalOrdering.FindIndex(x => ReferenceEquals(x, lastDescendent));

                List<int> verticalIndices = new List<int>();

                Queue<ListTreeNode<T>> queue = new Queue<ListTreeNode<T>>();
                queue.Enqueue(node);

                while (queue.Any())
                {
                    ListTreeNode<T> c = queue.Dequeue();

                    verticalIndices.Add(_verticalOrdering.FindIndex(x => ReferenceEquals(x, c)));
                    foreach (ListTreeNode<T> child in c.Children)
                    {
                        queue.Enqueue(child);
                    }
                }

                verticalIndices.Sort((x, y) => y.CompareTo(x));

                foreach (int i in verticalIndices)
                {
                    _verticalOrdering.RemoveAt(i);
                }

                // Raise an event!
                ListTreeEventArgs<T> args = new ListTreeEventArgs<T>();
                args.HorizontalDeleteStart = firstHorizontalIndex;
                args.HorizontalDeleteCount = lastHorizontalIndex - firstHorizontalIndex + 1;
                args.VerticalIndicesDeleted = verticalIndices;
                args.OldVerticalIndex = -1;
                args.NewVerticalIndex = -1;
                args.VerticalEvent = null;

                // Cheat for now. Not optimal! Should figure out the set of nodes whose horizontal ordering has changed.
                args.HorizontalIndices = null; // Enumerable.Range(0, _verticalOrdering.Count).ToList();

                _horizontalOrdering.RemoveRange(firstHorizontalIndex, lastHorizontalIndex - firstHorizontalIndex + 1);

                args.HorizontalOrdering = new List<ListTreeNode<T>>(_horizontalOrdering);

                //_horizontalOrdering = horizontalOrdering;

                SomethingChanged?.Invoke(this, args);

            }
        }

        public int GetVerticalIndex(ListTreeNode<T> node)
        {
            return _verticalOrdering.FindIndex(x => ReferenceEquals(x, node));
        }

        public int GetHorizontalIndex(ListTreeNode<T> node)
        {
            return _horizontalOrdering.FindIndex(x => ReferenceEquals(x, node));
        }
    }
}
