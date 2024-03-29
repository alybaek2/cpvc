﻿using System.Windows;

namespace CPvC
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Dispatcher.UnhandledException += OnUnhandledException;
        }

        private void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            string message = string.Format("An error occurred:\n\n{0}", e.Exception.Message);

            MessageBox.Show(message, "CPvC", MessageBoxButton.OK, MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}
