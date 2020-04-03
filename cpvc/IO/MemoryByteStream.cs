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

        public byte[] AsBytes()
        {
            return _bytes.ToArray();
        }

        public void Write(byte b)
        {
            _bytes.Add(b);
            _pos++;
        }

        public void Write(bool b)
        {
            Write((byte) (b ? 0xFF : 0x00));
        }

        public bool ReadBool()
        {
            int b = ReadByte();
            if (b == -1)
            {
                throw new Exception("Unexpected end of stream.");
            }

            return (b == 0xFF);
        }

        public void Write(UInt16 u)
        {
            Write(BitConverter.GetBytes(u));
        }

        public UInt16 ReadUInt16()
        {
            UInt16 u = _bytes[_pos];
            _pos++;
            u += (UInt16)(0x100 * _bytes[_pos]);
            _pos++;

            return u;
        }

        public void Write(int i)
        {
            Write(BitConverter.GetBytes(i));
        }

        public Int32 ReadInt32()
        {
            return (Int32)ReadUInt32();
        }

        public void Write(UInt32 u)
        {
            Write(BitConverter.GetBytes(u));
        }

        public UInt32 ReadUInt32()
        {
            UInt32 u = _bytes[_pos];
            _pos++;
            u += (UInt32)(0x100 * _bytes[_pos]);
            _pos++;
            u += (UInt32)(0x10000 * _bytes[_pos]);
            _pos++;
            u += (UInt32)(0x1000000 * _bytes[_pos]);
            _pos++;

            return u;
        }

        public void Write(UInt64 u)
        {
            Write(BitConverter.GetBytes(u));
        }

        public UInt64 ReadUInt64()
        {
            UInt64 u = ReadUInt32();
            u += (UInt64)(0x100000000 * (UInt64)ReadUInt32());

            return u;
        }

        public void WriteArray(byte[] bytes)
        {
            Write(bytes.Length);
            Write(bytes);
        }

        public byte[] ReadArray()
        {
            Int32 len = ReadInt32();
            byte[] bytes = new byte[len];
            int bytesRead = ReadBytes(bytes, len);

            if (bytesRead != len)
            {
                throw new Exception("Unexpected end of stream.");
            }

            return bytes;
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

        public byte ReadOneByte()
        {
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

        public void Write(string str)
        {
            WriteArray(Encoding.UTF8.GetBytes(str));
        }

        public string ReadString()
        {
            return Encoding.UTF8.GetString(ReadArray());
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
