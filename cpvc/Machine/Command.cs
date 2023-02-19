using System;
using System.Windows.Input;
using System.Windows.Threading;

namespace CPvC
{
    public class Command : ICommand
    {
        private readonly Predicate<object> _canExecute;
        private readonly Action<object> _execute;
        private Action<Action> _canExecuteChangedInvoker;

        public Command(Action<object> execute, Predicate<object> canExecute, Action<Action> canExecuteChangedInvoker2)
        {
            _execute = execute;
            _canExecute = canExecute;
            _canExecuteChangedInvoker = canExecuteChangedInvoker2;
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

        public void InvokeCanExecuteChanged(object sender, EventArgs e)
        {
            if (_canExecuteChangedInvoker != null)
            {
                _canExecuteChangedInvoker(new Action(() => CanExecuteChanged?.Invoke(sender, e)));
            }
        }
    }
}
