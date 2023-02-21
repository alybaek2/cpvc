using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ViewModelObservableCollection<D, V> : ObservableCollection<V>
    {
        public ViewModelObservableCollection(ReadOnlyObservableCollection<D> data, ViewModelFactory<D, V> factory)
        {
            _data = data;
            _factory = factory;
            ((INotifyCollectionChanged)_data).CollectionChanged += ViewModelObservableCollection_CollectionChanged;

            Refresh();
        }

        public V Get(D data)
        {
            if (_data.Contains(data))
            {
                return _factory.Create(data);
            }

            return default(V);
        }

        private void ViewModelObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Defeinitely can optimize this!
            Refresh();
        }

        private void Refresh()
        {
            int dindex = 0;
            int vmindex = 0;

            while (dindex < _data.Count || vmindex < Count)
            {
                if (dindex >= _data.Count)
                {
                    RemoveAt(vmindex);
                    continue;
                }

                V viewModelExpected = _factory.Create(_data[dindex]);
                if (vmindex >= Count)
                {
                    Add(viewModelExpected);
                }
                else if (!ReferenceEquals(viewModelExpected, this[vmindex]))
                {
                    Insert(vmindex, viewModelExpected);
                }

                dindex++;
                vmindex++;
            }
        }

        private ReadOnlyObservableCollection<D> _data;
        private ViewModelFactory<D, V> _factory;
    }
}
