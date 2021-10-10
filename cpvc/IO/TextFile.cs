using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class TextFile : ITextFile, IDisposable
    {
        static private readonly byte[] _newline = { 0x0d, 0x0a };
        public IFileByteStream _byteStream;

        public TextFile(IFileByteStream byteStream)
        {
            _byteStream = byteStream;
        }

        public void WriteLine(string line)
        {
            _byteStream.Write(Encoding.UTF8.GetBytes(line));
            _byteStream.Write(_newline);
        }

        public string ReadLine()
        {
            long startPosition = _byteStream.Position;
            long endPosition = startPosition;
            long streamLength = _byteStream.Length;

            while (endPosition < streamLength)
            {
                byte b = _byteStream.ReadByte();
                endPosition++;
                if (b == 0x0a)
                {
                    break;
                }
            }

            long length = endPosition - startPosition;
            if (length == 0)
            {
                return null;
            }

            byte[] bytes = new byte[length];

            _byteStream.Position = startPosition;
            _byteStream.ReadBytes(bytes, (int)length);

            if (bytes[length - 1] == 0x0a)
            {
                length--;

                if (bytes[length - 1] == 0x0d)
                {
                    length--;
                }
            }

            return Encoding.UTF8.GetString(bytes, 0, (int)length);
        }

        public void Dispose()
        {
            Close();
        }

        public virtual void Close()
        {
            _byteStream?.Close();
            _byteStream = null;
        }
    }
}
