using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CPvC
{
    /// <summary>
    /// Encapsulates the logic needed by the main window.
    /// </summary>
    /// <remarks>
    /// Most of these methods wrap methods on either Machine or MainWindow. See comments on those classes for more information.
    /// </remarks>
    public class MainWindowLogic : INotifyPropertyChanged
    {
        private readonly IUserInterface _userInterface;
        private readonly IFileSystem _fileSystem;
        private readonly ISettings _settings;

        private object _active;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowLogic(IUserInterface userInterface, IFileSystem fileSystem, ISettings settings)
        {
            Machines = new ObservableCollection<Machine>();
            _userInterface = userInterface;
            _fileSystem = fileSystem;
            _settings = settings;

            RecentlyOpenedMachines = new ObservableCollection<MachineInfo>();

            string recent = _settings.RecentlyOpened;
            if (recent != null)
            {
                foreach (MachineInfo info in Helpers.SplitWithEscape(',', recent).Select(x => MachineInfo.FromString(x, _fileSystem)).Where(y => y != null))
                {
                    RecentlyOpenedMachines.Add(info);
                }
            }
        }

        public object ActiveItem
        {
            get
            {
                return _active;
            }

            set
            {
                _active = value;
                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachine");
            }
        }

        public Machine ActiveMachine
        {
            get
            {
                return _active as Machine;
            }

            set
            {
                _active = value;
                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachine");
            }
        }

        public ObservableCollection<Machine> Machines { get; }
        public ObservableCollection<MachineInfo> RecentlyOpenedMachines { get; }

        public void Key(byte key, bool down)
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Key(key, down);
            }
        }

        public void Reset()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Reset();
            }
        }

        public void Pause()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Stop();
            }
        }

        public void Resume()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Start();
            }
        }

        public void ToggleRunning(Machine machine)
        {
            if (machine != null)
            {
                machine.ToggleRunning();
            }
        }

        public void AddBookmark()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.AddBookmark(false);
            }
        }

        public void SeekToLastBookmark()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.SeekToLastBookmark();
            }
        }

        public void SelectBookmark()
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                HistoryEvent historyEvent = _userInterface.PromptForBookmark();
                if (historyEvent != null)
                {
                    ActiveMachine.SetCurrentEvent(historyEvent);
                }
            }
        }

        public void EnableTurbo(bool enabled)
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                ActiveMachine.EnableTurbo(enabled);
            }
        }

        private byte[] PromptForMedia(FileTypes type)
        {
            string expectedExt;
            switch (type)
            {
                case FileTypes.Disc:
                    expectedExt = ".dsk";
                    break;
                case FileTypes.Tape:
                    expectedExt = ".cdt";
                    break;
                case FileTypes.Machine:
                    expectedExt = ".cpvc";
                    break;
                default:
                    throw new Exception(String.Format("Unknown FileTypes value {0}.", type));
            }

            string filename = _userInterface.PromptForFile(type, true);
            if (filename == null)
            {
                // Action was cancelled by the user
                return null;
            }

            byte[] buffer = null;
            string ext = System.IO.Path.GetExtension(filename);
            if (ext.ToLower() == ".zip")
            {
                string entry = null;

                List<string> entries = _fileSystem.GetZipFileEntryNames(filename);

                List<string> extEntries = entries.Where(x => System.IO.Path.GetExtension(x).ToLower() == expectedExt).ToList();
                if (extEntries.Count == 0)
                {
                    // No images available.
                    Diagnostics.Trace("No files with the extension \"{0}\" found in zip archive \"{1}\".", expectedExt, filename);

                    return null;
                }
                else if (extEntries.Count == 1)
                {
                    // Don't bother prompting the user, since there's only one!
                    entry = extEntries[0];
                }
                else
                {
                    entry = _userInterface.SelectItem(entries);
                    if (entry == null)
                    {
                        // Action was cancelled by the user.
                        return null;
                    }
                }

                Diagnostics.Trace("Loading \"{0}\" from zip archive \"{1}\"", entry, filename);
                buffer = _fileSystem.GetZipFileEntry(filename, entry);
            }
            else
            {
                Diagnostics.Trace("Loading \"{0}\"", filename);
                buffer = _fileSystem.ReadBytes(filename);
            }

            return buffer;
        }

        public void LoadDisc(byte drive)
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                byte[] image = PromptForMedia(FileTypes.Disc);
                if (image != null)
                {
                    ActiveMachine.LoadDisc(drive, image);
                }
            }
        }

        public void EjectDisc(byte drive)
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                ActiveMachine.LoadDisc(drive, null);
            }
        }

        public void LoadTape()
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                byte[] image = PromptForMedia(FileTypes.Tape);
                if (image != null)
                {
                    ActiveMachine.LoadTape(image);
                }
            }
        }

        public void EjectTape()
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                ActiveMachine.LoadTape(null);
            }
        }

        public void CompactFile()
        {
            if (ActiveMachine == null)
            {
                return;
            }

            ActiveMachine.RewriteMachineFile();
        }

        private void AddMachine(Machine machine)
        {
            lock (Machines)
            {
                Machines.Add(machine);
            }

            foreach (MachineInfo info in RecentlyOpenedMachines.Where(x => x.Filepath == machine.Filepath).ToList())
            {
                RecentlyOpenedMachines.Remove(info);
            }

            UpdateRecentlyOpened();
        }

        private void RemoveMachine(Machine machine)
        {
            lock (Machines)
            {
                Machines.Remove(machine);
            }
        }

        public void NewMachine()
        {
            string filepath = _userInterface.PromptForFile(FileTypes.Machine, false);
            if (filepath == null)
            {
                return;
            }

            string machineName = System.IO.Path.GetFileNameWithoutExtension(filepath);

            Machine machine = null;

            try
            {
                machine = Machine.New(machineName, filepath, _fileSystem);
            }
            catch (Exception ex)
            {
                string msg = String.Format("An error occurred while creating a new instance:\n\n{0}", ex.Message);
                _userInterface.ReportError(msg);

                if (machine != null)
                {
                    machine.Dispose();
                }

                return;
            }

            AddMachine(machine);
            machine.Start();
        }

        public void OpenMachine(string filepath)
        {
            if (filepath == null)
            {
                filepath = _userInterface.PromptForFile(FileTypes.Machine, true);
            }

            if (filepath == null)
            {
                return;
            }

            Machine machine = null;

            try
            {
                machine = Machine.Open(filepath, _fileSystem);
            }
            catch (Exception ex)
            {
                string msg = String.Format("An error occurred while opening an existing instance:\n\n{0}", ex.Message);
                _userInterface.ReportError(msg);

                if (machine != null)
                {
                    machine.Dispose();
                }

                return;
            }

            AddMachine(machine);
        }

        public void Close()
        {
            Close(ActiveMachine);
        }

        public void CloseAll()
        {
            // Make a copy of Machines since Close will be removing elements from it.
            foreach (Machine machine in Machines.ToList())
            {
                Close(machine);
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (Machines)
            {
                int samplesWritten = 0;
                foreach (Machine machine in Machines)
                {
                    // Play audio only from the currently selected machine; for the rest, just
                    // advance the audio playback position.
                    if (machine == ActiveMachine)
                    {
                        samplesWritten = machine.ReadAudio(buffer, offset, samplesRequested);
                    }
                    else
                    {
                        machine.AdvancePlayback(samplesRequested);
                    }
                }

                return samplesWritten;
            }
        }

        private void UpdateRecentlyOpened()
        {
            _settings.RecentlyOpened = Helpers.JoinWithEscape(',', RecentlyOpenedMachines.Select(x => x.AsString()).ToList());
        }

        private void Close(Machine machine)
        {
            if (machine != null)
            {
                RecentlyOpenedMachines.Add(new MachineInfo(machine));
                UpdateRecentlyOpened();

                machine.Close();
                RemoveMachine(machine);
            }
        }
    }
}
