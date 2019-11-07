using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace CPvC
{
    /// <summary>
    /// Provides a group of static helper methods for the history view shown in BookmarkSelectWindow.
    /// </summary>
    static public class HistoryView
    {
        /// <summary>
        /// Populates a ListView control with HistoryViewItems based on the given rootEvent.
        /// </summary>
        /// <param name="historyListView">The ListView control to populate.</param>
        /// <param name="rootEvent">The root HistoryEvent used to create the history view.</param>
        /// <param name="currentEvent">The current event in the machine's history. Used to render this node differently to other events in the history view.</param>
        static public void PopulateListView(ListView historyListView, HistoryEvent rootEvent, HistoryEvent currentEvent)
        {
            // Generate items...
            List<HistoryViewItem> items = new List<HistoryViewItem>();
            AddEventToItems(items, 0, rootEvent);

            // Render items...
            HistoryViewItem next = null;
            historyListView.Items.Clear();
            for (int i = items.Count - 1; i >= 0; i--)
            {
                HistoryViewItem item = items[i];
                item.Draw(next, currentEvent);
                historyListView.Items.Add(item);

                next = item;
            }
        }

        /// <summary>
        /// Populates the given Display with the currently selected HistoryViewItem if it has a bookmark.
        /// </summary>
        /// <param name="historyListView">The ListView control.</param>
        /// <param name="display">The Display object to populate.</param>
        /// <param name="machine">The Machine object whose history is being displayed.</param>
        /// <param name="image">The Image object displaying the selected bookmark.</param>
        /// <returns>A boolean indicating if the Display object was populated (true if the selected HistoryViewItem has a bookmark; false otherwise).</returns>
        static public bool SelectBookmark(ListView historyListView, Display display, Machine machine, Image image)
        {
            HistoryViewItem viewItem = (HistoryViewItem)historyListView.SelectedItem;
            if (viewItem != null)
            {
                HistoryEvent historyEvent = viewItem.HistoryEvent;

                // Even though the current event doesn't necessarily have a bookmark, we can still populate the display.
                if (historyEvent == machine.CurrentEvent)
                {
                    image.Source = machine.Display.Bitmap;
                    return true;
                }

                if (historyEvent != null && historyEvent.Type == HistoryEvent.Types.Checkpoint && historyEvent.Bookmark != null)
                {
                    display.GetFromBookmark(historyEvent.Bookmark);
                    image.Source = display.Bitmap;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new HistoryViewItem based on a HistoryEvent object and adds it to a List of HistoryEvents.
        /// </summary>
        /// <param name="items">The list of HistoryEvents.</param>
        /// <param name="left">The amount the new HistoryEvent should be indented from the left in the history view.</param>
        /// <param name="historyEvent">The HistoryEVent object.</param>
        static private void AddEventToItems(List<HistoryViewItem> items, int left, HistoryEvent historyEvent)
        {
            // Avoid calling this function recursively since the depth of the history could be large...
            List<Tuple<int, HistoryEvent>> eventStack = new List<Tuple<int, HistoryEvent>>
            {
                new Tuple<int, HistoryEvent>(left, historyEvent)
            };

            while (eventStack.Count > 0)
            {
                left = eventStack[0].Item1;
                historyEvent = eventStack[0].Item2;
                eventStack.RemoveAt(0);

                if (ShouldShow(historyEvent))
                {
                    HistoryViewItem item = new HistoryViewItem(historyEvent);

                    // Figure out where this new item should be placed; note that items is sorted in
                    // ascending order of ticks (i.e. oldest to most recent).
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
    }
}
