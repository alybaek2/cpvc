using System.Collections.Generic;

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
