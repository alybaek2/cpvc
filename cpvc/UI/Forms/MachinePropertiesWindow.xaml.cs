using System;
using System.Windows;
using System.Windows.Controls;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for MachinePropertiesWindow.xaml
    /// </summary>
    public sealed partial class MachinePropertiesWindow : Window, IDisposable
    {
        private Machine _machine;
        private Display _display;
        private MachinePropertiesWindowLogic _logic;

        public MachinePropertiesWindow(Window owner, Machine machine)
        {
            InitializeComponent();

            _machine = machine;
            _display = new Display();
            _logic = new MachinePropertiesWindowLogic(machine);

            Owner = owner;
        }

        public void Dispose()
        {
            if (_display != null)
            {
                _display.Dispose();
                _display = null;
            }
        }

        private void PopulateHistoryListView()
        {
            HistoryView.PopulateListView(_historyListView, _machine.RootEvent, _machine.CurrentEvent);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _nameTextBox.Text = _machine.Name;
            PopulateHistoryListView();

            _fullScreenImage.Source = _display.Bitmap;
            _historyListView.SelectedIndex = 0;
        }

        private void _cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void _timelineListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool visible = HistoryView.SelectBookmark(_historyListView, _display);

            _fullScreenImage.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            _noBookmarkSelectedLabel.Visibility = visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void _deleteBookmarkContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = _historyListView.SelectedItems;
            if (items == null)
            {
                return;
            }

            _logic.DeleteBookmarks(items);

            PopulateHistoryListView();
        }

        private void _deleteBranchContextMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = _historyListView.SelectedItems;
            if (items == null)
            {
                return;
            }

            _logic.DeleteTimelines(items);

            PopulateHistoryListView();
        }

        private void _okButton_Click(object sender, RoutedEventArgs e)
        {
            _machine.Name = _nameTextBox.Text;

            Close();
        }

        private void _compactFileButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.RewriteMachineFile();
        }
    }
}
