using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public sealed class ByteStream : IFileByteStream
    {
        private System.IO.FileStream _fileStream;

        public ByteStream(System.IO.FileStream fileStream)
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

        public void Seek(long offset)
        {
            _fileStream.Position = offset;
        }

        public void WriteByte(byte b)
        {
            _fileStream.WriteByte(b);
        }

        public void Write(byte[] b)
        {
            _fileStream.Write(b, 0, b.Length);
        }

        public int ReadByte()
        {
            return _fileStream.ReadByte();
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
