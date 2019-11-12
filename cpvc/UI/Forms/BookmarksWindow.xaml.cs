using System;
using System.Collections.Generic;
using System.Linq;
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
            RefreshHistoryView();

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

            bool loaded = SelectBookmark();

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

            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.TrimTimeline(item.HistoryEvent);
                }
            }

            RefreshHistoryView();
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

            RefreshHistoryView();
        }

        /// <summary>
        /// Populates the ListView control with HistoryViewItems based on the machine's history.
        /// </summary>
        private void RefreshHistoryView()
        {
            // Generate items...
            List<HistoryViewItem> items = GetHistoryViewItems();

            // Render items...
            HistoryViewItem next = null;
            _historyListView.Items.Clear();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                HistoryViewItem item = items[i];
                item.Draw(next, _machine.CurrentEvent);
                _historyListView.Items.Add(item);

                next = item;
            }
        }

        /// <summary>
        /// Generates a list of HistoryViewItem objects that are used to populate the history view.
        /// </summary>
        /// <returns>A list of HistoryEventItems</returns>
        private List<HistoryViewItem> GetHistoryViewItems()
        {
            // Avoid calling this function recursively since the depth of the history could be large...
            List<Tuple<int, HistoryEvent>> eventStack = new List<Tuple<int, HistoryEvent>>
            {
                new Tuple<int, HistoryEvent>(0, _machine.RootEvent)
            };

            // Note that items is sorted in ascending order of ticks (i.e. oldest to most recent).
            List<HistoryViewItem> items = new List<HistoryViewItem>();

            while (eventStack.Count > 0)
            {
                int left = eventStack[0].Item1;
                HistoryEvent historyEvent = eventStack[0].Item2;
                eventStack.RemoveAt(0);

                if (ShouldShow(historyEvent))
                {
                    HistoryViewItem item = new HistoryViewItem(historyEvent);

                    // Figure out where this new item should be placed.
                    int itemIndex = items.FindIndex(x => x.Ticks > historyEvent.Ticks);
                    if (itemIndex == -1)
                    {
                        // Not found? Add the item to the end.
                        itemIndex = items.Count;
                    }

                    // Add passthrough events to all items inbetween the item and its parent.
                    HistoryEvent parent = MostRecentShownAncestor(historyEvent);
                    if (parent != null)
                    {
                        for (int i = itemIndex - 1; i >= 0 && items[i].HistoryEvent != parent; i--)
                        {
                            left = items[i].AddEvent(left, historyEvent);
                        }
                    }

                    // Copy the Events from the next item so passthroughs are correctly rendered.
                    if (itemIndex < items.Count)
                    {
                        item.Events = new List<HistoryEvent>(items[itemIndex].Events);
                    }

                    // Now add the actual event itself.
                    left = item.AddEvent(left, historyEvent);

                    items.Insert(itemIndex, item);
                }

                List<HistoryEvent> sortedChildren = new List<HistoryEvent>(historyEvent.Children);
                sortedChildren.Sort((x, y) => GetMaxDescendentTicks(y).CompareTo(GetMaxDescendentTicks(x)));

                for (int c = 0; c < sortedChildren.Count; c++)
                {
                    // Place the children at the top of the stack; effectively means we're doing a depth-first walk of the tree.
                    eventStack.Insert(c, new Tuple<int, HistoryEvent>(left, sortedChildren[c]));
                }
            }

            return items;
        }
        
        /// <summary>
        /// Returns the maximum ticks value of any given HistoryEvent's descendents. Used when sorting children in <c>AddEventToItem</c>.
        /// </summary>
        /// <param name="historyEvent">The HistoryEvent object.</param>
        /// <returns></returns>
        static private UInt64 GetMaxDescendentTicks(HistoryEvent historyEvent)
        {
            if (historyEvent.Children.Count == 0)
            {
                return historyEvent.Ticks;
            }

            return historyEvent.Children.Select(x => GetMaxDescendentTicks(x)).Max();
        }

        /// <summary>
        /// Returns a boolean indicating whether the given event should be shown in the history view.
        /// </summary>
        /// <param name="historyEvent">The HistoryEvent object.</param>
        /// <returns>Boolean indicating whether <c>historyEvent</c> should be shown in the history view.</returns>
        static private bool ShouldShow(HistoryEvent historyEvent)
        {
            if (historyEvent.Children.Count != 1)
            {
                return true;
            }

            if (historyEvent.Type == HistoryEvent.Types.Checkpoint && historyEvent.Bookmark != null)
            {
                return true;
            }

            if (historyEvent.Parent == null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the most recent ancestor of <c>historyEvent</c> which is shown in the history view.
        /// </summary>
        /// <param name="historyEvent">The HistoryEvent object.</param>
        /// <returns>The most recent ancestor of <c>historyEvent</c> which is shown in the history view.</returns>
        static private HistoryEvent MostRecentShownAncestor(HistoryEvent historyEvent)
        {
            HistoryEvent parent = historyEvent.Parent;
            while (parent != null && !ShouldShow(parent))
            {
                parent = parent.Parent;
            }

            return parent;
        }

        /// <summary>
        /// Populates the given Display with the currently selected HistoryViewItem if it has a bookmark.
        /// </summary>
        /// <returns>A boolean indicating if the Display object was populated (true if the selected HistoryViewItem has a bookmark; false otherwise).</returns>
        private bool SelectBookmark()
        {
            HistoryViewItem viewItem = (HistoryViewItem)_historyListView.SelectedItem;
            if (viewItem != null)
            {
                HistoryEvent historyEvent = viewItem.HistoryEvent;

                // Even though the current event doesn't necessarily have a bookmark, we can still populate the display.
                if (historyEvent == _machine.CurrentEvent)
                {
                    _fullScreenImage.Source = _machine.Display.Bitmap;
                    return true;
                }

                if (historyEvent != null && historyEvent.Type == HistoryEvent.Types.Checkpoint && historyEvent.Bookmark != null)
                {
                    _display.GetFromBookmark(historyEvent.Bookmark);
                    _fullScreenImage.Source = _display.Bitmap;

                    return true;
                }
            }

            return false;
        }
    }
}
