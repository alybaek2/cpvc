using System.Windows;

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
