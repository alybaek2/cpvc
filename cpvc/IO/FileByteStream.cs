using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public sealed class FileByteStream : IFileByteStream
    {
        private System.IO.Stream _fileStream;

        public FileByteStream(System.IO.FileStream fileStream)
        {
            _fileStream = fileStream;
        }

        public void Dispose()
        {
            Close();
        }

        public long Length
        {
            get
            {
                return _fileStream.Length;
            }
        }

        public long Position
        {
            get
            {
                return _fileStream.Position;
            }

            set
            {
                _fileStream.Position = value;
            }
        }

        public void Write(byte b)
        {
            _fileStream.WriteByte(b);
        }

        public void Write(byte[] b)
        {
            _fileStream.Write(b, 0, b.Length);
        }

        public byte ReadByte()
        {
            int b = _fileStream.ReadByte();
            if (b == -1)
            {
                throw new Exception("Reached end of file!");
            }

            return (byte)b;
        }

        public int ReadBytes(byte[] array, int count)
        {
            return _fileStream.Read(array, 0, count);
        }

        public void Close()
        {
            _fileStream?.Close();
            _fileStream = null;
        }
    }
}
