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
        private Dictionary<int, HistoryEvent> _idToHistoryEvent;
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
            _idToHistoryEvent = new Dictionary<int, HistoryEvent>();
            _nextPersistentId = 0;
            _nextBlob = 0;
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

                        WriteDeleteEventAndChildren(persistentId);
                    }
                    break;
                case HistoryEventType.DeleteEvent:
                    {
                        int persistentId = _historyEventToId[historyEvent];

                        WriteDeleteEvent(persistentId);
                    }
                    break;
                case HistoryEventType.SetCurrent:
                    {
                        if (historyEvent == _machineHistory.RootEvent)
                        {
                            WriteCurrentRoot();
                        }
                        else
                        {
                            int persistentId = _historyEventToId[historyEvent];

                            WriteCurrent(persistentId);
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown history type!", "type");
            }
        }

        static public string DeleteEventCommand(int persistentId)
        {
            return String.Format("{0}:{1}", _idDeleteEventAndChildren, persistentId);
        }

        static public string DeleteEventAndChildrenCommand(int persistentId)
        {
            return String.Format("{0}:{1}", _idDeleteEvent, persistentId);
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
            return String.Format("{0}:{1},{2},{3},{4}",
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

        static public void Write(string filepath, string name, MachineHistory history)
        {
            List<string> nonBlobCommands = new List<string>();
            List<string> blobCommands = new List<string>();

            nonBlobCommands.Add(NameCommand(name));

            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryEvent> historyNodes = new List<HistoryEvent>();
            historyNodes.AddRange(history.RootEvent.Children);

            int? newCurrentNodeId = null;

            int persistentId = 0;

            int nextBlobId = 0;

            Dictionary<HistoryEvent, int> nodeIds = new Dictionary<HistoryEvent, int>();

            HistoryEvent previousNode = null;
            while (historyNodes.Count > 0)
            {
                int currentPersistentId = persistentId;
                HistoryEvent currentNode = historyNodes[0];
                nodeIds[currentNode] = currentPersistentId;
                persistentId++;

                if (previousNode != currentNode.Parent && previousNode != null)
                {
                    nonBlobCommands.Add(MachineFile.CurrentCommand(persistentId));
                }

                switch (currentNode.Type)
                {
                    case HistoryEventType.AddCoreAction:
                        {
                            CoreAction action = currentNode.CoreAction;
                            int id = currentPersistentId;
                            UInt64 ticks = currentNode.Ticks;

                            switch (action.Type)
                            {
                                case CoreRequest.Types.KeyPress:
                                    nonBlobCommands.Add(MachineFile.KeyCommand(id, ticks, action.KeyCode, action.KeyDown));
                                    break;
                                case CoreRequest.Types.Reset:
                                    nonBlobCommands.Add(MachineFile.ResetCommand(id, ticks));
                                    break;
                                case CoreRequest.Types.LoadDisc:
                                    {
                                        int mediaBlobId = nextBlobId++;
                                        blobCommands.Add(MachineFile.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));

                                        nonBlobCommands.Add(MachineFile.LoadDiscCommand(id, ticks, action.Drive, mediaBlobId));
                                    }
                                    break;
                                case CoreRequest.Types.LoadTape:
                                    {
                                        int mediaBlobId = nextBlobId++;
                                        blobCommands.Add(MachineFile.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));

                                        nonBlobCommands.Add(MachineFile.LoadTapeCommand(id, ticks, mediaBlobId));
                                    }
                                    break;
                                case CoreRequest.Types.CoreVersion:
                                    nonBlobCommands.Add(MachineFile.VersionCommand(id, ticks, action.Version));
                                    break;
                                case CoreRequest.Types.RunUntil:
                                    nonBlobCommands.Add(MachineFile.RunCommand(id, ticks, action.StopTicks));
                                    break;
                                default:
                                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", action.Type), "type");
                            }
                        }

                        break;
                    case HistoryEventType.AddBookmark:
                        {
                            Bookmark bookmark = currentNode.Bookmark;
                            int id = currentPersistentId;
                            UInt64 ticks = currentNode.Ticks;

                            int stateBlobId = nextBlobId++;
                            blobCommands.Add(MachineFile.BlobCommand(stateBlobId, bookmark.State.GetBytes()));

                            int screenBlobId = nextBlobId++;
                            blobCommands.Add(MachineFile.BlobCommand(screenBlobId, bookmark.Screen.GetBytes()));

                            nonBlobCommands.Add(MachineFile.AddBookmarkCommand(id, ticks, bookmark.System, bookmark.Version, stateBlobId, screenBlobId));
                        }

                        break;
                    default:
                        throw new Exception("Unexpected node type!");
                }

                if (currentNode == history.CurrentEvent)
                {
                    newCurrentNodeId = currentPersistentId;
                }

                historyNodes.RemoveAt(0);
                previousNode = currentNode;

                // Place the current event's children at the top of the "stack". This effectively means we're doing a depth-first traversion of the history tree.
                historyNodes.InsertRange(0, currentNode.Children);
            }

            if (newCurrentNodeId != null)
            {
                nonBlobCommands.Add(MachineFile.CurrentCommand(newCurrentNodeId.Value));
            }

            // Write the file.
            if (blobCommands.Count > 0)
            {
                // Create a "compound" command for all the blobs.
                string compoundCommand = MachineFile.CompoundCommand(blobCommands, true);

                nonBlobCommands.Insert(0, compoundCommand);
            }

            System.IO.File.WriteAllLines(filepath, nonBlobCommands);
        }

        private void WriteDeleteEvent(int persistentId)
        {
            WriteLine(DeleteEventCommand(persistentId));
        }

        private void WriteDeleteEventAndChildren(int persistentId)
        {
            WriteLine(DeleteEventAndChildrenCommand(persistentId));
        }

        private void WriteCurrent(int persistentId)
        {
            WriteLine(CurrentCommand(persistentId));
        }

        private void WriteCurrentRoot()
        {
            WriteLine(CurrentRootCommand());
        }

        public void ReadFile(out string name, out MachineHistory history)
        {
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

                ReadLine(line, blobs, ref name);
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

        private void ReadLine(string line, Dictionary<int, IBlob> blobs, ref string name)
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
                    ReadCurrent(args);
                    break;
                case _idAddBookmark:
                    ReadAddBookmark(args, blobs);
                    break;
                case _idDeleteEvent:
                    ReadDeleteEvent(args);
                    break;
                case _idDeleteEventAndChildren:
                    ReadDeleteEventAndChildren(args);
                    break;
                case _idKey:
                    ReadKey(args);
                    break;
                case _idReset:
                    ReadReset(args);
                    break;
                case _idLoadDisc:
                    ReadLoadDisc(args, blobs);
                    break;
                case _idLoadTape:
                    ReadLoadTape(args, blobs);
                    break;
                case _idVersion:
                    ReadVersion(args);
                    break;
                case _idRunUntil:
                    ReadRunUntil(args);
                    break;
                case _idBlob:
                    ReadBlob(args, blobs);
                    break;
                case _idCompound:
                    ReadCompoundCommand(args, blobs, ref name);
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

        private void WriteAddBookmark(int id, UInt64 ticks, Bookmark bookmark)
        {
            int stateBlobId = WriteBlob(bookmark.State.GetBytes());
            int screenBlobId = WriteBlob(bookmark.Screen.GetBytes());

            string str = AddBookmarkCommand(
                id,
                ticks,
                bookmark.System,
                bookmark.Version,
                stateBlobId,
                screenBlobId);

            WriteLine(str);
        }

        private void ReadCompoundCommand(string line, Dictionary<int, IBlob> blobs, ref string name)
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
                ReadLine(command, blobs, ref name);
            }
        }

        private void ReadAddBookmark(string line, Dictionary<int, IBlob> blobs)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            bool system = Convert.ToBoolean(tokens[2]);
            int version = Convert.ToInt32(tokens[3]);
            int stateBlobId = Convert.ToInt32(tokens[4]);
            int screenBlobId = Convert.ToInt32(tokens[5]);
            IBlob stateBlob = blobs[stateBlobId]; //  new MemoryBlob(Helpers.BytesFromStr(tokens[4]));
            IBlob screenBlob = blobs[screenBlobId]; //  new MemoryBlob(Helpers.BytesFromStr(tokens[5]));

            Bookmark bookmark = new Bookmark(system, version, stateBlob, screenBlob);

            HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark);

            _historyEventToId[historyEvent] = id;
            _idToHistoryEvent[id] = historyEvent;

            _nextPersistentId = Math.Max(_nextPersistentId, id + 1);
        }

        private void ReadCurrent(string line)
        {
            string[] tokens = line.Split(',');

            if (tokens[0] == "root")
            {
                _machineHistory.SetCurrent(_machineHistory.RootEvent);
            }
            else
            {
                int id = Convert.ToInt32(tokens[0]);

                if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent newId))
                {
                    _machineHistory.SetCurrent(newId);
                }
                else
                {
                    throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
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
                    WriteRunUntil(id, ticks, action.StopTicks);
                    break;
                default:
                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", action.Type), "type");
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

        private void ReadLoadDisc(string line, Dictionary<int, IBlob> blobs)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            byte drive = Convert.ToByte(tokens[2]);
            int mediaBlobId = Convert.ToInt32(tokens[3]);
            IBlob mediaBlob = blobs[mediaBlobId];

            CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);

            HistoryEvent historyEvent = AddCoreAction(id, action);
        }

        private void WriteLoadDisc(int id, UInt64 ticks, byte drive, byte[] media)
        {
            int mediaBlobId = WriteBlob(media);

            string str = String.Format("disc:{0},{1},{2},{3}",
                id,
                ticks,
                drive,
                mediaBlobId
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

        private void ReadLoadTape(string line, Dictionary<int, IBlob> blobs)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            int mediaBlobId = Convert.ToInt32(tokens[2]);
            IBlob mediaBlob = blobs[mediaBlobId];
            //IBlob mediaBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[2]));

            CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);

            HistoryEvent historyEvent = AddCoreAction(id, action);
        }

        private void WriteLoadTape(int id, UInt64 ticks, byte[] media)
        {
            int mediaBlobId = WriteBlob(media);

            string str = String.Format("tape:{0},{1},{2}",
                id,
                ticks,
                mediaBlobId
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
                throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
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
                throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
            }
        }
    }
}
