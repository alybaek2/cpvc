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

        public MachineFileBlob2 WriteBlob(byte[] bytes)
        {
            if (bytes == null)
            {
                Write((byte)0x00);

                return null;
            }
            else
            {
                int pos = (int) _binaryFile.Position;

                Write((byte)0x01);

                Write(bytes);

                return new MachineFileBlob2(this, pos);
            }
        }

        public class MachineFileBlob2 : IBlob
        {
            // 0x00 - null blob
            // 0x01 - bytes blob (offset, length)
            // 0x02 - diff blob (old offset, diff length, diff bytes)
            //private byte _type;

            private MachineFile _file;

            public int _pos;

            public MachineFileBlob2(MachineFile file, int pos)
            {
                _file = file;
                _pos = pos;
            }

            public int Length()
            {
                lock (_file._binaryFile)
                {
                    long currentPos = _file._binaryFile.Position;

                    _file._binaryFile.Position = _pos;

                    byte type = _file.ReadByte();

                    switch (type)
                    {
                        case 0x00:
                            return 1;
                        case 0x01:
                            {
                                int bytesLen = _file.ReadInt32();
                                int len = 1 + 4 + bytesLen;

                                _file._binaryFile.Position = currentPos;

                                return len;
                            }
                        case 0x02:
                            {
                                _file._binaryFile.Position += 4;

                                int diffLen = _file.ReadInt32();

                                int len = 1 + 4 + 4 + diffLen;

                                _file._binaryFile.Position = currentPos;

                                return len;
                            }
                        default:
                            throw new Exception("Unknwon type!!!!!!");
                    }
                }
            }


            static public MachineFileBlob2 WriteBytes(MachineFile file, byte[] bytes)
            {
                lock (file._binaryFile)
                {
                    long currentPos = file._binaryFile.Position;

                    if (bytes == null)
                    {
                        file.Write((byte)0x00);
                    }
                    else
                    {
                        file.Write((byte)0x01);
                        file.Write(bytes.Length);
                        file._binaryFile.Write(bytes);
                    }

                    return new MachineFileBlob2(file, (int)currentPos);
                }
            }

            static public MachineFileBlob2 WriteDiffBytes(MachineFile file, MachineFileBlob2 oldBlog, byte[] bytes)
            {
                lock (file._binaryFile)
                {
                    long currentPos = file._binaryFile.Position;

                    file.Write((byte)0x01);
                    file.Write(bytes.Length);
                    file._binaryFile.Write(bytes);

                    return new MachineFileBlob2(file, (int)currentPos);
                }
            }

            public byte[] GetBytes()
            {
                lock (_file._binaryFile)
                {
                    long currentPos = _file._binaryFile.Position;

                    _file._binaryFile.Position = _pos;


                    byte[] bytes = null;

                    byte type = _file.ReadByte();
                    switch (type)
                    {
                        case 0x00:
                            bytes = null;
                            break;
                        case 0x01:
                            {
                                int len = _file.ReadInt32();
                                bytes = _file.ReadBytes(len);
                            }
                            break;
                        case 0x02:
                            {
                                int oldPos = _file.ReadInt32();

                                MachineFileBlob2 blob2 = new MachineFileBlob2(_file, oldPos);
                                byte[] oldBytes = blob2.GetBytes();

                                int len = _file.ReadInt32();
                                byte[] diffBytes = _file.ReadBytes(len);

                                bytes = Helpers.BinaryUndiff(oldBytes, diffBytes);
                            }
                            break;
                        default:
                            throw new Exception("Unknown type!!!");
                    }

                    _file._binaryFile.Position = currentPos;

                    return bytes;
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
                    MachineFileBlob2 stateBlob = ReadBlob2();
                    MachineFileBlob2 screenBlob = ReadBlob2();

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
                MachineFileBlob2.WriteBytes(this, bookmark.State.GetBytes());
                MachineFileBlob2.WriteBytes(this, bookmark.Screen.GetBytes());
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
                long currentPos = _binaryFile.Position;
                byte[] bytes = null;

                try
                {
                    _binaryFile.Position = position;

                    bytes = ReadByteArray();
                }
                finally
                {
                    _binaryFile.Position = currentPos;
                }

                return bytes;
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
                MachineFileBlob2 mediaBlob = ReadBlob2();

                CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Load disc (drive {2}, {3} byte tape image)", id, ticks, (drive == 0) ? "A:" : "B:", mediaBlob.GetBytes()?.Length ?? 0);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            Write(_idLoadDisc, id, ticks, drive);
            MachineFileBlob2.WriteBytes(this, media);
        }

        private void ReadLoadTape(IMachineFileReader reader)
        {
            lock (_binaryFile)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                MachineFileBlob2 mediaBlob = ReadBlob2();

                CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Load tape ({2} byte tape image)", id, ticks, mediaBlob.GetBytes()?.Length ?? 0);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            Write(_idLoadTape, id, ticks);
            MachineFileBlob2.WriteBytes(this, media);
        }

        private MachineFileBlob2 ReadBlob2()
        {
            MachineFileBlob2 blob = new MachineFileBlob2(this, (int)_binaryFile.Position);

            _binaryFile.Position += blob.Length();

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

                    MachineFileBlob2 stateBlob = ReadBlob2();
                    MachineFileBlob2 compressedScreenBlob = ReadBlob2();


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
            if (historyEvent.Bookmark == null)
            {
                Write(_idCheckpoint, historyEvent.Id, historyEvent.Ticks, (UInt64)Helpers.DateTimeToNumber(historyEvent.CreateDate), false);
            }
            else
            {
                Write(_idCheckpoint, historyEvent.Id, historyEvent.Ticks, (UInt64)Helpers.DateTimeToNumber(historyEvent.CreateDate), true, historyEvent.Bookmark.System);

                byte[] bookmark = historyEvent.Bookmark.State.GetBytes();

                HistoryEvent parentEvent = GetParentBookmarkEvent(historyEvent?.Parent);
                IBlob stateBlob = parentEvent?.Bookmark?.State;
                MachineFileBlob2 mfb = stateBlob as MachineFileBlob2;

                if (stateBlob == null || mfb == null)
                {
                    MachineFileBlob2 blob = MachineFileBlob2.WriteBytes(this, bookmark);
                    historyEvent.Bookmark.State = blob;
                }
                else
                {
                    MachineFileBlob2 newBlob = MachineFileBlob2.WriteDiffBytes(this, mfb, bookmark);
                    historyEvent.Bookmark.State = newBlob;
                }


                byte[] screen = historyEvent.Bookmark.Screen.GetBytes();
                if (screen == null)
                {
                    MachineFileBlob2.WriteBytes(this, null);
                }
                else
                {
                    byte[] compressedScreen = Helpers.Compress(screen);

                    MachineFileBlob2.WriteBytes(this, compressedScreen);
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

        private void Write(Int32 i)
        {
            lock (_binaryFile)
            {
                byte[] bytes = BitConverter.GetBytes(i);
                _binaryFile.Write(bytes);
            }
        }

        private void Write(UInt64 u)
        {
            lock (_binaryFile)
            {
                byte[] bytes = BitConverter.GetBytes(u);
                _binaryFile.Write(bytes);
            }
        }

        private MachineFileBlob2 Write(byte[] b)
        {
            lock (_binaryFile)
            {
                if (b == null)
                {
                    Write((int)-1);

                    return null;
                }
                else
                {
                    MachineFileBlob2 blob = new MachineFileBlob2(this, (int) (_binaryFile.Position - 1));

                    Write(b.Length);
                    _binaryFile.Write(b);

                    return blob;
                }
            }
        }

        private void Write(string s)
        {
            Write(Encoding.UTF8.GetBytes(s));
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
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }

        private UInt64 ReadUInt64()
        {
            return BitConverter.ToUInt64(ReadBytes(8), 0);
        }

        private byte[] ReadByteArray()
        {
            int len = ReadInt32();
            if (len == -1)
            {
                return null;
            }
            else
            {
                return ReadBytes(len);
            }
        }

        private string ReadString()
        {
            byte[] bytes = ReadByteArray();

            return Encoding.UTF8.GetString(bytes);
        }

        private IBlob ReadBlob()
        {
            long pos = _binaryFile.Position;
            int len = ReadInt32();
            if (len == -1)
            {
                return new MemoryBlob(null);
            }

            _binaryFile.Position += len;

            return new MachineFileBlob2(this, (int) (pos - 1));
        }

        private byte[] ReadBytes(int count)
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

        public byte[] ReadEntireFile(bool upToCurrentPos)
        {
            lock (_binaryFile)
            {
                long currentPos = _binaryFile.Position;
                byte[] bytes = null;

                try
                {
                    _binaryFile.Position = 0;

                    if (upToCurrentPos)
                    {
                        bytes = ReadBytes((int)currentPos);
                    }
                    else
                    {
                        bytes = ReadBytes((int)_binaryFile.Length);
                    }
                }
                finally
                {
                    _binaryFile.Position = currentPos;
                }

                return bytes;
            }
        }
    }
}
