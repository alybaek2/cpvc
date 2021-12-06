using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class TextFile : ITextFile, IDisposable
    {
        private StreamReader _streamReader;
        private StreamWriter _streamWriter;
        private Stream _fileStream;

        public TextFile(Stream byteStream)
        {
            _fileStream = byteStream;
            _streamReader = new StreamReader(_fileStream);
            _streamWriter = new StreamWriter(_fileStream);
        }

        public void WriteLine(string line)
        {
            _streamWriter.WriteLine(line);
            _streamWriter.Flush();
        }

        public string ReadLine()
        {
            return _streamReader.ReadLine();
        }

        public void Dispose()
        {
            Close();
        }

        public virtual void Close()
        {
            _streamWriter?.Close();
            _streamWriter = null;

            _streamReader?.Close();
            _streamReader = null;

            _fileStream?.Close();
            _fileStream = null;
        }
    }
}
