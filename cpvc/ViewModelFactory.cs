using System;
using System.Collections.Generic;

namespace CPvC
{
    public class ViewModelFactory<D, V>
    {
        public ViewModelFactory(Func<D, V> createViewModel)
        {
            _createViewModel = createViewModel;
            _viewModels = new Dictionary<D, V>();
        }

        public V Create(D data)
        {
            if (!Get(data, out V viewModel))
            {
                viewModel = _createViewModel(data);
                _viewModels.Add(data, viewModel);
            }

            return viewModel;
        }

        public bool Get(D data, out V viewModel)
        {
            return _viewModels.TryGetValue(data, out viewModel);
        }

        private Func<D, V> _createViewModel;

        // Could this be a WeakConditionalTable?
        private Dictionary<D, V> _viewModels;
    }
}
