using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        private const string _idDeleteEvent = "delete";
        private const string _idBlob = "blob";
        private const string _idCompound = "compound";

        private Dictionary<HistoryEvent, int> _historyEventToId;
        private MachineHistory _machineHistory;
        private int _nextPersistentId;
        private int _nextBlob;

        private LocalMachine _machine;

        static private Dictionary<string, LocalMachine> _machines = new Dictionary<string, LocalMachine>();

        public LocalMachine Machine
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
            _nextPersistentId = 0;
            _nextBlob = 0;
        }

        private void HistoryEventHappened(HistoryEvent historyEvent, UInt64 ticks, HistoryChangedAction changeAction, CoreAction coreAction, Bookmark bookmark)
        {
            List<string> lines = new List<string>();
            GetLines(historyEvent, changeAction, ref _nextPersistentId, ref _nextBlob, lines, _historyEventToId);

            foreach (string line in lines)
            {
                WriteLine(line);
            }
        }

        static public string DeleteEventCommand(int persistentId)
        {
            return String.Format("{0}:{1}", _idDeleteEvent, persistentId);
        }

        static public string DeleteEventAndChildrenCommand(int persistentId)
        {
            return String.Format("{0}:{1}", _idDeleteEventAndChildren, persistentId);
        }

        static public string CurrentCommand(int persistentId)
        {
            return String.Format("{0}:{1}", _idCurrent, persistentId);
        }

        static public string CurrentRootCommand()
        {
            return String.Format("{0}:root", _idCurrent);
        }

        static public string NameCommand(string name)
        {
            return String.Format("{0}:{1}", _idName, name);
        }

        static public string AddBookmarkCommand(int id, UInt64 ticks, bool system, int version, int stateBlobId, int screenBlobId)
        {
            return String.Format(
                "{0}:{1},{2},{3},{4},{5},{6}",
                _idAddBookmark,
                id,
                ticks,
                system,
                version,
                stateBlobId,
                screenBlobId);
        }

        static public string KeyCommand(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            return String.Format("{0}:{1},{2},{3},{4}",
                _idKey,
                id,
                ticks,
                keyCode,
                keyDown);
        }

        static public string LoadDiscCommand(int id, UInt64 ticks, byte drive, int mediaBlobId)
        {
            return String.Format("{0}:{1},{2},{3},{4}",
                _idLoadDisc,
                id,
                ticks,
                drive,
                mediaBlobId);
        }

        static public string LoadTapeCommand(int id, UInt64 ticks, int mediaBlobId)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idLoadTape,
                id,
                ticks,
                mediaBlobId);
        }

        static public string RunCommand(int id, UInt64 ticks, UInt64 stopTicks)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idRunUntil,
                id,
                ticks,
                stopTicks);
        }

        static public string ResetCommand(int id, UInt64 ticks)
        {
            return String.Format("{0}:{1},{2}",
                _idReset,
                id,
                ticks);
        }

        static public string VersionCommand(int id, UInt64 ticks, int version)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idVersion,
                id,
                ticks,
                version);
        }

        static public string BlobCommand(int blobId, byte[] blob)
        {
            return String.Format(
                "{0}:{1},{2}",
                _idBlob,
                blobId,
                Helpers.StrFromBytes(blob));
        }

        static public string CompoundCommand(IEnumerable<string> commands, bool compress)
        {
            string str = String.Join("@", commands);

            if (compress)
            {
                byte[] b = Encoding.UTF8.GetBytes(str);
                str = Helpers.StrFromBytes(Helpers.Compress(b));
            }

            return String.Format(
                "{0}:{1},{2}",
                _idCompound,
                compress ? 1 : 0,
                str);
        }

        static public int GetLines(Bookmark bookmark, UInt64 ticks, ref int nextLineId, ref int nextBlobId, List<string> lines)
        {
            int stateBlobId = nextBlobId++;
            lines.Add(MachineFile.BlobCommand(stateBlobId, bookmark.State.GetBytes()));

            int screenBlobId = nextBlobId++;
            lines.Add(MachineFile.BlobCommand(screenBlobId, bookmark.Screen.GetBytes()));

            int lineId = nextLineId++;
            lines.Add(MachineFile.AddBookmarkCommand(lineId, ticks, bookmark.System, bookmark.Version, stateBlobId, screenBlobId));

            return lineId;
        }

        static public int GetLines(CoreAction action, ref int nextLineId, ref int nextBlobId, List<string> lines)
        {
            int lineId = nextLineId++;

            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    lines.Add(MachineFile.KeyCommand(lineId, action.Ticks, action.KeyCode, action.KeyDown));
                    break;
                case CoreRequest.Types.Reset:
                    lines.Add(MachineFile.ResetCommand(lineId, action.Ticks));
                    break;
                case CoreRequest.Types.LoadDisc:
                    {
                        int mediaBlobId = nextBlobId++;
                        lines.Add(MachineFile.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));
                        lines.Add(MachineFile.LoadDiscCommand(lineId, action.Ticks, action.Drive, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.LoadTape:
                    {
                        int mediaBlobId = nextBlobId++;
                        lines.Add(MachineFile.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));
                        lines.Add(MachineFile.LoadTapeCommand(lineId, action.Ticks, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.CoreVersion:
                    lines.Add(MachineFile.VersionCommand(lineId, action.Ticks, action.Version));
                    break;
                case CoreRequest.Types.RunUntil:
                    lines.Add(MachineFile.RunCommand(lineId, action.Ticks, action.StopTicks));
                    break;
                default:
                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", action.Type), "type");
            }

            return lineId;
        }

        static public void GetLines(HistoryEvent historyEvent, HistoryChangedAction changeType, ref int nextLineId, ref int nextBlobId, List<string> lines, Dictionary<HistoryEvent, int> historyEventIds)
        {
            switch (changeType)
            {
                case HistoryChangedAction.Add:
                    {
                        int lineId;
                        switch (historyEvent.Type)
                        {
                            case HistoryEventType.Bookmark:
                                lineId = GetLines(historyEvent.Bookmark, historyEvent.Ticks, ref nextLineId, ref nextBlobId, lines);
                                break;
                            case HistoryEventType.CoreAction:
                                lineId = GetLines(historyEvent.CoreAction, ref nextLineId, ref nextBlobId, lines);
                                break;
                            default:
                                throw new ArgumentException("Unknown history event type!", "historyEvent.Type");
                        }

                        historyEventIds[historyEvent] = lineId;
                    }
                    break;
                case HistoryChangedAction.Delete:
                    {
                        int lineId = historyEventIds[historyEvent];

                        lines.Add(DeleteEventCommand(lineId));

                        // Should we remove the id from the maps?
                    }
                    break;
                case HistoryChangedAction.DeleteRecursive:
                    {
                        int LineId = historyEventIds[historyEvent];

                        lines.Add(DeleteEventAndChildrenCommand(LineId));

                        // Should we remove the id from the maps?
                    }
                    break;
                case HistoryChangedAction.SetCurrent:
                    {
                        if (historyEvent.Type == HistoryEventType.Root)
                        {
                            lines.Add(CurrentRootCommand());
                        }
                        else
                        {
                            int lineId = historyEventIds[historyEvent];

                            lines.Add(CurrentCommand(lineId));
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown history action type!", nameof(changeType));
            }
        }

        static public void Write(string filepath, string name, MachineHistory history)
        {
            List<string> lines = new List<string>();

            lines.Add(NameCommand(name));

            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();
            historyEvents.AddRange(history.RootEvent.Children);

            int? newCurrenEventId = null;

            int nextLineId = 0;
            int nextBlodId = 0;

            Dictionary<HistoryEvent, int> eventIds = new Dictionary<HistoryEvent, int>();

            HistoryEvent previousEvent = null;
            while (historyEvents.Count > 0)
            {
                int currentLineId = nextLineId;
                HistoryEvent currentEvent = historyEvents[0];
                eventIds[currentEvent] = currentLineId;
                nextLineId++;

                if (previousEvent != currentEvent.Parent && previousEvent != null)
                {
                    lines.Add(MachineFile.CurrentCommand(nextLineId));
                }

                switch (currentEvent.Type)
                {
                    case HistoryEventType.CoreAction:
                        GetLines(currentEvent.CoreAction, ref nextLineId, ref nextBlodId, lines);
                        break;
                    case HistoryEventType.Bookmark:
                        GetLines(currentEvent.Bookmark, currentEvent.Ticks, ref nextLineId, ref nextBlodId, lines);
                        break;
                    default:
                        throw new Exception("Unexpected node type!");
                }

                if (currentEvent == history.CurrentEvent)
                {
                    newCurrenEventId = currentLineId;
                }

                historyEvents.RemoveAt(0);
                previousEvent = currentEvent;

                // Place the current event's children at the top of the "stack". This effectively means we're doing a depth-first traversion of the history tree.
                historyEvents.InsertRange(0, currentEvent.Children);
            }

            if (newCurrenEventId != null)
            {
                lines.Add(MachineFile.CurrentCommand(newCurrenEventId.Value));
            }

            // If we have any blob commands, stick them in a compound command and put them at the start of the file.
            // Putting them in a single command should allow them to be better compressed than individually.
            List<string> blobCommands = new List<string>();
            int i = 0;
            while (i < lines.Count)
            {
                if (lines[i].StartsWith(_idBlob))
                {
                    blobCommands.Add(lines[i]);
                    lines.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }

            // Write the file.
            if (blobCommands.Count > 0)
            {
                // Create a "compound" command for all the blobs.
                string compoundCommand = MachineFile.CompoundCommand(blobCommands, true);

                lines.Insert(0, compoundCommand);
            }

            System.IO.File.WriteAllLines(filepath, lines);
        }

        public void ReadFile(out string name, out MachineHistory history)
        {
            Dictionary<int, HistoryEvent> idToHistoryEvent = new Dictionary<int, HistoryEvent>();

            name = null;
            history = new MachineHistory();
            _machineHistory = history;

            Dictionary<int, IBlob> blobs = new Dictionary<int, IBlob>();

            // Should probably clear history first...
            if (_machine != null)
            {
                _machine.PropertyChanged -= Machine_PropertyChanged;
            }

            if (_machineHistory != null)
            {
                _machineHistory.Auditors -= HistoryEventHappened;
            }

            _byteStream.Position = 0;

            while (true)
            {
                string line = ReadLine();

                if (line == null)
                {
                    break;
                }

                ReadLine(line, blobs, idToHistoryEvent, ref name);
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

        private void ReadLine(string line, Dictionary<int, IBlob> blobs, Dictionary<int, HistoryEvent> idToHistoryEvent, ref string name)
        {
            int colon = line.IndexOf(':');
            if (colon == -1)
            {
                throw new Exception(String.Format("No colon found in line {0}", line));
            }

            string type = line.Substring(0, colon);
            string args = line.Substring(colon + 1);

            switch (type)
            {
                case _idName:
                    name = ReadName(args);
                    break;
                case _idCurrent:
                    ReadCurrent(args, idToHistoryEvent);
                    break;
                case _idAddBookmark:
                    ReadAddBookmark(args, blobs, idToHistoryEvent);
                    break;
                case _idDeleteEvent:
                    ReadDeleteEvent(args, idToHistoryEvent);
                    break;
                case _idDeleteEventAndChildren:
                    ReadDeleteEventAndChildren(args, idToHistoryEvent);
                    break;
                case _idKey:
                    ReadKey(args, idToHistoryEvent);
                    break;
                case _idReset:
                    ReadReset(args, idToHistoryEvent);
                    break;
                case _idLoadDisc:
                    ReadLoadDisc(args, blobs, idToHistoryEvent);
                    break;
                case _idLoadTape:
                    ReadLoadTape(args, blobs, idToHistoryEvent);
                    break;
                case _idVersion:
                    ReadVersion(args, idToHistoryEvent);
                    break;
                case _idRunUntil:
                    ReadRunUntil(args, idToHistoryEvent);
                    break;
                case _idBlob:
                    ReadBlob(args, blobs);
                    break;
                case _idCompound:
                    ReadCompoundCommand(args, blobs, ref name, idToHistoryEvent);
                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown type {0}.", type), "type");
            }
        }

        private string ReadName(string line)
        {
            string[] tokens = line.Split(',');

            string name = tokens[0];

            return name;
        }

        public int WriteBlob(byte[] blob)
        {
            int blobId = _nextBlob++;
            WriteLine(BlobCommand(blobId, blob));

            return blobId;
        }

        public void ReadBlob(string args, Dictionary<int, IBlob> blobs)
        {
            string[] tokens = args.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            IBlob blob = new MemoryBlob(Helpers.BytesFromStr(tokens[1]));

            blobs[id] = blob;
        }

        public void WriteName(string name)
        {
            WriteLine(NameCommand(name));
        }

        private void ReadCompoundCommand(string line, Dictionary<int, IBlob> blobs, ref string name, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int compress = Convert.ToInt32(tokens[0]);

            string commands = tokens[1];
            if (compress == 1)
            {
                byte[] bytes = Helpers.Uncompress(Helpers.BytesFromStr(commands));

                commands = Encoding.UTF8.GetString(bytes);
            }

            foreach(string command in commands.Split('@'))
            {
                ReadLine(command, blobs, idToHistoryEvent, ref name);
            }
        }

        private void ReadAddBookmark(string line, Dictionary<int, IBlob> blobs, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            bool system = Convert.ToBoolean(tokens[2]);
            int version = Convert.ToInt32(tokens[3]);
            int stateBlobId = Convert.ToInt32(tokens[4]);
            int screenBlobId = Convert.ToInt32(tokens[5]);
            IBlob stateBlob = blobs[stateBlobId];
            IBlob screenBlob = blobs[screenBlobId];

            Bookmark bookmark = new Bookmark(system, version, stateBlob, screenBlob);

            HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark);

            _historyEventToId[historyEvent] = id;
            idToHistoryEvent[id] = historyEvent;

            _nextPersistentId = Math.Max(_nextPersistentId, id + 1);
        }

        private void ReadCurrent(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            if (tokens[0] == "root")
            {
                _machineHistory.SetCurrent(_machineHistory.RootEvent);
            }
            else
            {
                int id = Convert.ToInt32(tokens[0]);

                if (idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
                {
                    _machineHistory.SetCurrent(newId);
                }
                else
                {
                    throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
                }
            }
        }

        private void ReadKey(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            byte keyCode = Convert.ToByte(tokens[2]);
            bool keyDown = Convert.ToBoolean(tokens[3]);

            CoreAction action = CoreAction.KeyPress(ticks, keyCode, keyDown);

            AddCoreAction(id, action, idToHistoryEvent);
        }

        private void ReadReset(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            CoreAction action = CoreAction.Reset(ticks);

            AddCoreAction(id, action, idToHistoryEvent);
        }

        private void ReadLoadDisc(string line, Dictionary<int, IBlob> blobs, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            byte drive = Convert.ToByte(tokens[2]);
            int mediaBlobId = Convert.ToInt32(tokens[3]);
            IBlob mediaBlob = blobs[mediaBlobId];

            CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);

            HistoryEvent historyEvent = AddCoreAction(id, action, idToHistoryEvent);
        }

        private void ReadVersion(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            int version = Convert.ToInt32(tokens[2]);

            CoreAction action = CoreAction.CoreVersion(ticks, version);

            AddCoreAction(id, action, idToHistoryEvent);
        }

        private void ReadRunUntil(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            UInt64 stopTicks = Convert.ToUInt64(tokens[2]);

            CoreAction action = CoreAction.RunUntil(ticks, stopTicks, null);

            AddCoreAction(id, action, idToHistoryEvent);
        }

        private void ReadLoadTape(string line, Dictionary<int, IBlob> blobs, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            int mediaBlobId = Convert.ToInt32(tokens[2]);
            IBlob mediaBlob = blobs[mediaBlobId];

            CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);

            HistoryEvent historyEvent = AddCoreAction(id, action, idToHistoryEvent);
        }

        private HistoryEvent AddCoreAction(int id, CoreAction coreAction, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            HistoryEvent historyEvent = _machineHistory.AddCoreAction(coreAction);
            _historyEventToId[historyEvent] = id;
            idToHistoryEvent[id] = historyEvent;

            _nextPersistentId = Math.Max(_nextPersistentId, id + 1);

            return historyEvent;
        }

        private void ReadDeleteEvent(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            if (idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
            {
                _machineHistory.DeleteEvent(newId);
            }
            else
            {
                throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
            }
        }

        private void ReadDeleteEventAndChildren(string line, Dictionary<int, HistoryEvent> idToHistoryEvent)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            if (idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
            {
                bool b = _machineHistory.DeleteEventAndChildren(newId);
                if (!b)
                {
                    throw new InvalidOperationException("Couldn't delete history event!");
                }
            }
            else
            {
                throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
            }
        }
    }
}
