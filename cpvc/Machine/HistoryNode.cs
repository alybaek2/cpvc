using System;
using System.Collections.Generic;

namespace CPvC
{
    internal class HistoryNode
    {
        protected HistoryNode(int id, UInt64 ticks, HistoryNode parent, DateTime createDate)
        {
            Id = id;
            Ticks = ticks;
            Parent = parent;
            CreateDate = createDate;
            Children = new List<HistoryNode>();
        }

        public int Id { get; }
        public UInt64 Ticks { get; }
        public HistoryNode Parent { get; set; }
        public DateTime CreateDate { get; }
        public List<HistoryNode> Children { get; }
        public HistoryEvent HistoryEvent { get; protected set; }

        public bool IsEqualToOrAncestorOf(HistoryNode ancestor)
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
    }

    internal class RootHistoryNode : HistoryNode
    {
        private const int RootId = -1;

        internal RootHistoryNode() : base(RootId, 0, null, DateTime.Now)
        {
            HistoryEvent = new RootHistoryEvent(this);
        }
    }

    internal class BookmarkHistoryNode : HistoryNode
    {
        public Bookmark Bookmark { get; }

        internal BookmarkHistoryNode(int id, UInt64 ticks, Bookmark bookmark, HistoryNode parent, DateTime createDate) : base(id, ticks, parent, createDate)
        {
            Bookmark = bookmark;

            HistoryEvent = new BookmarkHistoryEvent(this);
        }
    }

    internal class CoreActionHistoryNode : HistoryNode
    {
        public IMachineAction CoreAction { get; }

        internal CoreActionHistoryNode(int id, UInt64 ticks, IMachineAction action, HistoryNode parent, DateTime createDate) : base(id, ticks, parent, createDate)
        {
            CoreAction = action;

            HistoryEvent = new CoreActionHistoryEvent(this);
        }
    }
}
