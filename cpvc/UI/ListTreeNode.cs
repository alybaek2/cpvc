using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ListTreeNode<T> : INotifyPropertyChanged
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
                while (node.Children.Any())
                {
                    node = node.Children.Last();
                }

                return node;
            }
        }

        public T Data
        {
            get
            {
                return _data;
            }

            set
            {
                if (value.Equals(_data))
                {
                    return;
                }

                _data = value;
                OnPropertyChanged();
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private T _data;
    }
}
