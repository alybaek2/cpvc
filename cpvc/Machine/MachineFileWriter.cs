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
        public const string _idDeleteEventAndChildren = "deletewithchildren";
        public const string _idCurrent = "current";
        public const string _idAddBookmark = "bookmark";
        public const string _idVersion = "version";
        public const string _idRunUntil = "run";
        public const string _idDeleteEvent = "delete";
        public const string _idBlob = "blob";
        public const string _idCompound = "compound";

        private MachineHistory _machineHistory;
        private int _nextHistoryEventId;
        private int _nextBlob;

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

        public void Dispose()
        {
            Machine = null;

            if (_machineHistory != null)
            {
                _machineHistory.Auditors -= HistoryEventHappened;
            }
            _machineHistory = null;

            if (_textFile != null)
            {
                _textFile.Dispose();
                _textFile = null;
            }
        }

        public MachineFileWriter(ITextFile textFile)
        {
            _textFile = textFile;
            _nextHistoryEventId = 0;
            _nextBlob = 0;
        }

        public MachineFileWriter(ITextFile textFile, MachineHistory machineHistory)
        {
            _textFile = textFile;
            _nextHistoryEventId = 0;
            _nextBlob = 0;

            _machineHistory = machineHistory;
            if (_machineHistory != null)
            {
                _machineHistory.Auditors += HistoryEventHappened;
            }
        }

        public MachineFileWriter(ITextFile textFile, MachineHistory machineHistory, int nextHistoryEventId, int nextBlobId)
        {
            _textFile = textFile;
            _nextBlob = nextBlobId;
            _nextHistoryEventId = nextHistoryEventId;

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
                _textFile.WriteLine(line);
            }
        }

        static private string DeleteEventCommand(int historyEventId)
        {
            return String.Format("{0}:{1}", _idDeleteEvent, historyEventId);
        }

        static private string DeleteEventAndChildrenCommand(int historyEventId)
        {
            return String.Format("{0}:{1}", _idDeleteEventAndChildren, historyEventId);
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

        static private string CompoundCommand(IEnumerable<string> commands, bool compress)
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
                        int lineId = _nextHistoryEventId++;

                        GetLines(historyEvent, lineId, lines);
                    }
                    break;
                case HistoryChangedAction.Delete:
                    lines.Add(DeleteEventCommand(historyEvent.Id));
                    break;
                case HistoryChangedAction.DeleteRecursive:
                    lines.Add(DeleteEventAndChildrenCommand(historyEvent.Id));
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
            List<string> lines = new List<string>();

            lines.Add(NameCommand(name));

            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();
            historyEvents.AddRange(_machineHistory.RootEvent.Children);

            int? newCurrenEventId = null;

            HistoryEvent previousEvent = null;
            while (historyEvents.Count > 0)
            {
                int currentLineId = _nextHistoryEventId;
                HistoryEvent currentEvent = historyEvents[0];
                _nextHistoryEventId++;

                if (previousEvent != currentEvent.Parent && previousEvent != null)
                {
                    lines.Add(MachineFileWriter.CurrentCommand(currentEvent.Parent.Id));
                }

                GetLines(currentEvent, currentLineId, lines);

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
                _textFile.WriteLine(line);
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
            _textFile.WriteLine(BlobCommand(blobId, blob));

            return blobId;
        }

        public void WriteName(string name)
        {
            _textFile.WriteLine(NameCommand(name));
        }
    }
}
