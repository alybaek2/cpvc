using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC
{
    /// <summary>
    /// Class that represents an event in a machine's timeline. Can either be an action taken by the core (e.g. a keypress)
    /// or a checkpoint (which can be used to mark the end of a branch of the timeliine, for creating bookmarks, or for
    /// branching the timeline). Note that RunUntil core actions are not recorded as part of the history.
    /// </summary>
    public class HistoryEvent
    {
        public enum Types
        {
            Checkpoint,
            CoreAction
        }

        public Types Type { get; }
        public int Id { get; }
        public UInt64 Ticks { get; }
        public DateTime CreateDate { get; private set; }
        public Bookmark Bookmark { get; set; }
        public CoreAction CoreAction { get; private set; }
        public HistoryEvent Parent { get; set; }
        public List<HistoryEvent> Children { get; }

        public HistoryEvent(int id, Types type, UInt64 ticks)
        {
            Type = type;
            Id = id;
            Ticks = ticks;

            Children = new List<HistoryEvent>();
        }

        public void AddChild(HistoryEvent historyEvent)
        {
            if (Children.Contains(historyEvent))
            {
                return;
            }

            Children.Add(historyEvent);
            historyEvent.Parent = this;
        }

        public void RemoveChild(HistoryEvent historyEvent)
        {
            if (!Children.Contains(historyEvent))
            {
                return;
            }

            historyEvent.Parent = null;
            Children.Remove(historyEvent);
        }

        public bool IsEqualToOrAncestorOf(HistoryEvent ancestor)
        {
            while (ancestor != null)
            {
                if (ancestor == this)
                {
                    return true;
                }

                ancestor = ancestor.Parent;
            }

            return false;
        }

        public List<HistoryEvent> GetSelfAndDescendents()
        {
            List<HistoryEvent> descendents = new List<HistoryEvent>();
            descendents.Add(this);

            for (int i = 0; i < descendents.Count; i++)
            {
                HistoryEvent historyEvent = descendents[i];
                descendents.AddRange(historyEvent.Children);
            }

            return descendents;
        }

        static public HistoryEvent CreateCoreAction(int id, CoreAction coreAction)
        {
            HistoryEvent historyEvent = new HistoryEvent(id, Types.CoreAction, coreAction.Ticks)
            {
                CoreAction = coreAction
            };

            return historyEvent;
        }

        static public HistoryEvent CreateCheckpoint(int id, UInt64 ticks, DateTime createdDate, Bookmark bookmark)
        {
            HistoryEvent historyEvent = new HistoryEvent(id, Types.Checkpoint, ticks)
            {
                Bookmark = bookmark,
                CreateDate = createdDate
            };

            return historyEvent;
        }

        /// <summary>
        /// Returns the maximum ticks value of any given HistoryEvent's descendents. Used when sorting children in <c>AddEventToItem</c>.
        /// </summary>
        /// <param name="historyEvent">The HistoryEvent object.</param>
        /// <returns></returns>
        public UInt64 GetMaxDescendentTicks()
        {
            if (Children.Count == 0)
            {
                return Ticks;
            }

            return Children.Select(x => x.GetMaxDescendentTicks()).Max();
        }
    }
}
