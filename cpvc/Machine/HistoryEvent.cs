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

        public abstract void GetLine(List<string> lines, Blobs blobs);

        public bool IsEqualToOrAncestorOf(HistoryEvent ancestor)
        {
            return Node.IsEqualToOrAncestorOf(ancestor?.Node);
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

            return maxTicks;
        }
    }

    public class RootHistoryEvent : HistoryEvent
    {
        internal RootHistoryEvent(HistoryNode historyNode)
        {
            _node = historyNode as RootHistoryNode;
        }

        public override void GetLine(List<string> lines, Blobs blobs)
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

        public override void GetLine(List<string> lines, Blobs blobs)
        {
            int stateBlobId = blobs.Add(Bookmark.State);
            int screenBlobId = blobs.Add(Bookmark.Screen);

            lines.Add(MachineFile.ArgCommand(stateBlobId, Bookmark.State, true));
            lines.Add(MachineFile.ArgCommand(screenBlobId, Bookmark.Screen, true));

            lines.Add(MachineFile.AddBookmarkCommand(blobs, Id, Ticks, Bookmark.System, Bookmark.Version, Node.CreateDate, stateBlobId, screenBlobId));
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

        public override void GetLine(List<string> lines, Blobs blobs)
        {
            switch (CoreAction.Type)
            {
                case MachineRequest.Types.KeyPress:
                    lines.Add(MachineFile.KeyCommand(Id, CoreAction.Ticks, CoreAction.KeyCode, CoreAction.KeyDown));
                    break;
                case MachineRequest.Types.Reset:
                    lines.Add(MachineFile.ResetCommand(Id, CoreAction.Ticks));
                    break;
                case MachineRequest.Types.LoadDisc:
                    {
                        int mediaBlobId = blobs.Add(CoreAction.MediaBuffer);

                        lines.Add(MachineFile.ArgCommand(mediaBlobId, CoreAction.MediaBuffer, true));
                        lines.Add(MachineFile.LoadDiscCommand(Id, CoreAction.Ticks, CoreAction.Drive, mediaBlobId));
                    }
                    break;
                case MachineRequest.Types.LoadTape:
                    {
                        int mediaBlobId = blobs.Add(CoreAction.MediaBuffer);

                        lines.Add(MachineFile.ArgCommand(mediaBlobId, CoreAction.MediaBuffer, true));
                        lines.Add(MachineFile.LoadTapeCommand(Id, CoreAction.Ticks, mediaBlobId));
                    }
                    break;
                case MachineRequest.Types.CoreVersion:
                    lines.Add(MachineFile.VersionCommand(Id, CoreAction.Ticks, CoreAction.Version));
                    break;
                case MachineRequest.Types.RunUntil:
                    lines.Add(MachineFile.RunCommand(Id, CoreAction.Ticks, CoreAction.StopTicks));
                    break;
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

        public MachineAction CoreAction
        {
            get
            {
                return _node.CoreAction;
            }
        }

        public override UInt64 Ticks
        {
            get
            {
                if (_node.CoreAction.Type == MachineRequest.Types.RunUntil)
                {
                    return _node.CoreAction.StopTicks;
                }

                return _node.Ticks;
            }
        }

        private CoreActionHistoryNode _node;
    }
}
