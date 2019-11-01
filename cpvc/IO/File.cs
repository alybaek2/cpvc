using System;
using System.IO;

namespace CPvC
{
    /// <summary>
    /// Class for opening a file with exclusive access for appending.
    /// </summary>
    public class File : IFile, IDisposable
    {
        private FileStream _fileStream;
        private StreamWriter _streamWriter;

        public File(string filepath)
        {
            _fileStream = System.IO.File.Open(filepath, FileMode.Append, FileAccess.Write, FileShare.None);
            _streamWriter = new StreamWriter(_fileStream);
        }

        public void Dispose()
        {
            // Note that the _fileStream will also be disposed of automatically by _streamWriter being closed.
            if (_streamWriter != null)
            {
                _streamWriter.Close();
                _streamWriter = null;
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void WriteLine(string line)
        {
            _streamWriter.WriteLine(line);
        }
    }
}
