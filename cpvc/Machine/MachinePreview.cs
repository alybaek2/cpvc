using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace CPvC
{
    public class MachinePreview
    {
        private Display _display;
        private string _filepath;
        private IFileSystem _fileSystem;
        private string _name;

        public Display Display
        {
            get
            {
                return _display;
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
            }
        }

        public string Filepath
        {
            get
            {
                return _filepath;
            }
        }

        public MachinePreview(string filepath, IFileSystem fileSystem)
        {
            _filepath = filepath;
            _fileSystem = fileSystem;
            _display = new Display();
        }

        public Machine Open(MachineFile machineFile)
        {
            MachineHistory history;
            string name;
            machineFile.ReadFile(out name, out history);

            Machine machine = Machine.Create(name, history);
            machineFile.Machine = machine;
            machineFile.History = machine.History;

            HistoryEvent historyEvent = MostRecentBookmark(machine.History);
            machine.SetCurrentEvent(historyEvent);

            return machine;
        }

        public void OpenPreview()
        {
            using (IFileByteStream fileByteStream = _fileSystem.OpenFileByteStream(_filepath))
            {
                MachineFile file = new MachineFile(fileByteStream);

                MachineHistory history;
                string name;
                file.ReadFile(out name, out history);

                _name = name;

                if (history != null)
                {
                    HistoryEvent historyEvent = MostRecentBookmark(history);

                    _display.GetFromBookmark(historyEvent.Bookmark);
                    _display.EnableGreyscale(true);
                }
            }
        }

        private HistoryEvent MostRecentBookmark(MachineHistory history)
        {
            HistoryEvent historyEvent = history.CurrentEvent;
            while (historyEvent.Type != HistoryEventType.AddBookmark && historyEvent != history.RootEvent)
            {
                historyEvent = historyEvent.Parent;
            }

            return historyEvent;
        }
    }
}
