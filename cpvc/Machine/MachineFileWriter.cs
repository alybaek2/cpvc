using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CPvC
{
    public class MachineFileWriter : IDisposable
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
        public const string _idBlob = "blob";
        public const string _idCompound = "compound";

        private MachineHistory _machineHistory;
        private int _nextBlobId;

        private LocalMachine _machine;

        private ITextFile _textFile;

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

        private MachineHistory History
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

        public MachineFileWriter(ITextFile textFile, MachineHistory machineHistory, int nextBlobId)
        {
            _textFile = textFile ?? throw new ArgumentException("Need a text file to write to!", nameof(textFile));
            _nextBlobId = nextBlobId;

            History = machineHistory;
        }

        public MachineFileWriter(ITextFile textFile, MachineHistory machineHistory) : this(textFile, machineHistory, 0)
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

        static private string AddBookmarkCommand(int id, UInt64 ticks, bool system, int version, int stateBlobId, int screenBlobId)
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

        static private string KeyCommand(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            return String.Format("{0}:{1},{2},{3},{4}",
                _idKey,
                id,
                ticks,
                keyCode,
                keyDown);
        }

        static private string LoadDiscCommand(int id, UInt64 ticks, byte drive, int mediaBlobId)
        {
            return String.Format("{0}:{1},{2},{3},{4}",
                _idLoadDisc,
                id,
                ticks,
                drive,
                mediaBlobId);
        }

        static private string LoadTapeCommand(int id, UInt64 ticks, int mediaBlobId)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idLoadTape,
                id,
                ticks,
                mediaBlobId);
        }

        static private string RunCommand(int id, UInt64 ticks, UInt64 stopTicks)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idRunUntil,
                id,
                ticks,
                stopTicks);
        }

        static private string ResetCommand(int id, UInt64 ticks)
        {
            return String.Format("{0}:{1},{2}",
                _idReset,
                id,
                ticks);
        }

        static private string VersionCommand(int id, UInt64 ticks, int version)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idVersion,
                id,
                ticks,
                version);
        }

        static private string BlobCommand(int blobId, byte[] blob)
        {
            return String.Format(
                "{0}:{1},{2}",
                _idBlob,
                blobId,
                Helpers.StrFromBytes(blob));
        }

        static private string CompoundCommand(IEnumerable<string> commands)
        {
            string str = String.Join("@", commands);

            byte[] b = Encoding.UTF8.GetBytes(str);
            str = Helpers.StrFromBytes(Helpers.Compress(b));

            return String.Format(
                "{0}:{1},{2}",
                _idCompound,
                1,
                str);
        }

        private void GetLines(int id, CoreAction action, List<string> lines)
        {
            switch (action.Type)
            {
                case CoreRequest.Types.KeyPress:
                    lines.Add(KeyCommand(id, action.Ticks, action.KeyCode, action.KeyDown));
                    break;
                case CoreRequest.Types.Reset:
                    lines.Add(ResetCommand(id, action.Ticks));
                    break;
                case CoreRequest.Types.LoadDisc:
                    {
                        int mediaBlobId = _nextBlobId++;
                        lines.Add(BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));
                        lines.Add(LoadDiscCommand(id, action.Ticks, action.Drive, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.LoadTape:
                    {
                        int mediaBlobId = _nextBlobId++;
                        lines.Add(BlobCommand(mediaBlobId, action.MediaBuffer.GetBytes()));
                        lines.Add(LoadTapeCommand(id, action.Ticks, mediaBlobId));
                    }
                    break;
                case CoreRequest.Types.CoreVersion:
                    lines.Add(VersionCommand(id, action.Ticks, action.Version));
                    break;
                case CoreRequest.Types.RunUntil:
                    lines.Add(RunCommand(id, action.Ticks, action.StopTicks));
                    break;
                default:
                    throw new ArgumentException(String.Format("Unrecognized core action type {0}.", action.Type), "type");
            }
        }

        private void GetLines(HistoryEvent historyEvent, List<string> lines)
        {
            switch (historyEvent.Type)
            {
                case HistoryEventType.Bookmark:
                    {
                        Bookmark bookmark = historyEvent.Bookmark;

                        int stateBlobId = _nextBlobId++;
                        lines.Add(BlobCommand(stateBlobId, bookmark.State.GetBytes()));

                        int screenBlobId = _nextBlobId++;
                        lines.Add(BlobCommand(screenBlobId, bookmark.Screen.GetBytes()));

                        lines.Add(AddBookmarkCommand(historyEvent.Id, historyEvent.Ticks, bookmark.System, bookmark.Version, stateBlobId, screenBlobId));
                    }
                    break;
                case HistoryEventType.CoreAction:
                    GetLines(historyEvent.Id, historyEvent.CoreAction, lines);
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

                GetLines(currentEvent, lines);

                historyEvents.RemoveAt(0);
                previousEvent = currentEvent;

                // Place the current event's children at the top of the "stack". This effectively means we're doing a depth-first traversion of the history tree.
                historyEvents.InsertRange(0, currentEvent.Children);
            }

            lines.Add(CurrentCommand(History.CurrentEvent.Id));

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
                string compoundCommand = CompoundCommand(blobCommands);

                lines.Insert(0, compoundCommand);
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
    }
}
