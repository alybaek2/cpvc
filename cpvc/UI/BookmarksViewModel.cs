using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CPvC.UI
{
    /// <summary>
    /// View model for the Bookmarks window.
    /// </summary>
    public class BookmarksViewModel : INotifyPropertyChanged
    {
        private LocalMachine _machine;

        private Command _deleteBookmarksCommand;
        private Command _deleteBranchesCommand;

        private ObservableCollection<HistoryViewItem> _selectedItems;
        private ReadOnlyObservableCollection<HistoryViewItem> _readOnlySelectedItems;

        public ICommand DeleteBookmarksCommand
        {
            get { return _deleteBookmarksCommand; }
        }

        public ICommand DeleteBranchesCommand
        {
            get { return _deleteBranchesCommand; }
        }

        public HistoryEvent SelectedJumpEvent { get; private set; }
        public HistoryEvent SelectedReplayEvent { get; private set; }

        public WriteableBitmap Bitmap { get; private set; }
        public ObservableCollection<HistoryViewItem> Items { get; }

        public ReadOnlyObservableCollection<HistoryViewItem> SelectedItems
        {
            get
            {
                return _readOnlySelectedItems;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public BookmarksViewModel(LocalMachine machine)
        {
            _selectedItems = new ObservableCollection<HistoryViewItem>();
            _readOnlySelectedItems = new ReadOnlyObservableCollection<HistoryViewItem>(_selectedItems);

            _deleteBookmarksCommand = new Command(
                p => DeleteBookmarks(),
                p => SelectedItems.Any(item => item.HistoryEvent is BookmarkHistoryEvent)
            );

            _deleteBranchesCommand = new Command(
                p => DeleteBranches(),
                p => SelectedItems.Any(item => !item.HistoryEvent.IsEqualToOrAncestorOf(machine.History.CurrentEvent))
            );

            _machine = machine;
            Items = new ObservableCollection<HistoryViewItem>();
            RefreshHistoryViewItems();
        }

        public void AddSelectedItem(HistoryViewItem addedItem)
        {
            _selectedItems.Add(addedItem);
        }

        public void RemoveSelectedItem(HistoryViewItem removedItem)
        {
            _selectedItems.Remove(removedItem);
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

            if (historyEvent is BookmarkHistoryEvent)
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
        /// Generates a list of HistoryViewItem objects that are used to populate the history view.
        /// </summary>
        public void RefreshHistoryViewItems()
        {
            // Avoid calling this function recursively since the depth of the history could be large...
            List<Tuple<int, HistoryEvent>> eventStack = new List<Tuple<int, HistoryEvent>>
            {
                new Tuple<int, HistoryEvent>(0, _machine.History.RootEvent)
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
                    int itemIndex = items.FindIndex(x => x.Ticks > historyEvent.EndTicks);
                    if (itemIndex == -1)
                    {
                        // Not found? Add the item to the end.
                        itemIndex = items.Count;
                    }

                    // Add passthrough events to all items inbetween the item and its parent.
                    HistoryEvent parent = MostRecentShownAncestor(historyEvent);
                    if (parent != null)
                    {
                        // As the parent should have been added by now, we don't need to check for FindIndex returning -1.
                        int parentIndex = items.FindIndex(x => x.HistoryEvent == parent);
                        for (int i = parentIndex + 1; i < itemIndex; i++)
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
                sortedChildren.Sort((x, y) => y.GetMaxDescendentTicks().CompareTo(x.GetMaxDescendentTicks()));

                for (int c = 0; c < sortedChildren.Count; c++)
                {
                    // Place the children at the top of the stack; effectively means we're doing a depth-first walk of the tree.
                    eventStack.Insert(c, new Tuple<int, HistoryEvent>(left, sortedChildren[c]));
                }
            }

            // Draw items to their respective canvasses.
            HistoryViewItem next = null;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                HistoryViewItem item = items[i];
                item.Draw(next, _machine.History.CurrentEvent);

                next = item;
            }

            // Show the items in descending order of Ticks (i.e. most to least recent).
            items.Reverse();

            Items.Clear();
            foreach (HistoryViewItem item in items)
            {
                Items.Add(item);
            }
        }

        private void DeleteBookmarks()
        {
            bool refresh = false;

            foreach (HistoryViewItem selectedItem in SelectedItems)
            {
                if (selectedItem.HistoryEvent is BookmarkHistoryEvent bookmarkHistoryEvent)
                {
                    refresh |= _machine.DeleteBookmark(bookmarkHistoryEvent);
                }
            }

            if (refresh)
            {
                RefreshHistoryViewItems();
            }
        }

        private void DeleteBranches()
        {
            bool refresh = false;

            foreach (HistoryViewItem selectedItem in SelectedItems)
            {
                if (!selectedItem.HistoryEvent.IsEqualToOrAncestorOf(_machine.History.CurrentEvent))
                {
                    refresh |= TrimTimeline(selectedItem.HistoryEvent);
                }
            }

            if (refresh)
            {
                RefreshHistoryViewItems();
            }
        }

        /// <summary>
        /// Removes a branch of the timeline.
        /// </summary>
        /// <param name="historyEvent">HistoryEvent object which belongs to the branch to be removed.</param>
        public bool TrimTimeline(HistoryEvent historyEvent)
        {
            if (historyEvent == null || historyEvent.Children.Count != 0 || historyEvent == _machine.History.CurrentEvent || historyEvent.Parent == null)
            {
                return false;
            }

            // Walk up the tree to find the node to be removed...
            HistoryEvent parent = historyEvent.Parent;
            HistoryEvent child = historyEvent;
            while (parent != null && parent.Children.Count == 1)
            {
                child = parent;
                parent = parent.Parent;
            }

            return _machine.DeleteBranch(child);
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
