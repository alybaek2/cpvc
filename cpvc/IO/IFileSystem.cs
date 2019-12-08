using System;
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
        void DeleteFile(string filepath);
        IEnumerable<string> ReadLines(string filepath);
        IEnumerable<string> ReadLinesReverse(string filepath);
        byte[] ReadBytes(string filepath);
        bool Exists(string filepath);
        Int64 FileLength(string filepath);

        List<string> GetZipFileEntryNames(string filepath);
        byte[] GetZipFileEntry(string filepath, string entryName);
    }
}
