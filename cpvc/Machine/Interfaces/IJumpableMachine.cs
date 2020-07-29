namespace CPvC
{
    public interface IJumpableMachine : ICoreMachine
    {
        void JumpToMostRecentBookmark();
        void JumpToBookmark(HistoryEvent bookmarkEvent);
    }
}
