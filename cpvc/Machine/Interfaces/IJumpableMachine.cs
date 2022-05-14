namespace CPvC
{
    public interface IJumpableMachine : IMachine
    {
        void JumpToMostRecentBookmark();
        void JumpToBookmark(BookmarkHistoryEvent bookmarkEvent);
        void JumpToRoot();
    }
}
