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
    /// Interaction logic for RenameWindow.xaml
    /// </summary>
    public partial class RenameWindow : Window
    {
        public string NewName;

        public RenameWindow(Window owner, string currentName)
        {
            InitializeComponent();

            _nameTextBox.Text = currentName;
            Owner = owner;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            NewName = _nameTextBox.Text;

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
