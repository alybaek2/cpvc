using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineFile : BinaryFile
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

        public MachineFile(IByteStream byteStream) : base(byteStream)
        {
        }

        public void ReadFile(IMachineFileReader reader)
        {
            lock (_byteStream)
            {
                _byteStream.Position = 0;

                while (_byteStream.Position < _byteStream.Length)
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
            lock (_byteStream)
            {
                int id = ReadInt32();
                bool hasBookmark = ReadBool();

                Bookmark bookmark = null;
                if (hasBookmark)
                {
                    bool system = ReadBool();
                    IBlob stateBlob = ReadBlob();
                    IBlob screenBlob = ReadBlob();

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
                WriteBytesBlob(bookmark.State.GetBytes());
                WriteBytesBlob(bookmark.Screen.GetBytes());
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

        private void ReadKey(IMachineFileReader reader)
        {
            lock (_byteStream)
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
            lock (_byteStream)
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
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte drive = ReadByte();
                IBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Load disc (drive {2}, {3} byte tape image)", id, ticks, (drive == 0) ? "A:" : "B:", mediaBlob.GetBytes()?.Length ?? 0);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            Write(_idLoadDisc, id, ticks, drive);
            WriteBytesBlob(media);
        }

        private void ReadLoadTape(IMachineFileReader reader)
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                IBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                Diagnostics.Trace("{0} {1}: Load tape ({2} byte tape image)", id, ticks, mediaBlob.GetBytes()?.Length ?? 0);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            Write(_idLoadTape, id, ticks);
            WriteBytesBlob(media);
        }

        private void ReadCheckpoint(IMachineFileReader reader)
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                DateTime created = Helpers.NumberToDateTime((Int64)ReadUInt64());
                bool hasBookmark = ReadBool();

                Bookmark bookmark = null;
                if (hasBookmark)
                {
                    bool system = ReadBool();

                    IBlob stateBlob = ReadBlob();
                    IBlob compressedScreenBlob = ReadBlob();

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
                WriteBool(false);
            }
            else
            {
                Write(true, historyEvent.Bookmark.System);

                byte[] bookmark = historyEvent.Bookmark.State.GetBytes();

                HistoryEvent parentEvent = GetParentBookmarkEvent(historyEvent?.Parent);
                IBlob stateBlob = parentEvent?.Bookmark?.State;

                // Write the state blob, then update the bookmark to use the returned FileBlob rather than the in-memory blob.
                IStreamBlob newBlob = (stateBlob is IStreamBlob fileBlob) ? WriteDiffBlob(fileBlob, bookmark) : WriteBytesBlob(bookmark);
                historyEvent.Bookmark.State = newBlob;

                byte[] screen = historyEvent.Bookmark.Screen.GetBytes();
                if (screen == null)
                {
                    WriteBytesBlob(null);
                }
                else
                {
                    byte[] compressedScreen = Helpers.Compress(screen);

                    WriteBytesBlob(compressedScreen);
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
                        WriteBool(b);
                        break;
                    case byte b:
                        WriteByte(b);
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
                        WriteString(s);
                        break;
                    default:
                        throw new Exception(String.Format("Unknown argument type"));
                }
            }
        }
    }
}
