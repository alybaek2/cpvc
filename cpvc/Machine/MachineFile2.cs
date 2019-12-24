using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineFile2
    {
        // Using bytes here limits us to 256 possible block types.... could reserve most significant bit and do some kind of variable length encoding if we really need to.
        public const byte _idName = 0;
        public const byte _idKey = 1;
        public const byte _idReset = 2;
        public const byte _idLoadDisc = 3;
        public const byte _idLoadTape = 4;
        public const byte _idCheckpoint = 5;
        public const byte _idDeleteEvent = 6;
        public const byte _idCurrent = 7;
        public const byte _idBookmark = 8;


        private System.IO.FileStream _fileStream;


        public MachineFile2(string filepath)
        {
            _fileStream = System.IO.File.Open(filepath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, System.IO.FileShare.None);
        }

        public void Close()
        {
            _fileStream.Close();
        }

        public class MachineFileBlob : IBlob
        {
            private MachineFile2 _file;
            private long _pos;

            public MachineFileBlob(MachineFile2 file, long pos)
            {
                _file = file;
                _pos = pos;
            }

            public byte[] GetBytes()
            {
                return _file.ReadBytes(_pos);
            }
        }

        public void ReadFile(IMachineFileReader reader)
        {
            lock (_fileStream)
            {
                _fileStream.Position = 0;

                while (_fileStream.Position < _fileStream.Length)
                {
                    byte blockType = ReadByte();

                    switch (blockType)
                    {
                        case _idName:
                            ReadName(reader);
                            break;
                        case _idCurrent:
                            ReadCurrent(reader);
                            break;
                        case _idBookmark:
                            ReadBookmark(reader);
                            break;
                        case _idDeleteEvent:
                            ReadDelete(reader);
                            break;
                        case _idCheckpoint:
                            ReadCheckpoint(reader);
                            break;
                        case _idKey:
                            ReadKey(reader);
                            break;
                        case _idReset:
                            ReadReset(reader);
                            break;
                        case _idLoadDisc:
                            ReadLoadDisc(reader);
                            break;
                        case _idLoadTape:
                            ReadLoadTape(reader);
                            break;
                        default:
                            break;
                    }

                    //_fileStream.Position += (offset - 4);
                }
            }
        }

        public void WriteDelete(HistoryEvent historyEvent)
        {
            WriteBlock(() =>
            {
                Write(_idDeleteEvent, historyEvent.Id);
            });
        }

        private void ReadName(IMachineFileReader reader)
        {
            string name = ReadString();

            reader.SetName(name);
        }

        public void WriteName(string name)
        {
            WriteBlock(() =>
            {
                Write(_idName, name);
            });
        }

        public void ReadBookmark(IMachineFileReader reader)
        {
            lock (_fileStream)
            {
                int id = ReadInt32();
                bool hasBookmark = ReadBool();

                Bookmark bookmark = null;
                if (hasBookmark)
                {
                    bool system = ReadBool();
                    MachineFileBlob stateBlob = ReadBlob();
                    MachineFileBlob screenBlob = ReadBlob();

                    bookmark = new Bookmark(system, stateBlob, screenBlob);
                }

                reader.SetBookmark(id, bookmark);
            }
        }

        public void WriteBookmark(int id, Bookmark bookmark)
        {
            WriteBlock(() =>
            {
                if (bookmark == null)
                {
                    Write(_idBookmark, id, false);
                }
                else
                {
                    Write(_idBookmark, id, true, bookmark.System, bookmark.State, bookmark.Screen);
                }
            });
        }

        public void WriteHistoryEvent(HistoryEvent historyEvent)
        {
            WriteBlock(() =>
            {
                switch (historyEvent.Type)
                {
                    case HistoryEvent.Types.Checkpoint:
                        WriteCheckpoint(historyEvent.Id, historyEvent.Ticks, historyEvent.CreateDate, historyEvent.Bookmark);
                        break;
                    case HistoryEvent.Types.CoreAction:
                        WriteCoreAction(historyEvent.Id, historyEvent.Ticks, historyEvent.CoreAction);
                        break;
                }
            });
        }

        public void WriteBlock(Action writer)
        {
            writer();
        }

        private void ReadCurrent(IMachineFileReader reader)
        {
            int id = ReadInt32();

            reader.SetCurrentEvent(id);
        }

        public void WriteCurrent(HistoryEvent historyEvent)
        {
            WriteBlock(() =>
            {
                Write(_idCurrent, historyEvent.Id);
            });
        }

        private void WriteCoreAction(int id, UInt64 ticks, CoreAction action)
        {
            switch (action.Type)
            {
                case CoreActionBase.Types.KeyPress:
                    WriteKey(id, ticks, action.KeyCode, action.KeyDown);
                    break;
                case CoreActionBase.Types.Reset:
                    WriteReset(id, ticks);
                    break;
                case CoreActionBase.Types.LoadDisc:
                    WriteLoadDisc(id, ticks, action.Drive, action.MediaBuffer.GetBytes());
                    break;
                case CoreActionBase.Types.LoadTape:
                    WriteLoadTape(id, ticks, action.MediaBuffer.GetBytes());
                    break;
            }
        }

        private byte[] ReadBytes(long position)
        {
            lock (_fileStream)
            {
                long currentPos = _fileStream.Position;

                _fileStream.Position = position;

                byte[] bytes = ReadByteArray();

                _fileStream.Position = currentPos;

                return bytes;
            }
        }

        private void ReadKey(IMachineFileReader reader)
        {
            lock (_fileStream)
            {

                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte keyCode = ReadByte();
                bool keyDown = ReadBool();

                CoreAction action = CoreAction.KeyPress(ticks, keyCode, keyDown);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteKey(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            Write(_idKey, id, ticks, keyCode, keyDown);
        }

        private void ReadReset(IMachineFileReader reader)
        {
            lock (_fileStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                CoreAction action = CoreAction.Reset(ticks);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteReset(int id, UInt64 ticks)
        {
            Write(_idReset, id, ticks);
        }

        private void ReadLoadDisc(IMachineFileReader reader)
        {
            lock (_fileStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte drive = ReadByte();
                MachineFileBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            Write(_idLoadDisc, id, ticks, drive, media);
        }

        private void ReadLoadTape(IMachineFileReader reader)
        {
            lock (_fileStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                MachineFileBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            Write(_idLoadTape, id, ticks, media);
        }

        private void ReadCheckpoint(IMachineFileReader reader)
        {
            lock (_fileStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                DateTime created = Helpers.NumberToDateTime((Int64)ReadUInt64());
                bool hasBookmark = ReadBool();

                Bookmark bookmark = null;
                if (hasBookmark)
                {
                    bool system = ReadBool();
                    MachineFileBlob stateBlob = ReadBlob();
                    MachineFileBlob screenBlob = ReadBlob();

                    bookmark = new Bookmark(system, stateBlob, screenBlob);
                }

                HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(id, ticks, created, bookmark);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteCheckpoint(int id, UInt64 ticks, DateTime created, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                Write(_idCheckpoint, id, ticks, (UInt64)Helpers.DateTimeToNumber(created), false);
            }
            else
            {
                Write(_idCheckpoint, id, ticks, (UInt64)Helpers.DateTimeToNumber(created), true, bookmark.System, bookmark.State, bookmark.Screen);
            }
        }

        private void ReadDelete(IMachineFileReader reader)
        {
            int id = ReadInt32();

            reader.DeleteEvent(id);
        }

        private void WriteDelete(int id)
        {
            Write(_idDeleteEvent, id);
        }

        private void Write(params object[] args)
        {
            foreach (object arg in args)
            {
                switch (arg)
                {
                    case bool b:
                        Write(b);
                        break;
                    case byte b:
                        Write(b);
                        break;
                    case Int32 i:
                        Write(i);
                        break;
                    case UInt64 u:
                        Write(u);
                        break;
                    case Byte[] b:
                        Write(b);
                        break;
                    case string s:
                        Write(s);
                        break;
                    case IBlob b:
                        Write(b.GetBytes());
                        break;
                    default:
                        throw new Exception(String.Format("Unknown argument"));
                }
            }
        }

        private void Write(byte b)
        {
            lock (_fileStream)
            {
                _fileStream.WriteByte(b);
            }
        }

        private void Write(bool b)
        {
            lock (_fileStream)
            {
                _fileStream.WriteByte((byte)(b ? 1 : 0));
            }
        }

        private void Write(Int32 i)
        {
            lock (_fileStream)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                _fileStream.Write(bytes, 0, 4);
            }
        }

        private void Write(UInt64 u)
        {
            lock (_fileStream)
            {
                byte[] bytes = BitConverter.GetBytes(u);
                _fileStream.Write(bytes, 0, 8);
            }
        }

        private void Write(byte[] b)
        {
            lock (_fileStream)
            {
                Write(b.Length);
                _fileStream.Write(b, 0, b.Length);
            }
        }

        private void Write(string s)
        {
            Write(Encoding.UTF8.GetBytes(s));
        }

        private byte ReadByte()
        {
            lock (_fileStream)
            {
                int b = _fileStream.ReadByte();
                if (b == -1)
                {
                    throw new Exception("Insufficient bytes to read byte!");
                }

                return (byte)b;
            }
        }

        private bool ReadBool()
        {
            return ReadByte() != 0;
        }

        private int ReadInt32()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }

        private UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBytes(8), 0);
        }

        private byte[] ReadByteArray()
        {
            int len = ReadInt32();
            return ReadBytes(len);
        }

        private string ReadString()
        {
            byte[] bytes = ReadByteArray();

            return Encoding.UTF8.GetString(bytes);
        }

        private MachineFileBlob ReadBlob()
        {
            long pos = _fileStream.Position;
            int len = ReadInt32();
            _fileStream.Seek(len, System.IO.SeekOrigin.Current);

            return new MachineFileBlob(this, pos);
        }

        private byte[] ReadBytes(int count)
        {
            lock (_fileStream)
            {
                byte[] bytes = new byte[count];
                int bytesRead = _fileStream.Read(bytes, 0, count);
                if (bytesRead < count)
                {
                    throw new Exception(String.Format("Insufficient bytes to read {0} bytes.", count));
                }

                return bytes;
            }
        }
    }
}
