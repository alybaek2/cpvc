using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public Command(Action<object> execute, Predicate<object> canExecute, INotifyPropertyChanged o, List<string> propertyNames)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        // This is a bit of a heavy-handed approach, but gets around a problem where
        // CanExecute seemed to be called with null instead of the CommandParameter the
        // command was bound to, possibly because CanExecute was being called prior to
        // that binding taking place...
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute(parameter);
        }
    }
}
