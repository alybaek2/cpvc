using System;
using System.Collections.Generic;
using System.Text;

namespace CPvC
{
    public class MachineFileReader
    {
        private string _name;
        private History _machineHistory;
        private Dictionary<int, HistoryEvent> _idToHistoryEvent;
        private int _nextLineId = 0;
        private Dictionary<int, string> _args;

        public void ReadFile(ITextFile byteStream)
        {
            _idToHistoryEvent = new Dictionary<int, HistoryEvent>();
            _args = new Dictionary<int, string>();

            _name = null;
            _machineHistory = new History();
            _idToHistoryEvent[_machineHistory.RootEvent.Id] = _machineHistory.RootEvent;

            string line;
            while ((line = byteStream.ReadLine()) != null)
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

        static public void ReadArgCommand(string line, Dictionary<int, string> args)
        {
            string[] tokens = line.Split(',');
            int argId = Convert.ToInt32(tokens[0]);
            bool compress = Convert.ToBoolean(tokens[1]);
            string argValue = tokens[2];
            if (compress)
            {
                byte[] bytes = Helpers.Uncompress(Helpers.BytesFromStr(argValue));

                argValue = Encoding.UTF8.GetString(bytes);
            }

            args[argId] = argValue;
        }

        public void ReadArgsCommand(string line, Dictionary<int, string> args)
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

                args[argId] = argValue;
            }
        }

        private void ReadAddBookmark(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            bool system = Convert.ToBoolean(tokens[2]);
            int version = Convert.ToInt32(tokens[3]);
            byte[] state = Helpers.BytesFromStr(tokens[4]);
            byte[] screen = Helpers.BytesFromStr(tokens[5]);

            Bookmark bookmark = new Bookmark(system, version, state, screen);

            HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark);

            _idToHistoryEvent[id] = historyEvent;

            _nextLineId = Math.Max(_nextLineId, id + 1);
        }

        private void ReadCurrent(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            if (_idToHistoryEvent.TryGetValue(id, out HistoryEvent historyEvent))
            {
                _machineHistory.SetCurrent(historyEvent);
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

            CoreAction action = CoreAction.KeyPress(ticks, keyCode, keyDown);

            AddCoreAction(id, action);
        }

        private void ReadReset(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            CoreAction action = CoreAction.Reset(ticks);

            AddCoreAction(id, action);
        }

        private void ReadLoadDisc(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            byte drive = Convert.ToByte(tokens[2]);
            IBlob mediaBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[3]));

            CoreAction action = CoreAction.LoadDisc(ticks, drive, mediaBlob);

            AddCoreAction(id, action);
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

        private void ReadLoadTape(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);
            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            IBlob mediaBlob = new MemoryBlob(Helpers.BytesFromStr(tokens[2]));

            CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);

            AddCoreAction(id, action);
        }

        private HistoryEvent AddCoreAction(int id, CoreAction coreAction)
        {
            HistoryEvent historyEvent = _machineHistory.AddCoreAction(coreAction);
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
                    throw new InvalidOperationException("Couldn't delete history event!");
                }
            }
            else
            {
                throw new ArgumentException(String.Format("Unknown history node id {0}.", id), "id");
            }
        }

        private void ProcessLine(string line)
        {
            // Check for large arguments
            string[] tokens = line.Split(',');
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].StartsWith("$"))
                {
                    int argId = System.Convert.ToInt32(tokens[i].Substring(1));
                    tokens[i] = _args[argId];
                }
            }

            line = String.Join(",", tokens);

            int colon = line.IndexOf(':');
            if (colon == -1)
            {
                throw new Exception(String.Format("No colon found in line {0}", line));
            }

            string type = line.Substring(0, colon);
            string args = line.Substring(colon + 1);

            switch (type)
            {
                case MachineFileWriter._idName:
                    _name = ReadName(args);
                    break;
                case MachineFileWriter._idCurrent:
                    ReadCurrent(args);
                    break;
                case MachineFileWriter._idAddBookmark:
                    ReadAddBookmark(args);
                    break;
                case MachineFileWriter._idDeleteBookmark:
                    ReadDeleteBookmark(args);
                    break;
                case MachineFileWriter._idDeleteBranch:
                    ReadDeleteBranch(args);
                    break;
                case MachineFileWriter._idKey:
                    ReadKey(args);
                    break;
                case MachineFileWriter._idReset:
                    ReadReset(args);
                    break;
                case MachineFileWriter._idLoadDisc:
                    ReadLoadDisc(args);
                    break;
                case MachineFileWriter._idLoadTape:
                    ReadLoadTape(args);
                    break;
                case MachineFileWriter._idVersion:
                    ReadVersion(args);
                    break;
                case MachineFileWriter._idRunUntil:
                    ReadRunUntil(args);
                    break;
                case MachineFileWriter._idArg:
                    ReadArgCommand(args, _args);
                    break;
                case MachineFileWriter._idArgs:
                    ReadArgsCommand(args, _args);
                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown type {0}.", type), "type");
            }
        }
    }
}
