using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ListTreeNode<T>
    {
        public ListTreeNode(T data)
        {
            Data = data;
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

        public ListTreeNode<T> RightmostDescendent
        {
            get
            {
                ListTreeNode<T> node = this;
                while (node.Children.Count > 0)
                {
                    node = node.Children.Last();
                }

                return node;
            }
        }

        public T Data
        {
            get;
            set;
        }
    }
}
