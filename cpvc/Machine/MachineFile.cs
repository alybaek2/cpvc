﻿using System;

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
        public const byte _idVersion = 9;

        public MachineFile(IFileByteStream byteStream) : base(byteStream)
        {
        }

        public MachineFile(IFileSystem fileSystem, string filepath) : this(fileSystem.OpenFileByteStream(filepath))
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
                        case _idVersion:
                            ReadVersion(reader);
                            break;
                        default:
                            throw new Exception("Unknown block type!");
                    }
                }
            }
        }

        public void WriteDelete(HistoryEvent historyEvent)
        {
            WriteByte(_idDeleteEvent);
            WriteInt32(historyEvent.Id);
        }

        private void ReadName(IMachineFileReader reader)
        {
            string name = ReadString();

            reader.SetName(name);
        }

        public void WriteName(string name)
        {
            WriteByte(_idName);
            WriteString(name);
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
                    int version = ReadInt32();
                    IBlob stateBlob = ReadBlob();
                    IBlob screenBlob = ReadBlob();

                    bookmark = new Bookmark(system, version, stateBlob, screenBlob);
                }

                reader.SetBookmark(id, bookmark);
            }
        }

        public void WriteBookmark(int id, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                WriteByte(_idBookmark);
                WriteInt32(id);
                WriteBool(false);
            }
            else
            {
                WriteByte(_idBookmark);
                WriteInt32(id);
                WriteBool(true);
                WriteBool(bookmark.System);
                WriteInt32(bookmark.Version);
                WriteBytesBlob(bookmark.State.GetBytes());
                WriteCompressedBlob(bookmark.Screen.GetBytes());
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
            WriteByte(_idCurrent);
            WriteInt32(historyEvent.Id);
        }

        public void WriteCoreAction(int id, UInt64 ticks, CoreAction action)
        {
            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    WriteKey(id, ticks, action.KeyCode, action.KeyDown);
                    break;
                case CoreRequest.Types.Reset:
                    WriteReset(id, ticks);
                    break;
                case CoreRequest.Types.LoadDisc:
                    WriteLoadDisc(id, ticks, action.Drive, action.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.LoadTape:
                    WriteLoadTape(id, ticks, action.MediaBuffer.GetBytes());
                    break;
                case CoreRequest.Types.CoreVersion:
                    WriteVersion(id, ticks, action.Version);
                    break;
                default:
                    throw new Exception(String.Format("Unrecognized core action type {0}.", action.Type));
            }
        }

        private HistoryEvent ReadKey()
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

                return historyEvent;
            }
        }

        private void ReadKey(IMachineFileReader reader)
        {
            HistoryEvent historyEvent = ReadKey();

            reader.AddHistoryEvent(historyEvent);
        }

        private void WriteKey(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            // Since keyCode can only be a value from 0 to 79 (0x00 to 0x4F), we can use the most significant
            // bit to hold the "down" state of the key, instead of wasting a byte for that.
            byte keyCodeAndDown = (byte)((keyDown ? 0x80 : 0x00) | keyCode);

            WriteByte(_idKey);
            WriteInt32(id);
            WriteUInt64(ticks);
            WriteByte(keyCodeAndDown);
        }

        private HistoryEvent ReadReset()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                CoreAction action = CoreAction.Reset(ticks);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                return historyEvent;
            }
        }

        private void ReadReset(IMachineFileReader reader)
        {
            HistoryEvent historyEvent = ReadReset();

            reader.AddHistoryEvent(historyEvent);
        }

        private void WriteReset(int id, UInt64 ticks)
        {
            WriteByte(_idReset);
            WriteInt32(id);
            WriteUInt64(ticks);
        }

        private HistoryEvent ReadLoadDisc()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte drive = ReadByte();
                IBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                return historyEvent;
            }
        }

        private void ReadLoadDisc(IMachineFileReader reader)
        {
            HistoryEvent historyEvent = ReadLoadDisc();

            reader.AddHistoryEvent(historyEvent);
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            WriteByte(_idLoadDisc);
            WriteInt32(id);
            WriteUInt64(ticks);
            WriteByte(drive);
            WriteCompressedBlob(media);
        }

        private void ReadVersion(IMachineFileReader reader)
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                int version = ReadInt32();

                CoreAction action = CoreAction.CoreVersion(ticks, version);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteVersion(int id, UInt64 ticks, int version)
        {
            lock (_byteStream)
            {
                WriteByte(_idVersion);
                WriteInt32(id);
                WriteUInt64(ticks);
                WriteInt32(version);
            }
        }

        private HistoryEvent ReadLoadTape()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                IBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);
                HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, action);

                return historyEvent;
            }
        }

        private void ReadLoadTape(IMachineFileReader reader)
        {
            lock (_byteStream)
            {
                HistoryEvent historyEvent = ReadLoadTape();

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            WriteByte(_idLoadTape);
            WriteInt32(id);
            WriteUInt64(ticks);
            WriteCompressedBlob(media);
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
                    int version = ReadInt32();
                    IBlob stateBlob = ReadBlob();
                    IBlob screenBlob = ReadBlob();

                    bookmark = new Bookmark(system, version, stateBlob, screenBlob);
                }

                HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(id, ticks, created, bookmark);

                reader.AddHistoryEvent(historyEvent);
            }
        }

        private HistoryEvent GetParentBookmarkEvent(HistoryEvent historyEvent)
        {
            while (historyEvent != null && historyEvent.Bookmark == null)
            {
                historyEvent = historyEvent.Parent;
            }

            return historyEvent;
        }

        private void WriteCheckpoint(HistoryEvent historyEvent)
        {
            WriteByte(_idCheckpoint);
            WriteInt32(historyEvent.Id);
            WriteUInt64(historyEvent.Ticks);
            WriteUInt64((UInt64)Helpers.DateTimeToNumber(historyEvent.CreateDate));

            if (historyEvent.Bookmark == null)
            {
                WriteBool(false);
            }
            else
            {
                WriteBool(true);
                WriteBool(historyEvent.Bookmark.System);
                WriteInt32(historyEvent.Bookmark.Version);

                byte[] bookmarkBytes = historyEvent.Bookmark.State.GetBytes();

                HistoryEvent parentEvent = GetParentBookmarkEvent(historyEvent.Parent);
                Bookmark parentBookmark = parentEvent?.Bookmark;

                IStreamBlob baseBlob = parentBookmark?.State as IStreamBlob;

                // Write the state blob, then update the bookmark to use the returned FileBlob rather than the in-memory blob.
                IStreamBlob newBlob = WriteSmallestBlob(bookmarkBytes, baseBlob);
                historyEvent.Bookmark.State = newBlob;

                byte[] screen = historyEvent.Bookmark.Screen.GetBytes();

                IStreamBlob newBlob2 = WriteSmallestBlob(screen, parentBookmark?.Screen as IStreamBlob);
                historyEvent.Bookmark.Screen = newBlob2;
            }
        }

        private void ReadDelete(IMachineFileReader reader)
        {
            int id = ReadInt32();

            reader.DeleteEvent(id);
        }
    }
}
