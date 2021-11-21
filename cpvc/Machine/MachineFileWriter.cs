using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CPvC
{
    public class MachineFileWriter : TextFile
    {
        public const string _idName = "name";
        public const string _idKey = "key";
        public const string _idReset = "reset";
        public const string _idLoadDisc = "disc";
        public const string _idLoadTape = "tape";
        public const string _idDeleteEventAndChildren = "deletewithchildren";
        public const string _idCurrent = "current";
        public const string _idAddBookmark = "bookmark";
        public const string _idVersion = "version";
        public const string _idRunUntil = "run";
        public const string _idDeleteEvent = "delete";
        public const string _idBlob = "blob";
        public const string _idCompound = "compound";

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

        public override void Close()
        {
            Machine = null;

            if (_machineHistory != null)
            {
                _machineHistory.Auditors -= HistoryEventHappened;
            }
            _machineHistory = null;

            base.Close();
        }

        public MachineFileWriter(IFileByteStream byteStream) : base(byteStream)
        {
            _historyEventToId = new Dictionary<HistoryEvent, int>();
            _nextPersistentId = 0;
            _nextBlob = 0;
        }

        public MachineFileWriter(IFileByteStream byteStream, MachineHistory machineHistory) : base(byteStream)
        {
            _historyEventToId = new Dictionary<HistoryEvent, int>();
            _nextPersistentId = 0;
            _nextBlob = 0;

            _machineHistory = machineHistory;
            if (_machineHistory != null)
            {
                _machineHistory.Auditors += HistoryEventHappened;
            }
        }

        public MachineFileWriter(IFileByteStream byteStream, MachineHistory machineHistory, Dictionary<HistoryEvent, int> historyEventToId, int nextPersistentId, int nextBlobId) : base(byteStream)
        {
            _historyEventToId = historyEventToId ?? throw new ArgumentException("HistoryEvent to id map cannot be null!", nameof(historyEventToId));
            _nextBlob = nextBlobId;
            _nextPersistentId = nextPersistentId;

            _machineHistory = machineHistory;
            if (_machineHistory != null)
            {
                _machineHistory.Auditors += HistoryEventHappened;
            }
        }

        private void HistoryEventHappened(HistoryEvent historyEvent, UInt64 ticks, HistoryChangedAction changeAction, CoreAction coreAction, Bookmark bookmark)
        {
            List<string> lines = GetLines(historyEvent, changeAction);

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

        private int GetLines(Bookmark bookmark, UInt64 ticks, int lineId, List<string> lines)
        {
            int stateBlobId = _nextBlob++;
            lines.Add(MachineFileWriter.BlobCommand(stateBlobId, bookmark.State.GetBytes()));

            int screenBlobId = _nextBlob++;
            lines.Add(MachineFileWriter.BlobCommand(screenBlobId, bookmark.Screen.GetBytes()));

            lines.Add(MachineFileWriter.AddBookmarkCommand(lineId, ticks, bookmark.System, bookmark.Version, stateBlobId, screenBlobId));

            return lineId;
        }

        private int GetLines(CoreAction action, int lineId, List<string> lines)
        {
            //int lineId = nextLineId++;

            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    lines.Add(MachineFileWriter.KeyCommand(lineId, action.Ticks, action.KeyCode, action.KeyDown));
                    break;
                case CoreRequest.Types.Reset:
                    lines.Add(MachineFileWriter.ResetCommand(lineId, action.Ticks));
                    break;
                case CoreRequest.Types.LoadDisc:
                    {
                        int mediaBlobId = _nextBlob++;
                        lines.Add(MachineFileWriter.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));
                        lines.Add(MachineFileWriter.LoadDiscCommand(lineId, action.Ticks, action.Drive, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.LoadTape:
                    {
                        int mediaBlobId = _nextBlob++;
                        lines.Add(MachineFileWriter.BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));
                        lines.Add(MachineFileWriter.LoadTapeCommand(lineId, action.Ticks, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.CoreVersion:
                    lines.Add(MachineFileWriter.VersionCommand(lineId, action.Ticks, action.Version));
                    break;
                case CoreRequest.Types.RunUntil:
                    lines.Add(MachineFileWriter.RunCommand(lineId, action.Ticks, action.StopTicks));
                    break;
                default:
                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", action.Type), "type");
            }

            return lineId;
        }

        private void GetLines(HistoryEvent historyEvent, int lineId, List<string> lines)
        {
            switch (historyEvent.Type)
            {
                case HistoryEventType.Bookmark:
                    GetLines(historyEvent.Bookmark, historyEvent.Ticks, lineId, lines);
                    break;
                case HistoryEventType.CoreAction:
                    GetLines(historyEvent.CoreAction, lineId, lines);
                    break;
                default:
                    throw new ArgumentException("Unknown history event type!", "historyEvent.Type");
            }
        }

        private List<string> GetLines(HistoryEvent historyEvent, HistoryChangedAction changeType)
        {
            List<string> lines = new List<string>();

            switch (changeType)
            {
                case HistoryChangedAction.Add:
                    {
                        int lineId = _nextPersistentId++;

                        GetLines(historyEvent, lineId, lines);

                        _historyEventToId[historyEvent] = lineId;
                    }
                    break;
                case HistoryChangedAction.Delete:
                    {
                        int lineId = _historyEventToId[historyEvent];

                        lines.Add(DeleteEventCommand(lineId));

                        // Should we remove the id from the maps? Or should history events and blobs have Id's built into them?
                    }
                    break;
                case HistoryChangedAction.DeleteRecursive:
                    {
                        int lineId = _historyEventToId[historyEvent];

                        lines.Add(DeleteEventAndChildrenCommand(lineId));

                        // Should we remove the id from the maps? Or should history events and blobs have Id's built into them?
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
                            int lineId = _historyEventToId[historyEvent];

                            lines.Add(CurrentCommand(lineId));
                        }
                    }
                    break;
                default:
                    throw new ArgumentException("Unknown history action type!", nameof(changeType));
            }

            return lines;
        }

        public void WriteHistory(string name)
        {
            List<string> lines = new List<string>();

            lines.Add(NameCommand(name));

            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();
            historyEvents.AddRange(_machineHistory.RootEvent.Children);

            int? newCurrenEventId = null;

            HistoryEvent previousEvent = null;
            while (historyEvents.Count > 0)
            {
                int currentLineId = _nextPersistentId;
                HistoryEvent currentEvent = historyEvents[0];
                _historyEventToId[currentEvent] = currentLineId;
                _nextPersistentId++;

                if (previousEvent != currentEvent.Parent && previousEvent != null)
                {
                    // Todo: should use current:root for when currentEvent.Parent is the root!
                    if (currentEvent.Parent == _machineHistory.RootEvent)
                    {
                        lines.Add(MachineFileWriter.CurrentRootCommand());
                    }
                    else
                    {
                        lines.Add(MachineFileWriter.CurrentCommand(_historyEventToId[currentEvent.Parent]));
                    }
                }

                GetLines(currentEvent, currentLineId, lines);

                //switch (currentEvent.Type)
                //{
                //    case HistoryEventType.CoreAction:
                //        GetLines(currentEvent.CoreAction, currentLineId, ref _nextBlob, lines);
                //        break;
                //    case HistoryEventType.Bookmark:
                //        GetLines(currentEvent.Bookmark, currentEvent.Ticks, currentLineId, ref _nextBlob, lines);
                //        break;
                //    default:
                //        throw new Exception("Unexpected node type!");
                //}

                if (currentEvent == _machineHistory.CurrentEvent)
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
                lines.Add(MachineFileWriter.CurrentCommand(newCurrenEventId.Value));
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
                string compoundCommand = MachineFileWriter.CompoundCommand(blobCommands, true);

                lines.Insert(0, compoundCommand);
            }

            foreach (string line in lines)
            {
                WriteLine(line);
            }
        }

        private void Machine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Name")
            {
                WriteName(_machine.Name);
            }
        }

        public int WriteBlob(byte[] blob)
        {
            int blobId = _nextBlob++;
            WriteLine(BlobCommand(blobId, blob));

            return blobId;
        }

        public void WriteName(string name)
        {
            WriteLine(NameCommand(name));
        }
    }
}
