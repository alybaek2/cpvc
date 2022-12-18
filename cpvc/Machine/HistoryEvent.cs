using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CPvC
{
    public interface IHistoryEvent
    {

    }

    public abstract class HistoryEvent
    {
        internal abstract HistoryNode Node { get; }

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

        public int Id
        {
            get
            {
                return Node.Id;
            }
        }

        public virtual UInt64 Ticks
        {
            get
            {
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

        public void Sort(Comparison<HistoryEvent> comparison)
        {
           Node.Children.Sort((x, y) => comparison(x.HistoryEvent, y.HistoryEvent));
        }

        public T MostRecent<T>() where T : HistoryEvent
        {
            HistoryEvent historyEvent = this;
            while (!(historyEvent is RootHistoryEvent))
            {
                if (historyEvent is T historyEventType)
                {
                    return historyEventType;
                }

                historyEvent = historyEvent.Parent;
            }

            return null;
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
            UInt64 maxTicks = Ticks;

            int i = 0;
            while (i < events.Count)
            {
                HistoryEvent e = events[0];
                events.RemoveAt(0);

                if (e.Children.Count == 0)
                {
                    if (e.Ticks > maxTicks)
                    {
                        maxTicks = e.Ticks;
                    }
                }
                else
                {
                    events.AddRange(e.Children);
                }
            }

            UInt64 compare = Node.GetCachedMDT();
            if (compare != maxTicks)
            {
                string h = "";
            }

            return maxTicks;
        }
    }

    public class RootHistoryEvent : HistoryEvent
    {
        internal RootHistoryEvent(HistoryNode historyNode)
        {
            _node = historyNode as RootHistoryNode;
        }

        internal override HistoryNode Node
        {
            get
            {
                return _node;
            }
        }

        private RootHistoryNode _node;
    }

    public class BookmarkHistoryEvent : HistoryEvent
    {
        internal BookmarkHistoryEvent(BookmarkHistoryNode historyNode)
        {
            _node = historyNode;
        }

        internal override HistoryNode Node
        {
            get
            {
                return _node;
            }
        }

        public Bookmark Bookmark
        {
            get
            {
                return _node.Bookmark;
            }
        }


        private BookmarkHistoryNode _node;
    }

    public class CoreActionHistoryEvent : HistoryEvent, INotifyPropertyChanged
    {
        internal CoreActionHistoryEvent(CoreActionHistoryNode historyNode) : base()
        {
            _node = historyNode;

            if (_node.CoreAction is RunUntilAction runUntilAction)
            {
                runUntilAction.PropertyChanged += RunUntilAction_PropertyChanged;
            }
        }

        private void RunUntilAction_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RunUntilRequest.StopTicks))
            {
                Node.InvalidateCachedMDT();

                //HistoryEvent h = this;
                //while (h != null)
                //{
                //    h.Node.InvalidateCachedMDT();
                //    h = h.Parent;
                //}

                OnPropertyChanged(nameof(Ticks));
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        internal override HistoryNode Node
        {
            get
            {
                return _node;
            }
        }

        public IMachineAction CoreAction
        {
            get
            {
                return _node.CoreAction;
            }
        }

        //public override UInt64 Ticks
        //{
        //    get
        //    {
        //        if (_node.CoreAction is RunUntilAction runUntilAction)
        //        {
        //            return runUntilAction.StopTicks;
        //        }

        //        return _node.Ticks;
        //    }
        //}

        public event PropertyChangedEventHandler PropertyChanged;

        private CoreActionHistoryNode _node;
    }
}
