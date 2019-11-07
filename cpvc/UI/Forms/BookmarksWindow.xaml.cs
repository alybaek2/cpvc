using System;
using System.Windows;
using System.Windows.Controls;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for BookmarkSelectWindow.xaml
    /// </summary>
    public sealed partial class BookmarkSelectWindow : Window, IDisposable
    {
        private Display _display;
        private BookmarkSelectWindowLogic _logic;

        public HistoryEvent SelectedEvent { get; private set; }

        private readonly Machine _machine;

        public BookmarkSelectWindow(Window owner, Machine machine)
        {
            InitializeComponent();

            _logic = new BookmarkSelectWindowLogic(machine);
            _machine = machine;
            _display = new Display();
            _fullScreenImage.Source = _display.Bitmap;

            SelectedEvent = null;

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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HistoryView.PopulateListView(_historyListView, _machine.RootEvent, _machine.CurrentEvent);

            _historyListView.SelectedIndex = 0;
        }

        private void JumpToBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_historyListView.SelectedItems.Count != 1)
            {
                return;
            }

            HistoryViewItem item = (HistoryViewItem)_historyListView.SelectedItem;
            if (item != null && item.HistoryEvent != null)
            {
                SelectedEvent = item.HistoryEvent;
            }

            DialogResult = true;

            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void HistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Enable the buttons according to what is selected. It's possible to use data binding for this, but
            // this approach is considerably more simple.
            int bookmarksSelected = 0;
            foreach (HistoryViewItem historyItem in _historyListView.SelectedItems)
            {
                if (historyItem.HistoryEvent != null && historyItem.HistoryEvent.Bookmark != null)
                {
                    bookmarksSelected++;
                }
            }

            _deleteBookmarkButton.IsEnabled = (bookmarksSelected > 0);
            _jumpToBookmarkButton.IsEnabled = (bookmarksSelected == 1);
            _deleteBranchButton.IsEnabled = (_historyListView.SelectedItems.Count > 0);

            bool loaded = HistoryView.SelectBookmark(_historyListView, _display, _machine, _fullScreenImage);

            _fullScreenImage.Visibility = loaded ? Visibility.Visible : Visibility.Hidden;
            _noBookmarkSelectedLabel.Visibility = loaded ? Visibility.Hidden : Visibility.Visible;
        }

        private void DeleteBranchButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = _historyListView.SelectedItems;
            if (items == null)
            {
                return;
            }

            _logic.DeleteBranches(items);

            HistoryView.PopulateListView(_historyListView, _machine.RootEvent, _machine.CurrentEvent);
        }

        private void DeleteBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = _historyListView.SelectedItems;
            if (items == null)
            {
                return;
            }

            _logic.DeleteBookmarks(items);

            HistoryView.PopulateListView(_historyListView, _machine.RootEvent, _machine.CurrentEvent);
        }
    }
}
