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
    public class Command : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;

        public Command(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }

        public void InvokeCanExecuteChanged(object sender, PropertyChangedEventArgs e)
        {
            //if (CanExecuteChanged == null)
            //{
            //    return;
            //}

            //foreach (var del in CanExecuteChanged?.GetInvocationList())
            //{
            //    del.
            //    del.DynamicInvoke(new object[] { sender, e });
            //}

            if (Application.Current == null)
            {
                CanExecuteChanged?.Invoke(sender, e);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => CanExecuteChanged?.Invoke(sender, e)));
            }

            //CanExecuteChanged?.BeginInvoke(sender, e, null, null);
        }
    }
}
