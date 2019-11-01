using System.Collections.Generic;
using System.Windows;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for SelectFileWindow.xaml
    /// </summary>
    public partial class SelectItemWindow : Window
    {
        public SelectItemWindow()
        {
            InitializeComponent();
        }

        public void SetListItems(List<string> filenames)
        {
            _itemsListBox.Items.Clear();

            foreach (string filename in filenames)
            {
                _itemsListBox.Items.Add(filename);
            }
        }

        public string GetSelectedItem()
        {
            // Add code to return null if cancel is clicked!
            return (string)_itemsListBox.SelectedValue;
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Close();
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}
