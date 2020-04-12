using System.Windows;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for ConnectWindow.xaml
    /// </summary>
    public partial class ConnectWindow : Window
    {
        public string ServerNameAndPort;

        public ConnectWindow(Window owner)
        {
            InitializeComponent();

            Owner = owner;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            ServerNameAndPort = _serverNameAndPortTextBox.Text;

            DialogResult = true;

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}
