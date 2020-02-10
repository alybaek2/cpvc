using System;
using System.Windows;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for BookmarkSelectWindow.xaml
    /// </summary>
    public sealed partial class BookmarkSelectWindow : Window, IDisposable
    {
        public HistoryEvent SelectedEvent { get; private set; }
        public HistoryEvent SelectedReplayEvent { get; private set; }

        private readonly Machine _machine;
        private BookmarksViewModel _viewModel;

        public BookmarkSelectWindow(Window owner, Machine machine)
        {
            InitializeComponent();

            _machine = machine;

            SelectedEvent = null;
            _viewModel = new BookmarksViewModel(_machine);

            Owner = owner;

            DataContext = _viewModel;
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void JumpToBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_historyListView.SelectedItems.Count != 1)
            {
                return;
            }

            HistoryViewItem item = (HistoryViewItem)_historyListView.SelectedItem;
            if (item?.HistoryEvent != null)
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

        private void DeleteBranchButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = _historyListView.SelectedItems;
            if (items == null)
            {
                return;
            }

            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.TrimTimeline(item.HistoryEvent);
                }
            }

            _viewModel.RefreshHistoryViewItems();
        }

        private void DeleteBookmarkButton_Click(object sender, RoutedEventArgs e)
        {
            System.Collections.IList items = _historyListView.SelectedItems;
            if (items == null)
            {
                return;
            }

            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.SetBookmark(item.HistoryEvent, null);
                }
            }

            _viewModel.RefreshHistoryViewItems();
        }

        private void _replayButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            HistoryViewItem item = (HistoryViewItem)_historyListView.SelectedItem;
            if (item?.HistoryEvent != null)
            {
                SelectedReplayEvent = item.HistoryEvent;
            }
        }
    }
}
