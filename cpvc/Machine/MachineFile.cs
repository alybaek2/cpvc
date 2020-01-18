using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineFile
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

        private IBinaryFile _binaryFile;

        public MachineFile(string filepath)
        {
            _binaryFile = new BinaryFile(filepath);
        }

        public MachineFile(IBinaryFile binaryFile)
        {
            _binaryFile = binaryFile;
        }

        public void Close()
        {
            _binaryFile.Close();
        }

        // Need a way to read a blob and know what kind of blob it is!
        //
        // 0x00: A null blob
        // 0x01: A physical blob... i.e. length followed by actual bytes.
        // 0x02: A diff blob... i.e. the position of the old blob followed by the diff blob.

        public MachineFileBlob WriteBlob(byte[] bytes)
        {
            lock (_binaryFile)
            {
                if (bytes == null)
                {
                    Write((byte)0x00);

                    return null;
                }
                else
                {
                    int pos = (int)_binaryFile.Position;

                    Write((byte)0x01);
                    WriteVariableLengthByteArray(bytes);

                    return new MachineFileBlob(this, pos);
                }
            }
        }

        public class MachineFileBlob : IBlob
        {
            // 0x00 - null blob
            // 0x01 - bytes blob (offset, length)
            // 0x02 - diff blob (old offset, diff length, diff bytes)

            private MachineFile _file;

            public int _pos;

            public MachineFileBlob(MachineFile file, int pos)
            {
                _file = file;
                _pos = pos;
            }

            static public MachineFileBlob WriteDiffBytes(MachineFile file, MachineFileBlob oldBlog, byte[] bytes)
            {
                lock (file._binaryFile)
                {
                    long currentPos = file._binaryFile.Position;


                    byte[] oldBytes = oldBlog.GetBytes();

                    file.Write((byte)0x02);
                    file.Write(oldBlog._pos);

                    byte[] diffBytes = Helpers.BinaryDiff(oldBytes, bytes);

                    file.WriteVariableLengthByteArray(diffBytes);

                    return new MachineFileBlob(file, (int)currentPos);
                }
            }

            public byte[] GetBytes()
            {
                return _file.ReadBlobBytes(_pos);
            }
        }

        private class AutoPos : IDisposable
        {
            private MachineFile _file;
            private long _originalPos;

            public AutoPos(MachineFile file, long pos)
            {
                _file = file;
                _originalPos = _file._binaryFile.Position;
                _file._binaryFile.Position = pos;
            }

            public void Dispose()
            {
                _file._binaryFile.Position = _originalPos;
            }
        }

        private AutoPos PushPos(long pos)
        {
            return new AutoPos(this, pos);
        }

        public byte[] ReadBlobBytes(int pos)
        {
            lock (_binaryFile)
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

        public void SkipBlob()
        {
            lock (_binaryFile)
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

        public void ReadFile(IMachineFileReader reader)
        {
            lock (_binaryFile)
            {
                _binaryFile.Position = 0;

                while (_binaryFile.Position < _binaryFile.Length)
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
                }
            }
        }

        public void WriteDelete(HistoryEvent historyEvent)
        {
            Write(_idDeleteEvent, historyEvent.Id);
        }

        private void ReadName(IMachineFileReader reader)
        {
            string name = ReadString();

            reader.SetName(name);
        }

        public void WriteName(string name)
        {
            Write(_idName, name);
        }

        public void ReadBookmark(IMachineFileReader reader)
        {
            lock (_binaryFile)
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
            if (bookmark == null)
            {
                Write(_idBookmark, id, false);
            }
            else
            {
                Write(_idBookmark, id, true, bookmark.System);
                WriteBlob(bookmark.State.GetBytes());
                WriteBlob(bookmark.Screen.GetBytes());
            }
        }

        public void WriteHistoryEvent(HistoryEvent historyEvent)
        {
            switch (historyEvent.Type)
            {
                case HistoryEvent.Types.Checkpoint:
                    WriteCheckpoint(historyEvent);
                    break;
                case HistoryEvent.Types.CoreAction:
                    WriteCoreAction(historyEvent.Id, historyEvent.Ticks, historyEvent.CoreAction);
                    break;
                default:
                    throw new Exception(String.Format("Unrecognized history event type {0}.", historyEvent.Type));
            }
        }

        private void ReadCurrent(IMachineFileReader reader)
        {
            int id = ReadInt32();

            reader.SetCurrentEvent(id);
        }

        public void WriteCurrent(HistoryEvent historyEvent)
        {
            Write(_idCurrent, historyEvent.Id);
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
                default:
                    throw new Exception(String.Format("Unrecognized core action type {0}.", action.Type));
            }
        }

        private byte[] ReadByteArray(long position)
        {
            lock (_binaryFile)
            {
                using (PushPos(position))
                {
                    return ReadVariableLengthByteArray();
                }
            }
        }

        private void ReadKey(IMachineFileReader reader)
        {
            lock (_binaryFile)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte keyCodeAndDown = ReadByte();
                byte keyCode = (byte)(keyCodeAndDown & 0x7F);
                bool keyDown = ((keyCodeAndDown & 0x80) != 0);

                CoreAction action = CoreAction.KeyPress(ticks, keyCode, keyDown);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Key {2} {3}", id, ticks, keyCode, keyDown?"down":"up");

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteKey(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            // Since keyCode can only be a value from 0 to 79 (0x00 to 0x4F), we can use the most significant
            // bit to hold the "down" state of the key, instead of wasting a byte for that.
            byte keyCodeAndDown = (byte) ((keyDown ? 0x80 : 0x00) | keyCode);
            Write(_idKey, id, ticks, keyCodeAndDown);
        }

        private void ReadReset(IMachineFileReader reader)
        {
            lock (_binaryFile)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                CoreAction action = CoreAction.Reset(ticks);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Reset", id, ticks);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteReset(int id, UInt64 ticks)
        {
            Write(_idReset, id, ticks);
        }

        private void ReadLoadDisc(IMachineFileReader reader)
        {
            lock (_binaryFile)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte drive = ReadByte();
                MachineFileBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Load disc (drive {2}, {3} byte tape image)", id, ticks, (drive == 0) ? "A:" : "B:", mediaBlob.GetBytes()?.Length ?? 0);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            Write(_idLoadDisc, id, ticks, drive);
            WriteBlob(media);
        }

        private void ReadLoadTape(IMachineFileReader reader)
        {
            lock (_binaryFile)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                MachineFileBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Load tape ({2} byte tape image)", id, ticks, mediaBlob.GetBytes()?.Length ?? 0);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            Write(_idLoadTape, id, ticks);
            WriteBlob(media);
        }

        private MachineFileBlob ReadBlob()
        {
            MachineFileBlob blob = new MachineFileBlob(this, (int)_binaryFile.Position);

            SkipBlob();

            return blob;
        }

        private void ReadCheckpoint(IMachineFileReader reader)
        {
            lock (_binaryFile)
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
                    MachineFileBlob compressedScreenBlob = ReadBlob();

                    byte[] compressedScreen = compressedScreenBlob.GetBytes();
                    byte[] screen = (compressedScreen != null) ? Helpers.Uncompress(compressedScreen) : null;
                    IBlob screenBlob = new MemoryBlob(screen);


                    bookmark = new Bookmark(system, stateBlob, screenBlob);
                }

                HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(id, ticks, created, bookmark);

                if (bookmark == null)
                {
                    Diagnostics.Trace("{0} {1}: Checkpoint (no bookmark)", id, ticks);
                }
                else
                {
                    Diagnostics.Trace("{0} {1}: Checkpoint (with {2} bookmark)", id, ticks, bookmark.System ? "system" : "user");
                }

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private HistoryEvent GetParentBookmarkEvent(HistoryEvent historyEvent)
        {
            while (historyEvent != null && historyEvent?.Bookmark == null)
            {
                historyEvent = historyEvent.Parent;
            }

            return historyEvent;
        }

        private void WriteCheckpoint(HistoryEvent historyEvent)
        {
            Write(_idCheckpoint, historyEvent.Id, historyEvent.Ticks, (UInt64)Helpers.DateTimeToNumber(historyEvent.CreateDate));

            if (historyEvent.Bookmark == null)
            {
                Write(false);
            }
            else
            {
                Write(true, historyEvent.Bookmark.System);

                byte[] bookmark = historyEvent.Bookmark.State.GetBytes();

                HistoryEvent parentEvent = GetParentBookmarkEvent(historyEvent?.Parent);
                IBlob stateBlob = parentEvent?.Bookmark?.State;
                MachineFileBlob mfb = stateBlob as MachineFileBlob;

                if (stateBlob == null || mfb == null)
                {
                    MachineFileBlob blob = WriteBlob(bookmark);
                    historyEvent.Bookmark.State = blob;
                }
                else
                {
                    MachineFileBlob newBlob = MachineFileBlob.WriteDiffBytes(this, mfb, bookmark);
                    historyEvent.Bookmark.State = newBlob;
                }

                byte[] screen = historyEvent.Bookmark.Screen.GetBytes();
                if (screen == null)
                {
                    WriteBlob(null);
                }
                else
                {
                    byte[] compressedScreen = Helpers.Compress(screen);

                    WriteBlob(compressedScreen);
                }
            }
        }

        private void ReadDelete(IMachineFileReader reader)
        {
            int id = ReadInt32();

            Diagnostics.Trace("{0}: Delete", id);

            reader.DeleteEvent(id);
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
                        WriteInt32(i);
                        break;
                    case UInt64 u:
                        WriteUInt64(u);
                        break;
                    case Byte[] b:
                        WriteVariableLengthByteArray(b);
                        break;
                    case string s:
                        Write(s);
                        break;
                    case null:
                        Write((int)-1);
                        break;
                    default:
                        throw new Exception(String.Format("Unknown argument"));
                }
            }
        }

        private void Write(byte b)
        {
            lock (_binaryFile)
            {
                _binaryFile.WriteByte(b);
            }
        }

        private void Write(bool b)
        {
            lock (_binaryFile)
            {
                _binaryFile.WriteByte((byte)(b ? 1 : 0));
            }
        }

        private void WriteInt32(Int32 i)
        {
            lock (_binaryFile)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                _binaryFile.Write(bytes);
            }
        }

        private void WriteUInt64(UInt64 u)
        {
            lock (_binaryFile)
            {
                byte[] bytes = BitConverter.GetBytes(u);
                _binaryFile.Write(bytes);
            }
        }

        private void WriteVariableLengthByteArray(byte[] b)
        {
            lock (_binaryFile)
            {
                if (b == null)
                {
                    Write((int)-1);
                }
                else
                {
                    Write(b.Length);
                    _binaryFile.Write(b);
                }
            }
        }

        private void Write(string s)
        {
            WriteVariableLengthByteArray(Encoding.UTF8.GetBytes(s));
        }

        private byte ReadByte()
        {
            lock (_binaryFile)
            {
                int b = _binaryFile.ReadByte();
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
            return BitConverter.ToInt32(ReadFixedLengthByteArray(4), 0);
        }

        private UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadFixedLengthByteArray(8), 0);
        }

        private byte[] ReadVariableLengthByteArray()
        {
            int len = ReadInt32();
            return ReadFixedLengthByteArray(len);
        }

        private void SkipVariableLengthByteArray()
        {
            int len = ReadInt32();
            _binaryFile.Position += len;
        }

        private string ReadString()
        {
            byte[] bytes = ReadVariableLengthByteArray();

            return Encoding.UTF8.GetString(bytes);
        }

        private byte[] ReadFixedLengthByteArray(int count)
        {
            lock (_binaryFile)
            {
                byte[] bytes = new byte[count];
                int bytesRead = _binaryFile.ReadBytes(bytes, count);
                if (bytesRead < count)
                {
                    throw new Exception(String.Format("Insufficient bytes to read {0} bytes.", count));
                }

                return bytes;
            }
        }
    }
}
