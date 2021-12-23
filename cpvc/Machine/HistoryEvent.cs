using System;
using System.Collections.Generic;
using System.Linq;

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

        public UInt64 Ticks
        {
            get
            {
                return Node.Ticks;
            }
        }

        public virtual UInt64 EndTicks
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

        public abstract string GetLine();

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

    public class RootHistoryEvent : HistoryEvent
    {
        internal RootHistoryEvent(HistoryNode historyNode)
        {
            _node = historyNode as RootHistoryNode;
        }

        public override string GetLine()
        {
            throw new NotImplementedException();
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

        public override string GetLine()
        {
            return MachineFileWriter.AddBookmarkCommand(Id, Ticks, Bookmark.System, Bookmark.Version, Bookmark.State.GetBytes(), Bookmark.Screen.GetBytes());
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

    public class CoreActionHistoryEvent : HistoryEvent
    {
        internal CoreActionHistoryEvent(CoreActionHistoryNode historyNode) : base()
        {
            _node = historyNode;
        }

        public override string GetLine()
        {
            switch (CoreAction.Type)
            {
                case CoreRequest.Types.KeyPress:
                    return MachineFileWriter.KeyCommand(Id, CoreAction.Ticks, CoreAction.KeyCode, CoreAction.KeyDown);
                case CoreRequest.Types.Reset:
                    return MachineFileWriter.ResetCommand(Id, CoreAction.Ticks);
                case CoreRequest.Types.LoadDisc:
                    return MachineFileWriter.LoadDiscCommand(Id, CoreAction.Ticks, CoreAction.Drive, CoreAction.MediaBuffer.GetBytes());
                case CoreRequest.Types.LoadTape:
                    return MachineFileWriter.LoadTapeCommand(Id, CoreAction.Ticks, CoreAction.MediaBuffer.GetBytes());
                case CoreRequest.Types.CoreVersion:
                    return MachineFileWriter.VersionCommand(Id, CoreAction.Ticks, CoreAction.Version);
                case CoreRequest.Types.RunUntil:
                    return MachineFileWriter.RunCommand(Id, CoreAction.Ticks, CoreAction.StopTicks);
                default:
                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", CoreAction.Type), "type");
            }
        }

        internal override HistoryNode Node
        {
            get
            {
                return _node;
            }
        }

        public CoreAction CoreAction
        {
            get
            {
                return _node.CoreAction;
            }
        }

        public override UInt64 EndTicks
        {
            get
            {
                if (_node.CoreAction.Type == CoreRequest.Types.RunUntil)
                {
                    return _node.CoreAction.StopTicks;
                }

                return _node.Ticks;
            }
        }

        private CoreActionHistoryNode _node;
    }
}
