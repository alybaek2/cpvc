using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CPvC
{
    public class ViewModelCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;
        private readonly object _watchObject;
        private readonly string _watchPropertyName;

        public ViewModelCommand(Action<object> execute, Predicate<object> canExecute, INotifyPropertyChanged watchObject, string watchProperty)
        {
            _execute = execute;
            _canExecute = canExecute;
            _watchObject = watchObject;
            _watchPropertyName = watchProperty;

            if (watchObject != null)
            {
                watchObject.PropertyChanged += WatchObjectPropertyChanged;
            }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public event EventHandler CanExecuteChanged;

        public void WatchObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender == _watchObject && e.PropertyName == _watchPropertyName)
            {
                CanExecuteChanged?.Invoke(sender, e);
            }
        }
    }
}
