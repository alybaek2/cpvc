using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
