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

        public HistoryEvent SelectedEvent { get; private set; }

        private readonly Machine _machine;

        public BookmarkSelectWindow(Window owner, Machine machine)
        {
            InitializeComponent();

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

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            HistoryViewItem item = (HistoryViewItem)_historyListView.SelectedItem;
            if (item != null && item.HistoryEvent != null)
            {
                SelectedEvent = item.HistoryEvent;
            }

            DialogResult = true;

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }

        private void HistoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool loaded = HistoryView.SelectBookmark(_historyListView, _display, _machine, _fullScreenImage);

            _fullScreenImage.Visibility = loaded ? Visibility.Visible : Visibility.Hidden;
            _noBookmarkSelectedLabel.Visibility = loaded ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
