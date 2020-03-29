using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MemoryByteStream : IByteStream
    {
        private List<byte> _bytes;
        private int _pos;

        public MemoryByteStream()
        {
            _bytes = new List<byte>();
            _pos = 0;
        }

        public MemoryByteStream(byte[] bytes)
        {
            _bytes = bytes.ToList();
            _pos = 0;
        }

        public void WriteByte(byte b)
        {
            _bytes.Add(b);
            _pos++;
        }

        public void Write(byte[] b)
        {
            _bytes.AddRange(b);
            _pos += b.Length;
        }

        public int ReadByte()
        {
            if (_pos >= _bytes.Count)
            {
                return -1;
            }

            return _bytes[_pos++];
        }

        public int ReadBytes(byte[] array, int count)
        {
            int bytesRead = 0;
            while (_pos < _bytes.Count && bytesRead < count)
            {
                array[bytesRead] = _bytes[_pos];

                _pos++;
                bytesRead++;
            }

            return bytesRead;
        }

        public void Close()
        {

        }

        public long Length
        {
            get
            {
                return _bytes.Count;
            }
        }

        public long Position
        {
            get
            {
                return _pos;
            }

            set
            {
                _pos = (int)value;
            }
        }

        public void Clear()
        {
            _bytes.Clear();
            _pos = 0;
        }
    }
}
