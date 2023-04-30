namespace CPvC
{
    public interface IReplayableMachine
    {
        void StartReplay(BookmarkHistoryEvent beginEvent, HistoryEvent endEvent);
        void StopReplay();
    }
}
