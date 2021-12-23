using System;
using System.Collections.Generic;
using System.Linq;
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
        public const string _idArg = "arg";
        public const string _idArgs = "args";

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

        static public string AddBookmarkCommand(int id, UInt64 ticks, bool system, int version, byte[] state, byte[] screen)
        {
            return String.Format(
                "{0}:{1},{2},{3},{4},{5},{6}",
                _idAddBookmark,
                id,
                ticks,
                system,
                version,
                Helpers.StrFromBytes(state),
                Helpers.StrFromBytes(screen));
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

        static public string LoadDiscCommand(int id, UInt64 ticks, byte drive, byte[] media)
        {
            return String.Format("{0}:{1},{2},{3},{4}",
                _idLoadDisc,
                id,
                ticks,
                drive,
                Helpers.StrFromBytes(media));
        }

        static public string LoadTapeCommand(int id, UInt64 ticks, byte[] media)
        {
            return String.Format("{0}:{1},{2},{3}",
                _idLoadTape,
                id,
                ticks,
                Helpers.StrFromBytes(media));
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

        static private string ArgCommand(int argId, string argValue, bool compress)
        {
            if (compress)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(argValue);
                argValue = Helpers.StrFromBytes(Helpers.Compress(bytes));
            }

            return String.Format("{0}:{1},{2},{3}", _idArg, argId, compress, argValue);
        }

        static public string ArgsCommand(Dictionary<int, string> args, bool compress)
        {
            string argsLine = String.Join("@", args.Keys.Select(key => String.Format("{0}#{1}", key, args[key])));
            if (compress)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(argsLine);
                argsLine = Helpers.StrFromBytes(Helpers.Compress(bytes));
            }

            return String.Format("{0}:{1},{2}", _idArgs, compress, argsLine);
        }

        private void GetLines(HistoryEvent historyEvent, List<string> lines)
        {
            string line = historyEvent.GetLine();

            // Check for big parameters
            string[] args = line.Split(',');
            for (int i = 0; i < args.Length; i++)
            {
                string token = args[i];
                if (token.Length > 100)
                {
                    int blobId = _nextBlobId++;
                    lines.Add(ArgCommand(blobId, token, true));
                    args[i] = String.Format("${0}", blobId);
                }
            }

            lines.Add(String.Join(",", args));
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

            // If we have any "arg" commands, stick them in a "args" command and put them at the start of the file.
            // Putting them in a single command should allow them to be better compressed than individually.
            Dictionary<int, string> args = new Dictionary<int, string>();
            int i = 0;
            while (i < lines.Count)
            {
                string[] tokens = lines[i].Split(':');
                if (tokens[0] == _idArg)
                {
                    lines.RemoveAt(i);

                    MachineFileReader.ReadArgCommand(tokens[1], args);
                }
                else
                {
                    i++;
                }
            }

            // Write the file.
            if (args.Count > 0)
            {
                lines.Insert(0, ArgsCommand(args, true));
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
