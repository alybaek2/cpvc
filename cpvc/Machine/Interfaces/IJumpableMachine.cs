namespace CPvC
{
    public interface IJumpableMachine
    {
        void JumpToMostRecentBookmark();
        void JumpToBookmark(HistoryEvent bookmarkEvent);
    }
}
