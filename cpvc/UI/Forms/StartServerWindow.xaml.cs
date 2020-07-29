using System;
using System.Windows;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for StartServerWindow.xaml
    /// </summary>
    public partial class StartServerWindow : Window
    {
        public UInt16 Port { get; private set; }

        public StartServerWindow(Window owner, UInt16 defaultPort)
        {
            InitializeComponent();

            Owner = owner;
            _serverPortTextBox.Text = defaultPort.ToString();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (UInt16.TryParse(_serverPortTextBox.Text, out UInt16 port))
            {
                Port = port;
                DialogResult = true;
            }
            else
            {
                DialogResult = false;
            }

            Close();
        }
    }
}
