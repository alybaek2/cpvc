using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace CPvC
{
    public class ViewModelCommand : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;
        private readonly INotifyPropertyChanged _watchObject;
        private readonly string _watchPropertyName;
        private INotifyPropertyChanged _watchObject2;
        private string _watchPropertyName2;

        public ViewModelCommand(Action<object> execute, Predicate<object> canExecute, INotifyPropertyChanged watchObject, string watchProperty, string watchProperty2)
        {
            _execute = execute;
            _canExecute = canExecute;
            _watchObject = watchObject;
            _watchPropertyName = watchProperty;
            _watchPropertyName2 = watchProperty2;

            if (watchObject != null)
            {
                watchObject.PropertyChanged += WatchObjectPropertyChanged;

                if (_watchPropertyName2 != null)
                {
                    _watchObject2 = watchObject.GetType().GetProperty(watchProperty).GetValue(watchObject) as INotifyPropertyChanged;

                    if (_watchObject2 != null)
                    {
                        _watchObject2.PropertyChanged += WatchObjectPropertyChanged;
                    }
                }
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

        // The property changed handler must execute in the main thread to avoid having a "The calling 
        // thread cannot access this object because a different thread owns it" exception thrown. The
        // down side is it makes CanExecuteChanged difficult to unit test.
        public void WatchObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Application.Current == null)
            {
                WatchObjectPropertyChangedAsync(sender, e);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    WatchObjectPropertyChangedAsync(sender, e);
                }));
            }
        }

        private void WatchObjectPropertyChangedAsync(object sender, PropertyChangedEventArgs e)
        {
            if (sender == _watchObject && e.PropertyName == _watchPropertyName)
            {
                if (_watchObject2 != null)
                {
                    _watchObject2.PropertyChanged -= WatchObjectPropertyChanged;
                }

                _watchObject2 = _watchObject.GetType().GetProperty(_watchPropertyName).GetValue(_watchObject) as INotifyPropertyChanged;

                if (_watchObject2 != null)
                {
                    _watchObject2.PropertyChanged += WatchObjectPropertyChanged;
                }

                CanExecuteChanged?.Invoke(sender, e);
            }

            if (sender == _watchObject2 && e.PropertyName == _watchPropertyName2)
            {
                CanExecuteChanged?.Invoke(sender, e);
            }
        }
    }
}
