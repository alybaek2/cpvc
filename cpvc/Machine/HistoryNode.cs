using System;
using System.Collections.Generic;
using System.Linq;

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
            _maxDescendentTicks = null;
        }

        public int Id { get; }
        virtual public UInt64 Ticks { get; }
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

        public void Sort(Comparison<HistoryNode> comparison)
        {
            Children.Sort(comparison);
        }

        public UInt64 GetCachedMDT()
        {
            if (_maxDescendentTicks.HasValue)
            {
                return _maxDescendentTicks.Value;
            }

            if (Children.Count == 0)
            {
                _maxDescendentTicks = Ticks;

                return _maxDescendentTicks.Value;
            }

            List<HistoryNode> descendents = new List<HistoryNode>();
            descendents.Add(this);

            for (int i = 0; i < descendents.Count; i++)
            {
                descendents.AddRange(descendents[i].Children);
            }

            for (int i = descendents.Count - 1; i >= 0; i--)
            {
                HistoryNode node = descendents[i];
                if (node.Children.Count > 0)
                {
                    node._maxDescendentTicks = node.Children.Select(x => x.GetCachedMDT()).Max();
                }
            }

            return _maxDescendentTicks.Value;
        }

        internal void InvalidateCachedMDT()
        {
            HistoryNode node = this;
            while (node != null)
            {
                // If we're invalidated, all our ancestors are necessarily invalidated too!
                // in this case, break, so that this isn't a potentially very costly operation.
                if (node._maxDescendentTicks == null)
                {
                    break;
                }

                node._maxDescendentTicks = null;
                node = node.Parent;
            }

            _maxDescendentTicks = null;
        }

        private UInt64? _maxDescendentTicks;
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

        public override UInt64 Ticks
        {
            get
            {
                if (CoreAction is RunUntilAction runUntilAction)
                {
                    return runUntilAction.StopTicks;
                }

                return CoreAction.Ticks;
            }
        }

    }
}
