using System;
using System.Collections.Generic;

namespace CPvC
{
    internal class HistoryNode
    {
        private const int RootId = -1;

        internal HistoryNode() : this(HistoryEventType.Root, RootId, 0, null, null, null, DateTime.Now)
        {
        }

        internal HistoryNode(int id, UInt64 ticks, Bookmark bookmark, HistoryNode parent, DateTime createDate) : this(HistoryEventType.Bookmark, id, ticks, null, bookmark, parent, createDate)
        {
        }

        internal HistoryNode(int id, UInt64 ticks, CoreAction action, HistoryNode parent, DateTime createDate) : this(HistoryEventType.CoreAction, id, ticks, action, null, parent, createDate)
        {
        }

        private HistoryNode(HistoryEventType type, int id, UInt64 ticks, CoreAction action, Bookmark bookmark, HistoryNode parent, DateTime createDate)
        {
            Type = type;
            Id = id;
            Ticks = ticks;
            Bookmark = bookmark;
            CoreAction = action;
            Parent = parent;
            CreateDate = createDate;
            Children = new List<HistoryNode>();
            HistoryEvent = new HistoryEvent(this);
        }

        public HistoryEventType Type { get; }
        public int Id { get; }
        public UInt64 Ticks { get; }
        public Bookmark Bookmark { get; }
        public CoreAction CoreAction { get; }
        public HistoryNode Parent { get; set; }
        public DateTime CreateDate { get; }
        public List<HistoryNode> Children { get; }
        public HistoryEvent HistoryEvent { get; }

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
}
