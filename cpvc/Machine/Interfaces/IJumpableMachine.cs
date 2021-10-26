namespace CPvC
{
    public interface IJumpableMachine : IMachine
    {
        void JumpToMostRecentBookmark();
        void JumpToBookmark(HistoryEvent bookmarkEvent);
    }
}
