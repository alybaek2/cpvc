using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class HistoryEvent
    {
        internal HistoryEvent(HistoryNode historyNode)
        {
            Node = historyNode;
        }

        internal HistoryNode Node { get; private set; }

        public HistoryEvent Parent
        {
            get
            {
                return Node.Parent?.HistoryEvent;
            }
        }

        public List<HistoryEvent> Children
        {
            get
            {
                return Node.Children.Select(x => x.HistoryEvent).ToList();
            }
        }

        public HistoryEventType Type
        {
            get
            {
                return Node.Type;
            }
        }

        public int Id
        {
            get
            {
                return Node.Id;
            }
        }

        public Bookmark Bookmark
        {
            get
            {
                return Node.Bookmark;
            }
        }

        public CoreAction CoreAction
        {
            get
            {
                return Node.CoreAction;
            }
        }

        public UInt64 Ticks
        {
            get
            {
                return Node.Ticks;
            }
        }

        public UInt64 EndTicks
        {
            get
            {
                if (Node.Type == HistoryEventType.CoreAction && Node.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    return Node.CoreAction.StopTicks;
                }

                return Node.Ticks;
            }
        }

        public DateTime CreateDate
        {
            get
            {
                return Node.CreateDate;
            }
        }

        public bool IsEqualToOrAncestorOf(HistoryEvent ancestor)
        {
            return Node.IsEqualToOrAncestorOf(ancestor?.Node);
        }

        /// <summary>
        /// Returns the maximum ticks value of any given HistoryEvent's descendents. Used when sorting children in <c>AddEventToItem</c>.
        /// </summary>
        /// <param name="historyEvent">The HistoryEvent object.</param>
        /// <returns></returns>
        public UInt64 GetMaxDescendentTicks()
        {
            List<HistoryEvent> events = new List<HistoryEvent>();
            events.Add(this);
            UInt64 maxTicks = EndTicks;

            int i = 0;
            while (i < events.Count)
            {
                HistoryEvent e = events[0];
                events.RemoveAt(0);

                if (e.Children.Count == 0)
                {
                    if (e.EndTicks > maxTicks)
                    {
                        maxTicks = e.EndTicks;
                    }
                }
                else
                {
                    events.AddRange(e.Children);
                }
            }

            return maxTicks;
        }
    }
}
