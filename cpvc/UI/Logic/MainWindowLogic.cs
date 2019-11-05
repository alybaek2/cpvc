using System;
using System.Collections.Generic;
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
        private readonly List<Machine> _machines;
        private readonly IUserInterface _userInterface;
        private readonly IFileSystem _fileSystem;

        private Machine _machine;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowLogic(IUserInterface userInterface, IFileSystem fileSystem)
        {
            _machines = new List<Machine>();
            _userInterface = userInterface;
            _fileSystem = fileSystem;
        }

        public Machine Machine
        {
            get
            {
                return _machine;
            }

            set
            {
                _machine = value;
                OnPropertyChanged("Machine");
            }
        }

        public void Key(byte key, bool down)
        {
            if (Machine != null)
            {
                Machine.Key(key, down);
            }
        }

        public void Reset()
        {
            if (Machine != null)
            {
                Machine.Reset();
            }
        }

        public void Pause()
        {
            if (Machine != null)
            {
                Machine.Stop();
            }
        }

        public void Resume()
        {
            if (Machine != null)
            {
                Machine.Start();
            }
        }

        public void AddBookmark()
        {
            if (Machine != null)
            {
                Machine.AddBookmark(false);
            }
        }

        public void SeekToLastBookmark()
        {
            if (Machine != null)
            {
                Machine.SeekToLastBookmark();
            }
        }

        public void OpenProperties()
        {
            if (Machine != null)
            {
                _userInterface.OpenProperties();
            }
        }

        public void SelectBookmark()
        {
            if (Machine == null)
            {
                return;
            }

            using (Machine.AutoPause())
            {
                HistoryEvent historyEvent = _userInterface.PromptForBookmark();
                if (historyEvent != null)
                {
                    Machine.SetCurrentEvent(historyEvent);
                }
            }
        }

        public void EnableTurbo(bool enabled)
        {
            if (Machine == null)
            {
                return;
            }

            using (Machine.AutoPause())
            {
                Machine.EnableTurbo(enabled);
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
            if (Machine == null)
            {
                return;
            }

            using (Machine.AutoPause())
            {
                byte[] image = PromptForMedia(FileTypes.Disc);
                if (image != null)
                {
                    Machine.LoadDisc(drive, image);
                }
            }
        }

        public void LoadTape()
        {
            if (Machine == null)
            {
                return;
            }

            using (Machine.AutoPause())
            {
                byte[] image = PromptForMedia(FileTypes.Tape);
                if (image != null)
                {
                    Machine.LoadTape(image);
                }
            }
        }

        private void AddMachine(Machine machine)
        {
            lock (_machines)
            {
                _machines.Add(machine);
            }

            _userInterface.AddMachine(machine);
        }

        private void RemoveMachine(Machine machine)
        {
            lock (_machines)
            {
                _machines.Remove(machine);
            }

            _userInterface.RemoveMachine(Machine);
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

        public void OpenMachine()
        {
            string filepath = _userInterface.PromptForFile(FileTypes.Machine, true);
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
            if (Machine != null)
            {
                Machine.Close();

                RemoveMachine(Machine);
            }
        }

        public void CloseAll()
        {
            foreach (Machine machine in _machines)
            {
                machine.Close();
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (_machines)
            {
                int samplesWritten = 0;
                foreach (Machine machine in _machines)
                {
                    // Play audio only from the currently selected machine; for the rest, just
                    // advance the audio playback position.
                    if (machine == Machine)
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
    }
}
