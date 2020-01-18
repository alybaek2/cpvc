using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class BinaryFile : IBinaryFile
    {
        public IByteStream _byteStream;

        public BinaryFile(IByteStream byteStream)
        {
            _byteStream = byteStream;
        }

        public void Close()
        {
            _byteStream.Close();
        }

        public void WriteByte(byte b)
        {
            lock (_byteStream)
            {
                _byteStream.WriteByte(b);
            }
        }

        public void WriteBool(bool b)
        {
            lock (_byteStream)
            {
                _byteStream.WriteByte((byte)(b ? 1 : 0));
            }
        }

        public void WriteInt32(Int32 i)
        {
            lock (_byteStream)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                _byteStream.Write(bytes);
            }
        }

        public void WriteUInt64(UInt64 u)
        {
            lock (_byteStream)
            {
                byte[] bytes = BitConverter.GetBytes(u);
                _byteStream.Write(bytes);
            }
        }

        public void WriteVariableLengthByteArray(byte[] b)
        {
            lock (_byteStream)
            {
                if (b == null)
                {
                    WriteInt32(-1);
                }
                else
                {
                    WriteInt32(b.Length);
                    _byteStream.Write(b);
                }
            }
        }

        public void WriteString(string s)
        {
            WriteVariableLengthByteArray(Encoding.UTF8.GetBytes(s));
        }

        public byte ReadByte()
        {
            lock (_byteStream)
            {
                int b = _byteStream.ReadByte();
                if (b == -1)
                {
                    throw new Exception("Insufficient bytes to read byte!");
                }

                return (byte)b;
            }
        }

        public bool ReadBool()
        {
            return ReadByte() != 0;
        }

        public int ReadInt32()
        {
            return BitConverter.ToInt32(ReadFixedLengthByteArray(4), 0);
        }

        public UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadFixedLengthByteArray(8), 0);
        }

        public byte[] ReadVariableLengthByteArray()
        {
            int len = ReadInt32();
            return ReadFixedLengthByteArray(len);
        }

        public void SkipVariableLengthByteArray()
        {
            int len = ReadInt32();
            _byteStream.Position += len;
        }

        public string ReadString()
        {
            byte[] bytes = ReadVariableLengthByteArray();

            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] ReadFixedLengthByteArray(int count)
        {
            lock (_byteStream)
            {
                byte[] bytes = new byte[count];
                int bytesRead = _byteStream.ReadBytes(bytes, count);
                if (bytesRead < count)
                {
                    throw new Exception(String.Format("Insufficient bytes to read {0} bytes.", count));
                }

                return bytes;
            }
        }

        private class AutoPos : IDisposable
        {
            private BinaryFile _file;
            private long _originalPos;

            public AutoPos(BinaryFile file, int pos)
            {
                _file = file;
                _originalPos = _file._byteStream.Position;
                _file._byteStream.Position = pos;
            }

            public void Dispose()
            {
                _file._byteStream.Position = _originalPos;
            }
        }

        private AutoPos PushPos(int pos)
        {
            return new AutoPos(this, pos);
        }

        public class FileBlob2 : IStreamBlob
        {
            // 0x00 - null blob
            // 0x01 - bytes blob (offset, length)
            // 0x02 - diff blob (old offset, diff length, diff bytes)

            private BinaryFile _file;

            public int _pos;

            public FileBlob2(BinaryFile file, int pos)
            {
                _file = file;
                _pos = pos;
            }

            public byte[] GetBytes()
            {
                return _file.ReadBlobBytes(_pos);
            }

            public int Position
            {
                get
                {
                    return _pos;
                }
            }
        }

        public IStreamBlob WriteBytesBlob(byte[] bytes)
        {
            lock (_byteStream)
            {
                if (bytes == null)
                {
                    WriteByte((byte)0x00);

                    return null;
                }
                else
                {
                    int pos = (int)_byteStream.Position;

                    WriteByte((byte)0x01);
                    WriteVariableLengthByteArray(bytes);

                    return new FileBlob2(this, pos);
                }
            }
        }

        public IStreamBlob WriteDiffBlob(IStreamBlob oldBlob, byte[] newBytes)
        {
            lock (_byteStream)
            {
                long currentPos = _byteStream.Position;

                byte[] oldBytes = oldBlob.GetBytes();

                WriteByte((byte)0x02);
                WriteInt32(oldBlob.Position);

                byte[] diffBytes = Helpers.BinaryDiff(oldBytes, newBytes);

                WriteVariableLengthByteArray(diffBytes);

                return new FileBlob2(this, (int)currentPos);
            }
        }

        public IBlob ReadBlob()
        {
            FileBlob2 blob = new FileBlob2(this, (int)_byteStream.Position);

            SkipBlob();

            return blob;
        }

        private void SkipBlob()
        {
            lock (_byteStream)
            {
                byte type = ReadByte();
                switch (type)
                {
                    case 0x00:
                        break;
                    case 0x01:
                        SkipVariableLengthByteArray();
                        break;
                    case 0x02:
                        ReadInt32();
                        SkipVariableLengthByteArray();
                        break;
                    default:
                        throw new Exception("Unknown type!!!");
                }
            }
        }

        private byte[] ReadBlobBytes(int pos)
        {
            lock (_byteStream)
            {
                using (PushPos(pos))
                {
                    byte type = ReadByte();
                    switch (type)
                    {
                        case 0x00:
                            return null;
                        case 0x01:
                            return ReadVariableLengthByteArray();
                        case 0x02:
                            {
                                int oldPos = ReadInt32();

                                byte[] oldBytes = ReadBlobBytes(oldPos);
                                byte[] diffBytes = ReadVariableLengthByteArray();

                                return Helpers.BinaryUndiff(oldBytes, diffBytes);
                            }
                        default:
                            throw new Exception("Unknown type!!!");
                    }
                }
            }
        }
    }
}
