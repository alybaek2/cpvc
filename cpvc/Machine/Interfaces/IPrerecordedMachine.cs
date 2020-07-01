namespace CPvC
{
    public interface IPrerecordedMachine
    {
        void SeekToStart();
        void SeekToPreviousBookmark();
        void SeekToNextBookmark();
    }
}
