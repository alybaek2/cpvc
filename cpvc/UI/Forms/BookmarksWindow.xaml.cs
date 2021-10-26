using System;
using System.Windows;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for BookmarkSelectWindow.xaml
    /// </summary>
    public sealed partial class BookmarkSelectWindow : Window
    {
        private readonly LocalMachine _machine;
        private BookmarksViewModel _viewModel;

        public HistoryEvent SelectedJumpEvent
        {
            get
            {
                return _viewModel.SelectedJumpEvent;
            }
        }

        public HistoryEvent SelectedReplayEvent
        {
            get
            {
                return _viewModel.SelectedReplayEvent;
            }
        }

        public BookmarkSelectWindow(Window owner, LocalMachine machine)
        {
            InitializeComponent();

            _machine = machine;

            _viewModel = new BookmarksViewModel(_machine, ItemSelected);

            Owner = owner;

            DataContext = _viewModel;
        }

        public void ItemSelected()
        {
            DialogResult = true;
        }
    }
}
