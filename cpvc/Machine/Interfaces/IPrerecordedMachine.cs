namespace CPvC
{
    public interface IPrerecordedMachine
    {
        MachineRequest SeekToStart();
        MachineRequest SeekToPreviousBookmark();
        MachineRequest SeekToNextBookmark();
    }
}
