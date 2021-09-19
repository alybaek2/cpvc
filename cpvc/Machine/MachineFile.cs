using System;
using System.Collections.Generic;

namespace CPvC
{
    public class MachineFile : BinaryFile
    {
        // Using bytes here limits us to 256 possible block types.... could reserve most significant bit and do some kind of variable length encoding if we really need to.
        private const byte _idName = 0;
        private const byte _idKey = 1;
        private const byte _idReset = 2;
        private const byte _idLoadDisc = 3;
        private const byte _idLoadTape = 4;
        private const byte _idDeleteEventAndChildren = 7;
        private const byte _idCurrent = 8;
        private const byte _idAddBookmark = 9;
        private const byte _idVersion = 10;
        private const byte _idRunUntil = 11;
        private const byte _idSetCurrentToRoot = 12;
        private const byte _idDeleteEvent = 13;

        private Dictionary<HistoryEvent, int> _historyEventToId;
        private Dictionary<int, HistoryEvent> _idToHistoryEvent;
        private MachineHistory _machineHistory;
        private int _nextPersistentId;

        private Machine _machine;

        static private Dictionary<string, Machine> _machines = new Dictionary<string, Machine>();

        public Machine Machine
        {
            set
            {
                if (_machine != null)
                {
                    _machine.PropertyChanged -= Machine_PropertyChanged;
                }

                _machine = value;

                if (_machine != null)
                {
                    _machine.PropertyChanged += Machine_PropertyChanged;
                }
            }
        }

        public MachineHistory History
        {
            get
            {
                return _machineHistory;
            }

            set
            {
                if (_machineHistory != null)
                {
                    _machineHistory.Auditors -= HistoryEventHappened;
                }

                _machineHistory = value;

                if (_machineHistory != null)
                {
                    _machineHistory.Auditors += HistoryEventHappened;
                }
            }
        }

        public override void Close()
        {
            Machine = null;
            History = null;

            base.Close();
        }

        public MachineFile(IFileByteStream byteStream) : base(byteStream)
        {
            _historyEventToId = new Dictionary<HistoryEvent, int>();
            _idToHistoryEvent = new Dictionary<int, HistoryEvent>();
            _nextPersistentId = 0;
        }

        public MachineFile(IFileSystem fileSystem, string filepath) : this(fileSystem.OpenFileByteStream(filepath))
        {
        }

        private void HistoryEventHappened(HistoryEvent historyEvent, UInt64 ticks, HistoryEventType type, CoreAction coreAction, Bookmark bookmark)
        {
            switch (type)
            {
                case HistoryEventType.AddBookmark:
                    {
                        int id = _nextPersistentId++;
                        WriteAddBookmark(id, ticks, historyEvent.Bookmark);

                        _historyEventToId[historyEvent] = id;
                        _idToHistoryEvent[id] = historyEvent;

                    }
                    break;
                case HistoryEventType.AddCoreAction:
                    {
                        int id = _nextPersistentId++;
                        WriteCoreAction(id, ticks, historyEvent.CoreAction);

                        _historyEventToId[historyEvent] = id;
                        _idToHistoryEvent[id] = historyEvent;
                    }
                    break;
                case HistoryEventType.DeleteEventAndChildren:
                    {
                        if (_historyEventToId.TryGetValue(historyEvent, out int persistentId))
                        {
                            WriteByte(_idDeleteEventAndChildren);
                            WriteInt32(persistentId);
                        }
                        else
                        {
                            throw new Exception("Can't find id!");
                        }
                    }
                    break;
                case HistoryEventType.DeleteEvent:
                    {
                        if (_historyEventToId.TryGetValue(historyEvent, out int persistentId))
                        {
                            WriteByte(_idDeleteEvent);
                            WriteInt32(persistentId);
                        }
                        else
                        {
                            throw new Exception("Can't find id!");
                        }
                    }
                    break;
                case HistoryEventType.SetCurrent:
                    {
                        if (historyEvent == _machineHistory.RootEvent)
                        {
                            WriteByte(_idSetCurrentToRoot);
                        }
                        else if (_historyEventToId.TryGetValue(historyEvent, out int persistentId))
                        {
                            WriteByte(_idCurrent);
                            WriteInt32(persistentId);
                        }
                        else
                        {
                            throw new Exception("Can't find id!");
                        }
                    }
                    break;
                default:
                    throw new Exception("Unknown history type!");
            }
        }

