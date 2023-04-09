using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public enum NotifyListChangedAction
    {
        Added,
        Moved,
        Replaced,
        Removed,
        Cleared
    }

    public class PositionChangedEventArgs<T> : EventArgs
    {
        public PositionChangedEventArgs(List<ListTreeNode<T>> horizontalOrdering, List<ListTreeNode<T>> verticalOrdering, Dictionary<T, T> interestingParents)
        {
            // Make copies of the orderings... don't use the "live" copies of them!
            HorizontalOrdering = horizontalOrdering.ToList();
            VerticalOrdering = verticalOrdering.ToList();
            InterestingParents = new Dictionary<T, T>(interestingParents);
        }

        public List<ListTreeNode<T>> HorizontalOrdering { get; }
        public List<ListTreeNode<T>> VerticalOrdering { get; }
        public Dictionary<T, T> InterestingParents { get; }
    }

    public delegate void NotifyPositionChangedEventHandler<T>(object sender, PositionChangedEventArgs<T> e);
}
