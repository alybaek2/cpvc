using System;
using System.Collections.Generic;
using System.IO;

namespace CPvC
{
    public class MachineFile : TextFile
    {
        private const string _idName = "name";
        private const string _idKey = "key";
        private const string _idReset = "reset";
        private const string _idLoadDisc = "disc";
        private const string _idLoadTape = "tape";
        private const string _idDeleteEventAndChildren = "deletewithchildren";
        private const string _idCurrent = "current";
        private const string _idAddBookmark = "bookmark";
        private const string _idVersion = "version";
        private const string _idRunUntil = "run";
        private const string _idSetCurrentToRoot = "currentroot";
        private const string _idDeleteEvent = "delete";

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
                        int persistentId = _historyEventToId[historyEvent];

                        string str = String.Format("{0}:{1}", _idDeleteEventAndChildren, persistentId);

                        WriteLine(str);
                    }
                    break;
                case HistoryEventType.DeleteEvent:
                    {
                        int persistentId = _historyEventToId[historyEvent];

                        string str = String.Format("{0}:{1}", _idDeleteEvent, persistentId);

                        WriteLine(str);
                    }
                    break;
                case HistoryEventType.SetCurrent:
                    {
                        if (historyEvent == _machineHistory.RootEvent)
                        {
                            string str = String.Format("{0}:", _idSetCurrentToRoot);

                            WriteLine(str);
                        }
                        else
                        {
                            int persistentId = _historyEventToId[historyEvent];

                            string str = String.Format("{0}:{1}", _idCurrent, persistentId);

                            WriteLine(str);
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

            while (true)
            {
                string line = ReadLine();

                if (line == null)
                {
                    break;
                }

                int colon = line.IndexOf(':');
                if (colon == -1)
                {
                    throw new Exception(String.Format("No colon found in line {0}", line));
                }

                string s = line.Substring(0, colon);
                string p = line.Substring(colon + 1);

                switch (line.Substring(0, colon))
                {
                    case _idName:
                        name = ReadName(p);
                        break;
                    case _idCurrent:
                        ReadCurrent(p);
                        break;
                    case _idAddBookmark:
                        ReadAddBookmark(p);
                        break;
                    case _idDeleteEvent:
                        ReadDeleteEvent(p);
                        break;
                    case _idDeleteEventAndChildren:
                        ReadDeleteEventAndChildren(p);
                        break;
                    case _idKey:
                        ReadKey(p);
                        break;
                    case _idReset:
                        ReadReset(p);
                        break;
                    case _idLoadDisc:
                        ReadLoadDisc(p);
                        break;
                    case _idLoadTape:
                        ReadLoadTape(p);
                        break;
                    case _idVersion:
                        ReadVersion(p);
                        break;
                    case _idRunUntil:
                        ReadRunUntil(p);
                        break;
                    case _idSetCurrentToRoot:
                        _machineHistory.SetCurrent(_machineHistory.RootEvent);
                        break;
                    default:
                        throw new Exception("Unknown block type!");
                }
            }

            if (_machineHistory != null)
            {
                _machineHistory.Auditors += HistoryEventHappened;
            }

            if (_machine != null)
            {
                _machine.PropertyChanged += Machine_PropertyChanged;
            }
        }

        private void Machine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                WriteName(_machine.Name);
            }
        }

        private string ReadName(string line)
        {
            string[] tokens = line.Split(',');

            string name = tokens[0];

            return name;
        }

        public void WriteName(string name)
        {
            string str = String.Format(
                "name:{0}",
                name);

            WriteLine(str);
        }

        private void WriteAddBookmark(int id, UInt64 ticks, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                throw new Exception("Why are we adding a bookmark that's null??!?");
            }
            else
            {
                string str = String.Format(
                    "bookmark:{0},{1},{2},{3},{4},{5}",
                    id,
                    ticks,
                    bookmark.System,
                    bookmark.Version,
                    Helpers.StrFromBytes(bookmark.State.GetBytes()),
                    Helpers.StrFromBytes(bookmark.Screen.GetBytes()));

                WriteLine(str);
            }
        }

        private void ReadAddBookmark(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            bool system = Convert.ToBoolean(tokens[2]);
            int version = Convert.ToInt32(tokens[3]);
            IBlob stateBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[4]));
            IBlob screenBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[5]));

            Bookmark bookmark = new Bookmark(system, version, stateBlob, screenBlob);

            HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark);

            _historyEventToId[historyEvent] = id;
            _idToHistoryEvent[id] = historyEvent;

            _nextPersistentId = Math.Max(_nextPersistentId, id + 1);
        }

        private void ReadCurrent(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
            {
                _machineHistory.SetCurrent(newId);
            }
            else
            {
                throw new InvalidOperationException();
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
                    WriteRunUntil(id, ticks, action.StopTicks);
                    break;
                default:
                    throw new Exception(String.Format("Unrecognized core action type {0}.", action.Type));
            }
        }

        private void ReadKey(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            byte keyCode = Convert.ToByte(tokens[2]);
            bool keyDown = Convert.ToBoolean(tokens[3]);

            CoreAction action = CoreAction.KeyPress(ticks, keyCode, keyDown);

            AddCoreAction(id, action);
        }

        private void WriteKey(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            string str = String.Format("key:{0},{1},{2},{3}",
                id,
                ticks,
                keyCode,
                keyDown
                );

            WriteLine(str);
        }

        private void WriteRunUntil(int id, UInt64 ticks, UInt64 stopTicks)
        {
            string str = String.Format("run:{0},{1},{2}",
                id,
                ticks,
                stopTicks
                );

            WriteLine(str);
        }

        private void ReadReset(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            CoreAction action = CoreAction.Reset(ticks);

            CPvC.Diagnostics.Trace("[Reset] {0} {1}", id, ticks);

            AddCoreAction(id, action);
        }

        private void WriteReset(int id, UInt64 ticks)
        {
            string str = String.Format("reset:{0},{1}",
                id,
                ticks
                );

            WriteLine(str);
        }

        private void ReadLoadDisc(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            byte drive = Convert.ToByte(tokens[2]);
            IBlob mediaBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[3]));

            CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);

            HistoryEvent historyEvent = AddCoreAction(id, action);
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            string str = String.Format("disc:{0},{1},{2},{3}",
                id,
                ticks,
                drive,
                Helpers.StrFromBytes(media)
                );

            WriteLine(str);
        }

        private void ReadVersion(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            int version = Convert.ToInt32(tokens[2]);

            CoreAction action = CoreAction.CoreVersion(ticks, version);

            AddCoreAction(id, action);
        }

        private void ReadRunUntil(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            UInt64 stopTicks = Convert.ToUInt64(tokens[2]);

            CoreAction action = CoreAction.RunUntil(ticks, stopTicks, null);

            AddCoreAction(id, action);
        }

        private void WriteVersion(int id, UInt64 ticks, int version)
        {
            string str = String.Format("version:{0},{1},{2}",
                id,
                ticks,
                version
                );

            WriteLine(str);
        }

        private void ReadLoadTape(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            IBlob mediaBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[2]));

            CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);

            HistoryEvent historyEvent = AddCoreAction(id, action);
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            string str = String.Format("tape:{0},{1},{2}",
                id,
                ticks,
                Helpers.StrFromBytes(media)
                );

            WriteLine(str);
        }

        private HistoryEvent AddCoreAction(int id, CoreAction coreAction)
        {
            HistoryEvent historyEvent = _machineHistory.AddCoreAction(coreAction);
            _historyEventToId[historyEvent] = id;
            _idToHistoryEvent[id] = historyEvent;

            _nextPersistentId = Math.Max(_nextPersistentId, id + 1);

            return historyEvent;
        }

        private void ReadDeleteEvent(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
            {
                _machineHistory.DeleteEvent(newId);
            }
            else
            {
                throw new InvalidOperationException("Can't find id to delete!");
            }
        }

        private void ReadDeleteEventAndChildren(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

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
