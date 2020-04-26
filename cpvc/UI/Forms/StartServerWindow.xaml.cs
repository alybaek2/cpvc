using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
