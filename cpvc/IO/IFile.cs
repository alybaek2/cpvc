namespace CPvC
{
    /// <summary>
    /// Interface for writing to machine file.
    /// </summary>
    public interface IFile
    {
        void WriteLine(string line);
        void Close();
    }
}
