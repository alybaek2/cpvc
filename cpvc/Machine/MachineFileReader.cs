using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineFileReader
    {
        private string _name;
        private MachineHistory _machineHistory;
        private Dictionary<int, HistoryEvent> _idToHistoryEvent;
        private int _nextLineId = 0;
        private Dictionary<int, IBlob> _blobs;

        public void ReadFile(ITextFile byteStream)
        {
            _idToHistoryEvent = new Dictionary<int, HistoryEvent>();
            _blobs = new Dictionary<int, IBlob>();

            _name = null;
            _machineHistory = new MachineHistory();
            _idToHistoryEvent[MachineHistory.RootId] = _machineHistory.RootEvent;

            while (true)
            {
                string line = byteStream.ReadLine();
                if (line == null)
                {
                    break;
                }

                ReadLine(line);
            }
        }

        public int NextLineId
        {
            get
            {
                return _nextLineId;
            }
        }

        public MachineHistory History
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

            string name = tokens[0];

            return name;
        }

        public void ReadBlob(string args)
        {
            string[] tokens = args.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            IBlob blob = new MemoryBlob(Helpers.BytesFromStr(tokens[1]));

            _blobs[id] = blob;

            _nextLineId = Math.Max(_nextLineId, id) + 1;
        }

        private void ReadCompoundCommand(string line)
        {
            string[] tokens = line.Split(',');

            int compress = Convert.ToInt32(tokens[0]);

            string commands = tokens[1];
            if (compress == 1)
            {
                byte[] bytes = Helpers.Uncompress(Helpers.BytesFromStr(commands));

                commands = Encoding.UTF8.GetString(bytes);
            }

            foreach (string command in commands.Split('@'))
            {
                ReadLine(command);
            }
        }

        private void ReadAddBookmark(string line)
        {
            string[] tokens = line.Split(',');

            int id = Convert.ToInt32(tokens[0]);

            UInt64 ticks = Convert.ToUInt64(tokens[1]);
            bool system = Convert.ToBoolean(tokens[2]);
            int version = Convert.ToInt32(tokens[3]);
            int stateBlobId = Convert.ToInt32(tokens[4]);
            int screenBlobId = Convert.ToInt32(tokens[5]);
            IBlob stateBlob = _blobs[stateBlobId];
            IBlob screenBlob = _blobs[screenBlobId];

            Bookmark bookmark = new Bookmark(system, version, stateBlob, screenBlob);

            HistoryEvent historyEvent = _machineHistory.AddBookmark(ticks, bookmark, id);

            _idToHistoryEvent[id] = historyEvent;

            _nextLineId = Math.Max(_nextLineId, id + 1);
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
            int mediaBlobId = Convert.ToInt32(tokens[3]);
            IBlob mediaBlob = _blobs[mediaBlobId];

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
            int mediaBlobId = Convert.ToInt32(tokens[2]);
            IBlob mediaBlob = _blobs[mediaBlobId];

            CoreAction action = CoreAction.LoadTape(ticks, mediaBlob);

            AddCoreAction(id, action);
        }

        private HistoryEvent AddCoreAction(int id, CoreAction coreAction)
        {
            HistoryEvent historyEvent = _machineHistory.AddCoreAction(coreAction, id);
            _idToHistoryEvent[id] = historyEvent;

            _nextLineId = Math.Max(_nextLineId, id + 1);

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

        private void ReadLine(string line)
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
                case MachineFileWriter._idName:
                    _name = ReadName(args);
                    break;
                case MachineFileWriter._idCurrent:
                    ReadCurrent(args);
                    break;
                case MachineFileWriter._idAddBookmark:
                    ReadAddBookmark(args);
                    break;
                case MachineFileWriter._idDeleteEvent:
                    ReadDeleteEvent(args);
                    break;
                case MachineFileWriter._idDeleteEventAndChildren:
                    ReadDeleteEventAndChildren(args);
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
                case MachineFileWriter._idBlob:
                    ReadBlob(args);
                    break;
                case MachineFileWriter._idCompound:
                    ReadCompoundCommand(args);
                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown type {0}.", type), "type");
            }
        }
    }
}
