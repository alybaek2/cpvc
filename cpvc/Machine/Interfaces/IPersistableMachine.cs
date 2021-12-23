namespace CPvC
{
    public interface IPersistableMachine
    {
        bool Persist(IFileSystem fileSystem, string filepath);
        void OpenFromFile(IFileSystem fileSystem);
        string PersistantFilepath { get; }
        bool IsOpen { get; }
    }
}
