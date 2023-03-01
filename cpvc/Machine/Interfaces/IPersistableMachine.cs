namespace CPvC
{
    public interface IPersistableMachine
    {
        bool Persist(IFileSystem fileSystem, string filepath);
        void OpenFromFile(IFileSystem fileSystem);
        string PersistentFilepath { get; }
        bool IsOpen { get; }
    }
}
