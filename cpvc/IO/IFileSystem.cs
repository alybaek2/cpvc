using System.Collections.Generic;

namespace CPvC
{
    /// <summary>
    /// Common file operations.
    /// </summary>
    public interface IFileSystem
    {
        IFile OpenFile(string filepath);
        void RenameFile(string oldFilepath, string newFilepath);
        void ReplaceFile(string filepath, string newFilepath);
        void DeleteFile(string filename);
        string[] ReadLines(string filename);
        byte[] ReadBytes(string filename);

        List<string> GetZipFileEntryNames(string filename);
        byte[] GetZipFileEntry(string filename, string entryName);
    }
}
