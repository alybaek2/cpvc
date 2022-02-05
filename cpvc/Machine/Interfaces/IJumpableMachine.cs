namespace CPvC
{
    public interface IJumpableMachine
    {
        void JumpToMostRecentBookmark();
        void JumpToBookmark(BookmarkHistoryEvent bookmarkEvent);
        void JumpToRoot();
    }
}
