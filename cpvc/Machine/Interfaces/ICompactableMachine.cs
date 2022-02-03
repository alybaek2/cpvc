namespace CPvC
{
    public interface ICompactableMachine
    {
        void Compact(IFileSystem fileSystem);
        bool CanCompact { get; }
    }
}
