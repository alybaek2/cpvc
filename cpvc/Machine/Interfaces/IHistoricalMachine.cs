using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    interface IHistoricalMachine
    {
        History History { get; }
        void DeleteBookmarks(List<HistoryEvent> bookmarksToDelete);
        void DeleteBranches(List<HistoryEvent> branchesToDelete);
        void JumpToBookmark(BookmarkHistoryEvent bookmarkEvent);
    }
}
