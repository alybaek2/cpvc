namespace CPvC
{
    public interface IMachineFileReader
    {
        void SetName(string name);
        void DeleteEvent(int id);
        void SetBookmark(int id, Bookmark bookmark);
        void SetCurrentEvent(int id);
        void AddHistoryEvent(HistoryEvent historyEvent);
    }
}
