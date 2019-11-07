using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class BookmarkSelectWindowLogic
    {
        private Machine _machine;

        public BookmarkSelectWindowLogic(Machine machine)
        {
            _machine = machine;
        }

        public void DeleteBranches(System.Collections.IList items)
        {
            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.TrimTimeline(item.HistoryEvent);
                }
            }
        }

        public void DeleteBookmarks(System.Collections.IList items)
        {
            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.SetBookmark(item.HistoryEvent, null);
                }
            }
        }
    }
}
