using System;

namespace CPvC
{
    /// <summary>
    /// Class for reading and writing events to a file.
    /// </summary>
    public class MachineFile
    {
        private readonly IFile _file;

        public const string _checkpointToken = "checkpoint";
        public const string _deleteToken = "delete";
        public const string _currentToken = "current";
        public const string _bookmarkToken = "bookmark";
        public const string _keyToken = "key";
        public const string _discToken = "disc";
        public const string _tapeToken = "tape";
        public const string _resetToken = "reset";
        public const string _nameToken = "name";

        public MachineFile(IFile file)
        {
            _file = file;
        }

        public void Close()
        {
            _file.Close();
        }

        private void WriteLine(string line)
        {
            _file.WriteLine(line);
        }

        public void WriteName(string name)
        {
            WriteLine(NameLine(name));
        }

        public void WriteCurrentEvent(HistoryEvent historyEvent)
        {
            WriteLine(CurrentLine(historyEvent));
        }

        public void WriteDelete(HistoryEvent historyEvent)
        {
            WriteLine(DeleteLine(historyEvent));
        }

        public void WriteBookmark(HistoryEvent historyEvent, Bookmark bookmark)
        {
            WriteLine(BookmarkLine(historyEvent, bookmark));
        }

        public void WriteHistoryEvent(HistoryEvent historyEvent)
        {
            WriteLine(HistoryEventLine(historyEvent));
        }

        static private string NameLine(string name)
        {
            return String.Format("{0}:{1}", _nameToken, name);
        }

        static private string CurrentLine(HistoryEvent historyEvent)
        {
            return String.Format("{0}:{1}", _currentToken, historyEvent.Id);
        }

        static private string DeleteLine(HistoryEvent historyEvent)
        {
            return String.Format("{0}:{1}", _deleteToken, historyEvent.Id);
        }

        static private string BookmarkLine(HistoryEvent historyEvent, Bookmark bookmark)
        {
            if (bookmark == null)
            {
                return String.Format("{0}:{1}:0", _bookmarkToken, historyEvent.Id);
            }

            return String.Format("{0}:{1}:{2}:{3}",
                _bookmarkToken,
                historyEvent.Id,
                bookmark.System ? "1" : "2",
                Helpers.HexString(bookmark.State));
        }

        static private string HistoryEventLine(HistoryEvent historyEvent)
        {
            switch (historyEvent.Type)
            {
                case HistoryEvent.Types.CoreAction:
                    switch (historyEvent.CoreAction.Type)
                    {
                        case CoreAction.Types.KeyPress:
                            return String.Format("{0}:{1}:{2}:{3}:{4}", _keyToken, historyEvent.Id, historyEvent.Ticks, historyEvent.CoreAction.KeyCode, historyEvent.CoreAction.KeyDown ? "1" : "0");
                        case CoreAction.Types.Reset:
                            return String.Format("{0}:{1}:{2}", _resetToken, historyEvent.Id, historyEvent.Ticks);
                        case CoreAction.Types.LoadDisc:
                            return String.Format("{0}:{1}:{2}:{3}:{4}", _discToken, historyEvent.Id, historyEvent.Ticks, historyEvent.CoreAction.Drive, Helpers.HexString(historyEvent.CoreAction.MediaBuffer));
                        case CoreAction.Types.LoadTape:
                            return String.Format("{0}:{1}:{2}:{3}", _tapeToken, historyEvent.Id, historyEvent.Ticks, Helpers.HexString(historyEvent.CoreAction.MediaBuffer));
                        default:
                            throw new Exception(String.Format("Unknown CoreAction type {0}", historyEvent.CoreAction.Type));
                    }
                case HistoryEvent.Types.Checkpoint:
                    return CheckpointLine(historyEvent);
                default:
                    throw new Exception("Unknown HistoryEvent type!");
            }
        }

        static private string CheckpointLine(HistoryEvent historyEvent)
        {
            // Checkpoint format: <event id>:<ticks>:checkpoint:<bookmark type>:<core state binary>
            // <bookmark type> is one of the following:
            // 0 = No Bookmark
            // 1 = System Bookmark
            // 2 = User Bookmark
            string line;
            if (historyEvent.Bookmark == null)
            {
                line = String.Format("{0}:{1}:{2}:0:{3}",
                    _checkpointToken,
                    historyEvent.Id,
                    historyEvent.Ticks,
                    Helpers.DateTimeToNumber(historyEvent.CreateDate));
            }
            else
            {
                line = String.Format("{0}:{1}:{2}:{3}:{4}:{5}",
                    _checkpointToken,
                    historyEvent.Id,
                    historyEvent.Ticks,
                    historyEvent.Bookmark.System ? "1" : "2",
                    Helpers.DateTimeToNumber(historyEvent.CreateDate),
                    Helpers.HexString(historyEvent.Bookmark.State));
            }

            return line;
        }

        static public HistoryEvent ParseCheckpointLine(string[] tokens)
        {
            int id = Convert.ToInt32(tokens[1]);
            UInt64 ticks = Convert.ToUInt64(tokens[2]);
            DateTime createdDate = Helpers.NumberToDateTime(tokens[4]);

            string bookmarkType = tokens[3];
            switch (bookmarkType)
            {
                case "0":
                    {
                        return HistoryEvent.CreateCheckpoint(id, ticks, createdDate, null);
                    }
                case "1":
                case "2":
                    {
                        bool system = (bookmarkType == "1");
                        byte[] state = Helpers.Bytes(tokens[5]);

                        Bookmark bookmark = new Bookmark(system, state);
                        HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(id, ticks, createdDate, bookmark);

                        return historyEvent;
                    }
                default:
                    throw new Exception(String.Format("Unknown bookmark type {0}", bookmarkType));
            }
        }

        static public HistoryEvent ParseKeyPressLine(string[] tokens)
        {
            int id = Convert.ToInt32(tokens[1]);
            UInt64 ticks = Convert.ToUInt64(tokens[2]);
            byte keycode = Convert.ToByte(tokens[3]);
            bool down = (tokens[4] != "0");
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, CoreAction.KeyPress(ticks, keycode, down));

            return historyEvent;
        }

        static public HistoryEvent ParseDiscLine(string[] tokens)
        {
            int id = Convert.ToInt32(tokens[1]);
            UInt64 ticks = Convert.ToUInt64(tokens[2]);
            byte drive = Convert.ToByte(tokens[3]);
            byte[] media = Helpers.Bytes(tokens[4]);
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, CoreAction.LoadDisc(ticks, drive, media));

            return historyEvent;
        }

        static public HistoryEvent ParseTapeLine(string[] tokens)
        {
            int id = Convert.ToInt32(tokens[1]);
            UInt64 ticks = Convert.ToUInt64(tokens[2]);
            byte[] media = Helpers.Bytes(tokens[3]);
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, CoreAction.LoadTape(ticks, media));

            return historyEvent;
        }

        static public HistoryEvent ParseResetLine(string[] tokens)
        {
            int id = Convert.ToInt32(tokens[1]);
            UInt64 ticks = Convert.ToUInt64(tokens[2]);
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(id, CoreAction.Reset(ticks));

            return historyEvent;
        }

        static public Bookmark ParseBookmarkLine(string[] tokens)
        {
            Bookmark bookmark = null;
            if (tokens[2] != "0")
            {
                bool system = (tokens[2] != "2");
                byte[] state = Helpers.Bytes(tokens[3]);

                bookmark = new Bookmark(system, state);
            }

            return bookmark;
        }
    }
}
