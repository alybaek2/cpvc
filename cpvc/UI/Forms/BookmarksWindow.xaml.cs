using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        private WriteableBitmap _bitmap;
        private Int32Rect _drawRect = new Int32Rect(0, 0, Display.Width, Display.Height);

        public HistoryEvent SelectedJumpEvent { get; private set; }
        public HistoryEvent SelectedReplayEvent { get; private set; }

        public Command ReplayCommand { get; }
        public Command JumpCommand { get; }

        public BookmarkSelectWindow(Window owner, LocalMachine machine)
        {
            InitializeComponent();

            _machine = machine;

            Action<Action> canExecuteChangedInvoker = (action) =>
            {
                Dispatcher.BeginInvoke(action, null);
            };

            _viewModel = new BookmarksViewModel(_machine.History, canExecuteChangedInvoker);
            _bitmap = new WriteableBitmap(768, 288, 0, 0, PixelFormats.Indexed8, Display.Palette);

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
                p => _viewModel.SelectedItems.Count == 1,
                canExecuteChangedInvoker
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
                p => _viewModel.SelectedItems.Count == 1 && _viewModel.SelectedItems[0].HistoryEvent is BookmarkHistoryEvent,
                canExecuteChangedInvoker
            );

            DataContext = _viewModel;
        }

        private void HistoryListView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null)
            {
                foreach (HistoryViewItem addedItem in e.AddedItems)
                {
                    _viewModel.AddSelectedItem(addedItem);
                }

                JumpCommand.InvokeCanExecuteChanged(this, new EventArgs());
                ReplayCommand.InvokeCanExecuteChanged(this, new EventArgs());
            }

            if (e.RemovedItems != null)
            {
                foreach (HistoryViewItem removedItem in e.RemovedItems)
                {
                    _viewModel.RemoveSelectedItem(removedItem);
                }

                JumpCommand.InvokeCanExecuteChanged(this, new EventArgs());
                ReplayCommand.InvokeCanExecuteChanged(this, new EventArgs());
            }

            if (_historyListView.SelectedItems.Count == 1)
            {
                // Set bitmap to currently focussed list item, if it happens to be a bookmark:
                HistoryViewItem selectedItem = _historyListView.SelectedItems[0] as HistoryViewItem;
                {
                    HistoryEvent historyEvent = selectedItem.HistoryEvent;

                    // Even though the current event doesn't necessarily have a bookmark, we can still populate the display.
                    if (historyEvent == _machine.History.CurrentEvent)
                    {
                        MainWindow.CopyScreen(_machine, _bitmap);
                    }
                    else if (historyEvent is BookmarkHistoryEvent bookmarkHistoryEvent)
                    {
                        MainWindow.CopyScreen(bookmarkHistoryEvent.Bookmark.Screen.GetBytes(), _bitmap);
                    }
                }

                _fullScreenImage.Visibility = Visibility.Visible;
            }
            else
            {
                _fullScreenImage.Visibility = Visibility.Hidden;
            }
        }

        private void FullScreenImage_Loaded(object sender, RoutedEventArgs e)
        {
            Image image = (Image)sender;

            image.Source = _bitmap;
        }
    }
}
