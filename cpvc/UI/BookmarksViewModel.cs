using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace CPvC.UI
{
    /// <summary>
    /// View model for the Bookmarks window.
    /// </summary>
    public class BookmarksViewModel
    {
        private History _history;
        private ObservableCollection<HistoryViewItem> _selectedItems;

        public Command DeleteBookmarksCommand { get; }
        public Command DeleteBranchesCommand { get; }

        public ObservableCollection<HistoryViewItem> Items { get; }

        public ReadOnlyObservableCollection<HistoryViewItem> SelectedItems { get; }

        public HistoryViewItem CurrentItem
        {
            get
            {
                return Items.FirstOrDefault(item => item.HistoryEvent == _history.CurrentEvent);
            }
        }

        public BookmarksViewModel(History history, Action<Action> canExecuteChangedInvoker)
        {
            _selectedItems = new ObservableCollection<HistoryViewItem>();
            SelectedItems = new ReadOnlyObservableCollection<HistoryViewItem>(_selectedItems);

            DeleteBookmarksCommand = new Command(
                p => DeleteBookmarks(),
                p => SelectedItems.Any(item => item.HistoryEvent is BookmarkHistoryEvent),
                canExecuteChangedInvoker
            );

            DeleteBranchesCommand = new Command(
                p => DeleteBranches(),
                p => SelectedItems.Any(item => !(item.HistoryEvent.Children.Count != 0 || item.HistoryEvent == _history.CurrentEvent || item.HistoryEvent is RootHistoryEvent)),
                canExecuteChangedInvoker
            );

            _history = history;
            Items = new ObservableCollection<HistoryViewItem>();
            RefreshHistoryViewItems();
        }

        public void AddSelectedItem(HistoryViewItem addedItem)
        {
            if (!_selectedItems.Contains(addedItem))
            {
                _selectedItems.Add(addedItem);

                DeleteBookmarksCommand.InvokeCanExecuteChanged(this, new EventArgs());
                DeleteBranchesCommand.InvokeCanExecuteChanged(this, new EventArgs());
            }
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
                new Tuple<int, HistoryEvent>(0, _history.RootEvent)
            };

            // Note that items is sorted in ascending order of ticks (i.e. oldest to most recent).
            List<HistoryViewItem> items = CreateHistoryView(_history);

            // Draw items to their respective canvasses.
            HistoryViewItem next = null;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                HistoryViewItem item = items[i];
                item.Draw(next, _history.CurrentEvent);

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

        static private bool InterestingEvent(HistoryEvent historyEvent)
        {
            bool interested = false;
            if (historyEvent is RootHistoryEvent)
            {
                interested = true;
            }
            else if (historyEvent is BookmarkHistoryEvent)
            {
                interested = true;
            }
            else if (historyEvent.Children.Count != 1)
            {
                interested = true;
            }

            return interested;
        }

        static public List<HistoryViewItem> CreateHistoryView(History history)
        {
            List<HistoryEvent> verticalOrdering = new List<HistoryEvent>();

            Dictionary<HistoryEvent, HistoryEvent> eventParents = new Dictionary<HistoryEvent, HistoryEvent>();

            // Figure out vertical and horizontal ordering
            Queue<HistoryEvent> historyEvents = new Queue<HistoryEvent>();
            historyEvents.Enqueue(history.RootEvent);

            while (historyEvents.Count > 0)
            {
                HistoryEvent historyEvent = historyEvents.Dequeue();

                if (InterestingEvent(historyEvent))
                {
                    // Get interesting parent
                    HistoryEvent interestingParent = historyEvent.Parent;
                    while (interestingParent != null && !InterestingEvent(interestingParent))
                    {
                        interestingParent = interestingParent.Parent;
                    }

                    eventParents.Add(historyEvent, interestingParent);
                }

                foreach (HistoryEvent childHistoryEvent in historyEvent.Children)
                {
                    historyEvents.Enqueue(childHistoryEvent);
                }
            }

            Stack<HistoryEvent> historyEventsStack = new Stack<HistoryEvent>();
            historyEventsStack.Push(history.RootEvent);

            List<HistoryEvent> horizontalOrdering = new List<HistoryEvent>();

            List<HistoryEvent> children = new List<HistoryEvent>();

            while (historyEventsStack.Count > 0)
            {
                HistoryEvent historyEvent = historyEventsStack.Pop();

                children.Clear();
                children.AddRange(historyEvent.Children);
                children.Sort((x, y) => x.MaxDescendentTicks.CompareTo(y.MaxDescendentTicks));

                if (InterestingEvent(historyEvent))
                {
                    horizontalOrdering.Add(historyEvent);
                }

                foreach (HistoryEvent child in children)
                {
                    historyEventsStack.Push(child);
                }
            }

            verticalOrdering = new List<HistoryEvent>(horizontalOrdering);
            verticalOrdering.Sort((x, y) =>
            {
                if (x.Ticks < y.Ticks)
                {
                    return -1;
                }
                else if (x.Ticks > y.Ticks)
                {
                    return 1;
                }
                else
                {
                    if (x.IsEqualToOrAncestorOf(y))
                    {
                        return -1;
                    }
                    else if (y.IsEqualToOrAncestorOf(x))
                    {
                        return 1;
                    }
                }

                return 0;
            });

            Dictionary<HistoryEvent, int> horizontalLookup = new Dictionary<HistoryEvent, int>();
            Dictionary<HistoryEvent, int> verticalLookup = new Dictionary<HistoryEvent, int>();

            for (int i = 0; i < horizontalOrdering.Count; i++)
            {
                horizontalLookup[horizontalOrdering[i]] = i;
            }

            for (int i = 0; i < verticalOrdering.Count; i++)
            {
                verticalLookup[verticalOrdering[i]] = i;
            }

            List<HistoryViewItem> historyItems = new List<HistoryViewItem>();
            HistoryViewItem previousViewItem = null;

            for (int v = 0; v < verticalOrdering.Count; v++)
            {
                HistoryViewItem viewItem = new HistoryViewItem(verticalOrdering[v]);

                // Add events; either "passthrough" events, or the actual event for this HistoryViewItem.
                for (int h = 0; h < horizontalOrdering.Count; h++)
                {
                    int verticalHorizontalIndex = verticalLookup[horizontalOrdering[h]];
                    int previousVerticalIndex = -1;

                    if (eventParents.TryGetValue(horizontalOrdering[h], out HistoryEvent previousEvent) && previousEvent != null)
                    {
                        previousVerticalIndex = verticalLookup[previousEvent];
                    }

                    if (previousVerticalIndex < v && v <= verticalHorizontalIndex)
                    {
                        int hindex = viewItem.Events.Count;
                        if (previousViewItem != null)
                        {
                            int prevIndex = previousViewItem.Events.FindIndex(x => x == horizontalOrdering[h]);
                            if (prevIndex == -1)
                            {
                                prevIndex = previousViewItem.Events.FindIndex(x => x == previousEvent);
                            }

                            if (prevIndex != -1)
                            {
                                if (hindex < prevIndex)
                                {
                                    for (int g = 0; g < prevIndex - hindex; g++)
                                    {
                                        viewItem.Events.Add(null);
                                    }
                                }
                            }
                        }

                        viewItem.Events.Add(horizontalOrdering[h]);
                    }
                }

                historyItems.Add(viewItem);

                previousViewItem = viewItem;
            }

            return historyItems;
        }

        private void DeleteBookmarks()
        {
            bool refresh = false;

            foreach (HistoryViewItem selectedItem in SelectedItems)
            {
                if (selectedItem.HistoryEvent is BookmarkHistoryEvent bookmarkHistoryEvent)
                {
                    refresh |= _history.DeleteBookmark(bookmarkHistoryEvent);
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
                refresh |= DeleteBranch(selectedItem.HistoryEvent);
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
        public bool DeleteBranch(HistoryEvent historyEvent)
        {
            if (historyEvent.Children.Count != 0 || historyEvent == _history.CurrentEvent || historyEvent is RootHistoryEvent)
            {
                return false;
            }

            // Walk up the tree to find the node to be removed...
            HistoryEvent parent = historyEvent.Parent;
            HistoryEvent child = historyEvent;
            while (parent != _history.CurrentEvent && parent.Children.Count == 1)
            {
                child = parent;
                parent = parent.Parent;
            }

            return _history.DeleteBranch(child);
        }
    }
}
