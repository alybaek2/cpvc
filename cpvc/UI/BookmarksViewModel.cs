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
        public delegate void ItemSelectedDelegate();

        private LocalMachine _machine;
        private HistoryViewItem _selectedItem;
        private Display _display;

        private Command _deleteBookmarksCommand;
        private Command _deleteBranchesCommand;
        private Command _jumpToBookmarkCommand;
        private Command _replayTimelineCommand;

        public ICommand DeleteBookmarksCommand
        {
            get { return _deleteBookmarksCommand; }
        }

        public ICommand DeleteBranchesCommand
        {
            get { return _deleteBranchesCommand; }
        }

        public ICommand JumpToBookmarkCommand
        {
            get { return _jumpToBookmarkCommand; }
        }

        public ICommand ReplayTimelineCommand
        {
            get { return _replayTimelineCommand; }
        }

        public HistoryEvent SelectedJumpEvent { get; private set; }
        public HistoryEvent SelectedReplayEvent { get; private set; }

        public WriteableBitmap Bitmap { get; private set; }
        public ObservableCollection<HistoryViewItem> Items { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public BookmarksViewModel(LocalMachine machine, ItemSelectedDelegate itemSelected)
        {
            _replayTimelineCommand = new Command(
                p => ReplayTimeline(itemSelected),
                p => (SelectedItem as HistoryViewItem)?.HistoryEvent != null
            );

            _jumpToBookmarkCommand = new Command(
                p => JumpToBookmark(itemSelected),
                p => (SelectedItem as HistoryViewItem)?.HistoryEvent.Bookmark != null
            );

            _deleteBookmarksCommand = new Command(
                p => DeleteBookmarks(),
                p => (SelectedItem as HistoryViewItem)?.HistoryEvent.Bookmark != null
            );

            _deleteBranchesCommand = new Command(
                p => DeleteBranches(),
                p =>
                {
                    HistoryEvent historyEvent = (SelectedItem as HistoryViewItem)?.HistoryEvent;

                    return !historyEvent?.IsEqualToOrAncestorOf(machine.History.CurrentEvent) ?? false;
                }
            );

            _display = new Display();
            _machine = machine;
            Items = new ObservableCollection<HistoryViewItem>();
            RefreshHistoryViewItems();

            // The initial selected item is set to the current event.
            SelectedItem = Items.FirstOrDefault(i => i.HistoryEvent == _machine.History.CurrentEvent);
        }

        /// <summary>
        /// Represents the currently selected HistoryViewItem. Note that when this is
        /// changed, other properties are recalculated as well.
        /// </summary>
        public HistoryViewItem SelectedItem
        {
            get
            {
                return _selectedItem;
            }

            set
            {
                if (_selectedItem == value)
                {
                    return;
                }

                _selectedItem = value;
                OnPropertyChanged();

                WriteableBitmap bitmap = null;
                if (_selectedItem != null)
                {
                    HistoryEvent historyEvent = SelectedItem.HistoryEvent;

                    // Even though the current event doesn't necessarily have a bookmark, we can still populate the display.
                    if (historyEvent == _machine.History.CurrentEvent)
                    {
                        bitmap = _machine.Display.Bitmap;
                    }
                    else if (historyEvent.Type == HistoryEventType.Bookmark && historyEvent.Bookmark != null)
                    {
                        _display.GetFromBookmark(historyEvent.Bookmark);

                        bitmap = _display.Bitmap;
                    }
                }

                Bitmap = bitmap;
                OnPropertyChanged("Bitmap");
            }
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

            if (historyEvent.Type == HistoryEventType.Bookmark && historyEvent.Bookmark != null)
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
            if (SelectedItem?.HistoryEvent.Bookmark != null)
            {
                if (_machine.DeleteBookmark(SelectedItem.HistoryEvent))
                {
                    RefreshHistoryViewItems();
                }
            }
        }

        private void DeleteBranches()
        {
            if (SelectedItem?.HistoryEvent != null)
            {
                TrimTimeline(SelectedItem.HistoryEvent);

                RefreshHistoryViewItems();
            }
        }

        /// <summary>
        /// Removes a branch of the timeline.
        /// </summary>
        /// <param name="historyEvent">HistoryEvent object which belongs to the branch to be removed.</param>
        public void TrimTimeline(HistoryEvent historyEvent)
        {
            if (historyEvent == null || historyEvent.Children.Count != 0 || historyEvent == _machine.History.CurrentEvent || historyEvent.Parent == null)
            {
                return;
            }

            // Walk up the tree to find the node to be removed...
            HistoryEvent parent = historyEvent.Parent;
            HistoryEvent child = historyEvent;
            while (parent != null && parent.Children.Count == 1)
            {
                child = parent;
                parent = parent.Parent;
            }

            _machine.DeleteBranch(child);
        }

        private void JumpToBookmark(ItemSelectedDelegate itemSelected)
        {
            if (SelectedItem?.HistoryEvent.Bookmark != null)
            {
                SelectedJumpEvent = SelectedItem.HistoryEvent;
                itemSelected();
            }
        }

        private void ReplayTimeline(ItemSelectedDelegate itemSelected)
        {
            if (SelectedItem?.HistoryEvent != null)
            {
                SelectedReplayEvent = SelectedItem.HistoryEvent;
                itemSelected();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
