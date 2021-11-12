namespace CPvC
{
    public interface ICompactableMachine
    {
        void Compact(IFileSystem fileSystem, bool enableDiffs);
        bool CanCompact();
    }
}