        public void ReadFile(out string name, out MachineHistory history)
        {
            name = null;
            history = new MachineHistory();
            _machineHistory = history;

            lock (_byteStream)
            {
                // Should probably clear history first...
                if (_machine != null)
                {
                    _machine.PropertyChanged -= Machine_PropertyChanged;
                }

                if (_machineHistory != null)
                {
                    _machineHistory.Auditors -= HistoryEventHappened;
                }

                CPvC.Diagnostics.Trace("Read File STARTING!!!");

                _byteStream.Position = 0;

                while (_byteStream.Position < _byteStream.Length)
                {
                    byte blockType = ReadByte();

                    switch (blockType)
                    {
                        case _idName:
                            name = ReadName();
                            break;
                        case _idCurrent:
                            ReadCurrent();
                            break;
                        case _idAddBookmark:
                            ReadAddBookmark();
                            break;
                        case _idDeleteEvent:
                            ReadDeleteEvent();
                            break;
                        case _idDeleteEventAndChildren:
                            ReadDeleteEventAndChildren();
                            break;
                        case _idKey:
                            ReadKey();
                            break;
                        case _idReset:
                            ReadReset();
                            break;
                        case _idLoadDisc:
                            ReadLoadDisc();
                            break;
                        case _idLoadTape:
                            ReadLoadTape();
                            break;
                        case _idVersion:
                            ReadVersion();
                            break;
                        case _idRunUntil:
                            ReadRunUntil();
                            break;
                        case _idSetCurrentToRoot:
                            CPvC.Diagnostics.Trace("[SetCurrentToRoot]");

                            _machineHistory.SetCurrent(_machineHistory.RootEvent);
                            break;
                        default:
                            throw new Exception("Unknown block type!");
                    }
                }

                CPvC.Diagnostics.Trace("Read File DONE!!!");

                if (_machineHistory != null)
                {
                    _machineHistory.Auditors += HistoryEventHappened;
                }

                if (_machine != null)
                {
                    _machine.PropertyChanged += Machine_PropertyChanged;
                }
            }
        }

