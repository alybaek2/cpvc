using System.Windows;
using System.Windows.Media.Imaging;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for BookmarkSelectWindow.xaml
    /// </summary>
    public sealed partial class BookmarkSelectWindow : Window
    {
        private readonly LocalMachine _machine;
        private BookmarksViewModel _viewModel;
        private Display _display;

        public HistoryEvent SelectedJumpEvent { get; private set; }
        public HistoryEvent SelectedReplayEvent { get; private set; }

        public Command ReplayCommand { get; }
        public Command JumpCommand { get; }

        public BookmarkSelectWindow(Window owner, LocalMachine machine)
        {
            InitializeComponent();

            _machine = machine;

            _viewModel = new BookmarksViewModel(_machine);
            _display = new Display();

            Owner = owner;

            ReplayCommand = new Command(
                p =>
                {
                    if (_viewModel.SelectedItems.Count == 1)
                    {
                        SelectedReplayEvent = _viewModel.SelectedItems[0].HistoryEvent;

                        DialogResult = true;
                    }
                },
                p => _viewModel.SelectedItems.Count == 1
            );

            JumpCommand = new Command(
                p =>
                {
                    if (_viewModel.SelectedItems.Count == 1)
                    {
                        SelectedJumpEvent = _viewModel.SelectedItems[0].HistoryEvent;

                        DialogResult = true;
                    }
                },
                p => _viewModel.SelectedItems.Count == 1 && _viewModel.SelectedItems[0].HistoryEvent is BookmarkHistoryEvent
            );

            DataContext = _viewModel;
        }

        private void HistoryListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null)
            {
                foreach(HistoryViewItem addedItem in e.AddedItems)
                {
                    _viewModel.AddSelectedItem(addedItem);
                }
            }

            if (e.RemovedItems != null)
            {
                foreach (HistoryViewItem removedItem in e.RemovedItems)
                {
                    _viewModel.RemoveSelectedItem(removedItem);
                }
            }

            if (_historyListView.SelectedItems.Count == 1)
            {
                // Set bitmap to currently focussed list item, if it happens to be a bookmark:
                HistoryViewItem selectedItem = _historyListView.SelectedItems[0] as HistoryViewItem;
                {
                    WriteableBitmap bitmap = null;
                    HistoryEvent historyEvent = selectedItem.HistoryEvent;

                    // Even though the current event doesn't necessarily have a bookmark, we can still populate the display.
                    if (historyEvent == _machine.History.CurrentEvent)
                    {
                        bitmap = _machine.Display.Bitmap;
                    }
                    else if (historyEvent is BookmarkHistoryEvent bookmarkHistoryEvent)
                    {
                        _display.GetFromBookmark(bookmarkHistoryEvent.Bookmark);

                        bitmap = _display.Bitmap;
                    }

                    _fullScreenImage.Source = bitmap;
                }

                _fullScreenImage.Visibility = Visibility.Visible;
            }
            else
            {
                _fullScreenImage.Visibility = Visibility.Hidden;
            }
        }
    }
}
