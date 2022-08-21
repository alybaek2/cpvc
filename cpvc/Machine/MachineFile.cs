using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CPvC
{
    public class MachineFileInfo
    {
        public string Name { get; private set; }
        public History History { get; private set; }
        public int NextLineId { get; private set; }

        public MachineFileInfo(string name, History history, int nextLineId)
        {
            Name = name;
            History = history;
            NextLineId = nextLineId;
        }
    }

    public class MachineFile : IDisposable
    {
        public const string _idName = "name";
        public const string _idKey = "key";
        public const string _idReset = "reset";
        public const string _idLoadDisc = "disc";
        public const string _idLoadTape = "tape";
        public const string _idDeleteBranch = "deletebranch";
        public const string _idCurrent = "current";
        public const string _idAddBookmark = "bookmark";
        public const string _idVersion = "version";
        public const string _idRunUntil = "run";
        public const string _idDeleteBookmark = "deletebookmark";
        public const string _idArg = "arg";
        public const string _idArgs = "args";

        private History _machineHistory;
        private Blobs _blobs;

        private LocalMachine _machine;

        private ITextFile _textFile;

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

        private History History
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

        public void Dispose()
        {
            Machine = null;
            History = null;

            if (_textFile != null)
            {
                _textFile.Dispose();
                _textFile = null;
            }
        }

        public MachineFile(ITextFile textFile, History machineHistory, int nextBlobId)
        {
            _textFile = textFile ?? throw new ArgumentNullException(nameof(textFile));
            _blobs = new Blobs(nextBlobId);

            History = machineHistory;
        }

        public MachineFile(ITextFile textFile, History machineHistory) : this(textFile, machineHistory, 0)
        {
        }

        private void HistoryEventHappened(HistoryEvent historyEvent, HistoryChangedAction changeAction)
        {
            List<string> lines = GetLines(historyEvent, changeAction);

            foreach (string line in lines)
            {
                _textFile.WriteLine(line);
            }
        }

        static private string DeleteBookmarkCommand(int historyEventId)
        {
            return String.Format("{0}:{1}", _idDeleteBookmark, historyEventId);
        }

        static private string DeleteBranchCommand(int historyEventId)
        {
            return String.Format("{0}:{1}", _idDeleteBranch, historyEventId);
        }

        static private string CurrentCommand(int historyEventId)
        {
            return String.Format("{0}:{1}", _idCurrent, historyEventId);
        }

        static private string NameCommand(string name)
        {
            return String.Format("{0}:{1}", _idName, name);
        }

        private string ArgCommand(int argId, IBlob blob, bool compress)
        {
            string bytesStr = "";
            byte[] bytes = blob.GetBytes();
            if (bytes == null)
            {
                bytesStr = "";
                compress = false;
            }
            else if (compress)
            {
                bytesStr = Helpers.StrFromBytes(Helpers.Compress(bytes));
            }
            else
            {
                bytesStr = Helpers.StrFromBytes(blob.GetBytes());
            }

            return String.Format("{0}:{1},{2},{3}", _idArg, argId, compress, bytesStr);
        }

        private string ArgsCommand(Blobs blobs, bool compress)
        {
            string argsLine = String.Join("@", blobs.Ids.Select(key => String.Format("{0}#{1}", key, Helpers.StrFromBytes(blobs.Get(key).GetBytes()))));
            if (compress)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(argsLine);
                argsLine = Helpers.StrFromBytes(Helpers.Compress(bytes));
            }

            return String.Format("{0}:{1},{2}", _idArgs, compress, argsLine);
        }

        private void GetLines(HistoryEvent historyEvent, List<string> lines)
        {
            int AddBlobLine(IBlob blob)
            {
                int id = _blobs.Add(blob);
                if (id != Blobs._nullBlobId)
                {
                    lines.Add(ArgCommand(id, blob, true));
                }

                return id;
            }

            switch (historyEvent)
            {
                case BookmarkHistoryEvent bookmarkHistoryEvent:
                    lines.Add(
                        String.Format(
                            "{0}:{1},{2},{3},{4},{5},${6},${7}",
                            _idAddBookmark,
                            bookmarkHistoryEvent.Id,
                            bookmarkHistoryEvent.Ticks,
                            bookmarkHistoryEvent.Bookmark.System,
                            bookmarkHistoryEvent.Bookmark.Version,
                            bookmarkHistoryEvent.Node.CreateDate.Ticks,
                            AddBlobLine(bookmarkHistoryEvent.Bookmark.State),
                            AddBlobLine(bookmarkHistoryEvent.Bookmark.Screen)));

                    break;
                case CoreActionHistoryEvent coreActionHistoryEvent:
                    switch (coreActionHistoryEvent.CoreAction.Type)
                    {
                        case MachineRequest.Types.KeyPress:
                            lines.Add(
                                String.Format(
                                    "{0}:{1},{2},{3},{4}",
                                    _idKey,
                                    coreActionHistoryEvent.Id,
                                    coreActionHistoryEvent.CoreAction.Ticks,
                                    coreActionHistoryEvent.CoreAction.KeyCode,
                                    coreActionHistoryEvent.CoreAction.KeyDown));
                            break;
                        case MachineRequest.Types.Reset:
                            lines.Add(
                                String.Format("{0}:{1},{2}",
                                    _idReset,
                                    coreActionHistoryEvent.Id,
                                    coreActionHistoryEvent.CoreAction.Ticks));
                            break;
                        case MachineRequest.Types.LoadDisc:
                            lines.Add(
                                String.Format("{0}:{1},{2},{3},${4}",
                                    _idLoadDisc,
                                    coreActionHistoryEvent.Id,
                                    coreActionHistoryEvent.CoreAction.Ticks,
                                    coreActionHistoryEvent.CoreAction.Drive,
                                    AddBlobLine(coreActionHistoryEvent.CoreAction.MediaBuffer)));
                            break;
                        case MachineRequest.Types.LoadTape:
                            lines.Add(
                                String.Format("{0}:{1},{2},${3}",
                                    _idLoadTape,
                                    coreActionHistoryEvent.Id,
                                    coreActionHistoryEvent.CoreAction.Ticks,
                                    AddBlobLine(coreActionHistoryEvent.CoreAction.MediaBuffer)));
                            break;
                        case MachineRequest.Types.CoreVersion:
                            lines.Add(
                                String.Format("{0}:{1},{2},{3}",
                                    _idVersion,
                                    coreActionHistoryEvent.Id,
                                    coreActionHistoryEvent.CoreAction.Ticks,
                                    coreActionHistoryEvent.CoreAction.Version));
                            break;
                        case MachineRequest.Types.RunUntil:
                            lines.Add(
                                String.Format("{0}:{1},{2},{3}",
                                    _idRunUntil,
                                    coreActionHistoryEvent.Id,
                                    coreActionHistoryEvent.CoreAction.Ticks,
                                    coreActionHistoryEvent.CoreAction.StopTicks));
                            break;
                        default:
                            throw new ArgumentException(String.Format("Unrecognized core action type {0}.", coreActionHistoryEvent.CoreAction.Type), "type");
                    }

                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown history event type '{0}'.", historyEvent.GetType()));
            }
        }

        private List<string> GetLines(HistoryEvent historyEvent, HistoryChangedAction changeType)
        {
            List<string> lines = new List<string>();

            switch (changeType)
            {
                case HistoryChangedAction.Add:
                    GetLines(historyEvent, lines);
                    break;
                case HistoryChangedAction.DeleteBookmark:
                    lines.Add(DeleteBookmarkCommand(historyEvent.Id));
                    break;
                case HistoryChangedAction.DeleteBranch:
                    lines.Add(DeleteBranchCommand(historyEvent.Id));
                    break;
                case HistoryChangedAction.SetCurrent:
                    lines.Add(CurrentCommand(historyEvent.Id));
                    break;
                default:
                    throw new ArgumentException("Unknown history action type!", nameof(changeType));
            }

            return lines;
        }

        public void WriteHistory(string name)
        {
            List<string> lines = new List<string>
            {
                NameCommand(name)
            };

            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();
            historyEvents.AddRange(History.RootEvent.Children);

            HistoryEvent previousEvent = null;
            while (historyEvents.Count > 0)
            {
                HistoryEvent currentEvent = historyEvents[0];

                if (previousEvent != currentEvent.Parent && previousEvent != null)
                {
                    lines.Add(CurrentCommand(currentEvent.Parent.Id));
                }

                if (History.IsClosedEvent(currentEvent) && !(currentEvent is CoreActionHistoryEvent coreActionHistoryEvent && coreActionHistoryEvent.CoreAction.Type == MachineRequest.Types.RunUntil && coreActionHistoryEvent.Children.Count == 1))
                {
                    GetLines(currentEvent, lines);
                }

                historyEvents.RemoveAt(0);
                previousEvent = currentEvent;

                // Place the current event's children at the top of the "stack". This effectively means we're doing a depth-first traversion of the history tree.
                historyEvents.InsertRange(0, currentEvent.Children);
            }

            HistoryEvent historyEvent = History.MostRecentClosedEvent(History.CurrentEvent);
            lines.Add(CurrentCommand(historyEvent.Id));

            // If we have any "arg" commands, stick them in a "args" command and put them at the start of the file.
            // Putting them in a single command should allow them to be better compressed than individually.
            Blobs blobs = new Blobs(1);
            int i = 0;
            while (i < lines.Count)
            {
                string[] tokens = lines[i].Split(':');
                if (tokens[0] == _idArg)
                {
                    lines.RemoveAt(i);

                    MachineFileReader.ReadArgCommand(tokens[1], blobs);
                }
                else
                {
                    i++;
                }
            }

            // Write the file.
            if (blobs.Ids.Count > 0)
            {
                lines.Insert(0, ArgsCommand(blobs, true));
            }

            foreach (string line in lines)
            {
                _textFile.WriteLine(line);
            }
        }

        private void Machine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                _textFile.WriteLine(NameCommand(_machine.Name));
            }
        }

        static public MachineFileInfo Read(ITextFile textFile)
        {
            MachineFileReader reader = new MachineFileReader();
            reader.ReadFile(textFile);

            MachineFileInfo info = new MachineFileInfo(reader.Name, reader.History, reader.NextLineId);

            return info;
        }

        private class MachineFileReader
        {
            private string _name;
            private History _machineHistory;
            private Dictionary<int, HistoryEvent> _idToHistoryEvent;
            private int _nextLineId = 0;
            private Blobs _blobs;

            public void ReadFile(ITextFile textFile)
            {
                _idToHistoryEvent = new Dictionary<int, HistoryEvent>();
                _blobs = new Blobs(1);

                _name = null;
                _machineHistory = new History();
                _idToHistoryEvent[_machineHistory.RootEvent.Id] = _machineHistory.RootEvent;

                string line;
                while ((line = textFile.ReadLine()) != null)
                {
                    ProcessLine(line);
                }
            }

            public int NextLineId
            {
                get
                {
                    return _nextLineId;
                }
            }

            public History History
            {
                get
                {
                    return _machineHistory;
                }
            }

            public string Name
            {
                get
                {
                    return _name;
                }
            }

            private string ReadName(string line)
            {
                string[] tokens = line.Split(',');

                return tokens[0];
            }

            static public void ReadArgCommand(string line, Blobs blobs)
            {
                string[] tokens = line.Split(',');
                int argId = Convert.ToInt32(tokens[0]);
                bool compress = Convert.ToBoolean(tokens[1]);
                string argValue = tokens[2];
                blobs.Add(argId, new LazyLoadBlob(argValue, compress));
            }

            public void ReadArgsCommand(string line, Blobs blobs)
            {
                string[] tokens = line.Split(',');

                bool compress = Convert.ToBoolean(tokens[0]);
                string argsArg = tokens[1];
                if (compress)
                {
                    byte[] bytes = Helpers.Uncompress(Helpers.BytesFromStr(argsArg));
                    argsArg = Encoding.UTF8.GetString(bytes);
                }

                string[] argPairs = argsArg.Split('@');

                foreach (string argPair in argPairs)
                {
                    string[] argPairTokens = argPair.Split('#');

                    int argId = Convert.ToInt32(argPairTokens[0]);
                    string argValue = argPairTokens[1];

                    blobs.Add(argId, new LazyLoadBlob(argValue, false));
                }
            }

            private IBlob GetBlobArg(string arg)
            {
                if (arg.StartsWith("$"))
                {
                    int argId = System.Convert.ToInt32(arg.Substring(1));
                    return _blobs.Get(argId);
                }

                throw new Exception("No blob found!");
            }

            private void ReadAddBookmark(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                if (_idToHistoryEvent.ContainsKey(id))
                {
                    throw new InvalidOperationException(String.Format("Id {0} has already been processed!", id));
                }

                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                bool system = Convert.ToBoolean(tokens[2]);
                int version = Convert.ToInt32(tokens[3]);
                Int64 dateTimeTicks = Convert.ToInt64(tokens[4]);

                IBlob state = GetBlobArg(tokens[5]);
                IBlob screen = GetBlobArg(tokens[6]);

                DateTime creationTime = new DateTime(dateTimeTicks, DateTimeKind.Utc);

                Bookmark bookmark = new Bookmark(system, version, state, screen);

                HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark, creationTime, id);

                _idToHistoryEvent[id] = historyEvent;

                _nextLineId = Math.Max(_nextLineId, id + 1);
            }

            private void ReadCurrent(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);

                if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent historyEvent))
                {
                    _machineHistory.CurrentEvent = historyEvent;
                }
                else
                {
                    throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
                }
            }

            private void ReadKey(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                byte keyCode = Convert.ToByte(tokens[2]);
                bool keyDown = Convert.ToBoolean(tokens[3]);

                MachineAction action = MachineAction.KeyPress(ticks, keyCode, keyDown);

                AddCoreAction(id, action);
            }

            private void ReadReset(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                MachineAction action = MachineAction.Reset(ticks);

                AddCoreAction(id, action);
            }

            private void ReadLoadDisc(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                byte drive = Convert.ToByte(tokens[2]);
                IBlob mediaBlob = GetBlobArg(tokens[3]);

                MachineAction action = MachineAction.LoadDisc(ticks, drive, mediaBlob);

                AddCoreAction(id, action);
            }

            private void ReadVersion(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                int version = Convert.ToInt32(tokens[2]);

                MachineAction action = MachineAction.CoreVersion(ticks, version);

                AddCoreAction(id, action);
            }

            private void ReadRunUntil(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                UInt64 stopTicks = Convert.ToUInt64(tokens[2]);

                MachineAction action = MachineAction.RunUntil(ticks, stopTicks, null);

                AddCoreAction(id, action);
            }

            private void ReadLoadTape(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);
                UInt64 ticks = Convert.ToUInt64(tokens[1]);
                IBlob mediaBlob = GetBlobArg(tokens[2]);

                MachineAction action = MachineAction.LoadTape(ticks, mediaBlob);

                AddCoreAction(id, action);
            }

            private HistoryEvent AddCoreAction(int id, MachineAction coreAction)
            {
                if (_idToHistoryEvent.ContainsKey(id))
                {
                    throw new InvalidOperationException(String.Format("Id {0} has already been processed!", id));
                }

                HistoryEvent historyEvent = _machineHistory.AddCoreAction(coreAction, id);
                _idToHistoryEvent[id] = historyEvent;

                _nextLineId = Math.Max(_nextLineId, id + 1);

                return historyEvent;
            }

            private void ReadDeleteBookmark(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);

                if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent historyEvent))
                {
                    _machineHistory.DeleteBookmark(historyEvent);
                }
                else
                {
                    throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
                }
            }

            private void ReadDeleteBranch(string line)
            {
                string[] tokens = line.Split(',');

                int id = Convert.ToInt32(tokens[0]);

                if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent historyEvent))
                {
                    bool b = _machineHistory.DeleteBranch(historyEvent);
                    if (!b)
                    {
                        throw new InvalidOperationException(String.Format("Couldn't delete history event {0}!", id));
                    }
                }
                else
                {
                    throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
                }
            }

            private void ProcessLine(string line)
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
                    case MachineFile._idName:
                        _name = ReadName(args);
                        break;
                    case MachineFile._idCurrent:
                        ReadCurrent(args);
                        break;
                    case MachineFile._idAddBookmark:
                        ReadAddBookmark(args);
                        break;
                    case MachineFile._idDeleteBookmark:
                        ReadDeleteBookmark(args);
                        break;
                    case MachineFile._idDeleteBranch:
                        ReadDeleteBranch(args);
                        break;
                    case MachineFile._idKey:
                        ReadKey(args);
                        break;
                    case MachineFile._idReset:
                        ReadReset(args);
                        break;
                    case MachineFile._idLoadDisc:
                        ReadLoadDisc(args);
                        break;
                    case MachineFile._idLoadTape:
                        ReadLoadTape(args);
                        break;
                    case MachineFile._idVersion:
                        ReadVersion(args);
                        break;
                    case MachineFile._idRunUntil:
                        ReadRunUntil(args);
                        break;
                    case MachineFile._idArg:
                        ReadArgCommand(args, _blobs);
                        break;
                    case MachineFile._idArgs:
                        ReadArgsCommand(args, _blobs);
                        break;
                    default:
                        throw new ArgumentException(String.Format("Unknown type {0}.", type), "type");
                }
            }
        }

        private class Blobs
        {
            public const int _nullBlobId = -1;
            static private NullBlob _nullBlob = new NullBlob();
            private int _nextBlobId;
            private Dictionary<int, IBlob> _blobs;

            private class NullBlob : IBlob
            {
                public byte[] GetBytes()
                {
                    return null;
                }
            }

            public Blobs(int nextBlobId)
            {
                _nextBlobId = nextBlobId;
                _blobs = new Dictionary<int, IBlob>();
                _blobs[_nullBlobId] = _nullBlob;
            }

            public List<int> Ids
            {
                get
                {
                    return _blobs.Keys.ToList();
                }
            }

            public int Add(IBlob blob)
            {
                if (blob == null || blob.GetBytes() == null)
                {
                    return _nullBlobId;
                }

                int id = _nextBlobId++;
                _blobs[id] = blob;

                return id;
            }

            public int Add(int id, IBlob blob)
            {
                if (blob == null || blob.GetBytes() == null)
                {
                    return _nullBlobId;
                }

                _blobs[id] = blob;

                _nextBlobId = Math.Max(id + 1, _nextBlobId);
                return id;
            }

            public IBlob Get(int id)
            {
                return _blobs[id];
            }
        }
    }
}