        private void Machine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                WriteName(_machine.Name);
            }
        }

        private string ReadName()
        {
            string name = ReadString();

            CPvC.Diagnostics.Trace("[Name] {0}", name);

            return name;
        }

        public void WriteName(string name)
        {
            lock (_byteStream)
            {
                WriteByte(_idName);
                WriteString(name);
            }
        }

        private void WriteAddBookmark(int id, UInt64 ticks, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                throw new Exception("Why are we adding a bookmark that's null??!?");
            }
            else
            {
                lock (_byteStream)
                {
                    WriteByte(_idAddBookmark);
                    WriteInt32(id);
                    WriteUInt64(ticks);
                    WriteBool(bookmark.System);
                    WriteInt32(bookmark.Version);
                    WriteBytesBlob(bookmark.State.GetBytes());
                    WriteCompressedBlob(bookmark.Screen.GetBytes());
                }
            }
        }

        private void ReadAddBookmark()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();

                Bookmark bookmark = null;
                UInt64 ticks = ReadUInt64();
                bool system = ReadBool();
                int version = ReadInt32();
                IBlob stateBlob = ReadBlob();
                IBlob screenBlob = ReadBlob();

                CPvC.Diagnostics.Trace("[AddBookmark] Id: {0} Ticks: {1}", id, ticks);

                bookmark = new Bookmark(system, version, stateBlob, screenBlob);

                HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark);

                _historyEventToId[historyEvent] = id;
                _idToHistoryEvent[id] = historyEvent;

                _nextPersistentId = Math.Max(_nextPersistentId, id + 1);
            }
        }

        private void ReadCurrent()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();

                //CPvC.Diagnostics.Trace("[SetCurrent] {0}", id);

                if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
                {
                    _machineHistory.SetCurrent(newId);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void WriteCoreAction(int id, UInt64 ticks, CoreAction action)
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
                case CoreRequest.Types.RunUntil:
                    WriteRunUntil(id, ticks);
                    break;
                default:
                    throw new Exception(String.Format("Unrecognized core action type {0}.", action.Type));
            }
        }

        private void ReadKey()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte keyCodeAndDown = ReadByte();
                byte keyCode = (byte)(keyCodeAndDown & 0x7F);
                bool keyDown = ((keyCodeAndDown & 0x80) != 0);

                //CPvC.Diagnostics.Trace("[Key] Id: {0} Ticks: {1} Code: {2}, Down: {3}", id, ticks, keyCode, keyDown);

                CoreAction action = CoreAction.KeyPress(ticks, keyCode, keyDown);

                AddCoreAction(id, action);
            }
        }

        private void WriteKey(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            lock (_byteStream)
            {
                // Since keyCode can only be a value from 0 to 79 (0x00 to 0x4F), we can use the most significant
                // bit to hold the "down" state of the key, instead of wasting a byte for that.
                byte keyCodeAndDown = (byte)((keyDown ? 0x80 : 0x00) | keyCode);

                WriteByte(_idKey);
                WriteInt32(id);
                WriteUInt64(ticks);
                WriteByte(keyCodeAndDown);
            }
        }

        private void WriteRunUntil(int id, UInt64 ticks)
        {
            lock (_byteStream)
            {
                WriteByte(_idRunUntil);
                WriteInt32(id);
                WriteUInt64(ticks);
            }
        }

        private void ReadReset()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                CoreAction action = CoreAction.Reset(ticks);

                CPvC.Diagnostics.Trace("[Reset] {0} {1}", id, ticks);

                AddCoreAction(id, action);
            }
        }

        private void WriteReset(int id, UInt64 ticks)
        {
            lock (_byteStream)
            {
                WriteByte(_idReset);
                WriteInt32(id);
                WriteUInt64(ticks);
            }
        }

        private void ReadLoadDisc()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                byte drive = ReadByte();
                IBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);

                AddCoreAction(id, action);
            }
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            lock (_byteStream)
            {
                WriteByte(_idLoadDisc);
                WriteInt32(id);
                WriteUInt64(ticks);
                WriteByte(drive);
                WriteCompressedBlob(media);
            }
        }

        private void ReadVersion()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                int version = ReadInt32();

                CPvC.Diagnostics.Trace("[Version] Id: {0} Ticks: {1} Version: {2}", id, ticks, version);

                CoreAction action = CoreAction.CoreVersion(ticks, version);

                AddCoreAction(id, action);
            }
        }

        private void ReadRunUntil()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();

                CPvC.Diagnostics.Trace("[RunUntil] Id: {0} Ticks: {1}", id, ticks);

                CoreAction action = CoreAction.RunUntil(ticks, ticks, null);

                AddCoreAction(id, action);
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

        private void ReadLoadTape()
        {
            lock (_byteStream)
            {
                int id = ReadInt32();
                UInt64 ticks = ReadUInt64();
                IBlob mediaBlob = ReadBlob();

                CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);

                HistoryEvent historyEvent = AddCoreAction(id, action);
            }
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            lock (_byteStream)
            {
                WriteByte(_idLoadTape);
                WriteInt32(id);
                WriteUInt64(ticks);
                WriteCompressedBlob(media);
            }
        }

        private HistoryEvent AddCoreAction(int id, CoreAction coreAction)
        {
            HistoryEvent historyEvent = _machineHistory.AddCoreAction(coreAction);
            _historyEventToId[historyEvent] = id;
            _idToHistoryEvent[id] = historyEvent;

            _nextPersistentId = Math.Max(_nextPersistentId, id + 1);

            return historyEvent;
        }

        private void ReadDeleteEvent()
        {
            int id = ReadInt32();

            CPvC.Diagnostics.Trace("[Delete] {0}", id);


            if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
            {
                _machineHistory.DeleteEvent(newId);
            }
            else
            {
                throw new InvalidOperationException("Can't find id to delete!");
            }
        }

        private void ReadDeleteEventAndChildren()
        {
            int id = ReadInt32();

            CPvC.Diagnostics.Trace("[Delete with Children] {0}", id);

            if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
            {
                bool b = _machineHistory.DeleteEventAndChildren(newId);
                if (!b)
                {
                    throw new InvalidOperationException("Couldn't delete history event!");
                }
            }
            else
            {
                throw new InvalidOperationException("Can't find id to delete!");
            }
        }
    }
}
