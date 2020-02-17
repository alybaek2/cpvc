using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace CPvC.UI
{
    /// <summary>
    /// View Model for the main window.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Delegates for calling back into MainWindow.
        public delegate string SelectItemDelegate(List<string> items);
        public delegate string PromptForFileDelegate(FileTypes type, bool existing);
        public delegate HistoryEvent PromptForBookmarkDelegate();
        public delegate string PromptForNameDelegate(string existingName);

        ViewModelCommand _driveACommand;
        ViewModelCommand _driveAEjectCommand;
        ViewModelCommand _driveBCommand;
        ViewModelCommand _driveBEjectCommand;
        ViewModelCommand _tapeCommand;
        ViewModelCommand _tapeEjectCommand;
        ViewModelCommand _resetCommand;
        ViewModelCommand _pauseCommand;
        ViewModelCommand _resumeCommand;
        ViewModelCommand _toggleRunningCommand;
        ViewModelCommand _addBookmarkCommand;
        ViewModelCommand _seekToPreviousBookmarkCommand;
        ViewModelCommand _browseBookmarksCommand;
        ViewModelCommand _compactCommand;
        ViewModelCommand _renameCommand;

        public ICommand ResetCommand
        {
            get { return _resetCommand; }
        }

        public ICommand DriveACommand
        {
            get { return _driveACommand; }
        }

        public ICommand DriveBCommand
        {
            get { return _driveBCommand; }
        }

        public ICommand DriveAEjectCommand
        {
            get { return _driveAEjectCommand; }
        }

        public ICommand DriveBEjectCommand
        {
            get { return _driveBEjectCommand; }
        }

        public ICommand TapeCommand
        {
            get { return _tapeCommand; }
        }

        public ICommand TapeEjectCommand
        {
            get { return _tapeEjectCommand; }
        }

        public ICommand PauseCommand
        {
            get { return _pauseCommand; }
        }

        public ICommand ResumeCommand
        {
            get { return _resumeCommand; }
        }

        public ICommand ToggleRunningCommand
        {
            get { return _toggleRunningCommand; }
        }

        public ICommand AddBookmarkCommand
        {
            get { return _addBookmarkCommand; }
        }

        public ICommand SeekToPreviousBookmarkCommand
        {
            get { return _seekToPreviousBookmarkCommand; }
        }

        public ICommand BrowseBookmarksCommand
        {
            get { return _browseBookmarksCommand; }
        }

        public ICommand CompactCommand
        {
            get { return _compactCommand; }
        }

        public ICommand RenameCommand
        {
            get { return _renameCommand; }
        }

        /// <summary>
        /// The data model associated with this view model.
        /// </summary>
        private readonly MainModel _model;

        /// <summary>
        /// The currently active item. Will be either this instance (the Home tab) or a Machine.
        /// </summary>
        private object _active;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(ISettings settings, IFileSystem fileSystem, SelectItemDelegate selectItem, PromptForFileDelegate promptForFile, PromptForBookmarkDelegate promptForBookmark, PromptForNameDelegate promptForName)
        {
            _model = new MainModel(settings, fileSystem);
            ActiveItem = this;

            _resetCommand = new ViewModelCommand(
                p => (ActiveMachine as IInteractiveMachine)?.Reset(),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _driveACommand = new ViewModelCommand(
                p => LoadDisc(0, fileSystem, promptForFile, selectItem),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _driveAEjectCommand = new ViewModelCommand(
                p => EjectDisc(0),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _driveBCommand = new ViewModelCommand(
                p => LoadDisc(1, fileSystem, promptForFile, selectItem),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _driveBEjectCommand = new ViewModelCommand(
                p => EjectDisc(1),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _tapeCommand = new ViewModelCommand(
                p => LoadTape(fileSystem, promptForFile, selectItem),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _tapeEjectCommand = new ViewModelCommand(
                p => EjectTape(),
                p => (ActiveMachine as IInteractiveMachine) != null
            );

            _pauseCommand = new ViewModelCommand(
                p => Pause(null),
                p => (ActiveMachine as IPausableMachine) != null && ((ActiveMachine as Machine)?.Core?.Running ?? false)
            );

            _resumeCommand = new ViewModelCommand(
                p => Resume(null),
                p => (ActiveMachine as IPausableMachine) != null && !(((ActiveMachine as Machine)?.Core?.Running ?? true))
            );

            _toggleRunningCommand = new ViewModelCommand(
                p => ToggleRunning(p as IPausableMachine),
                p => (ActiveMachine as IPausableMachine) != null
            );

            _addBookmarkCommand = new ViewModelCommand(
                p => AddBookmark(),
                p => (ActiveMachine as IBookmarkableMachine) != null
            );

            _seekToPreviousBookmarkCommand = new ViewModelCommand(
                p => SeekToLastBookmark(),
                p => (ActiveMachine as IBookmarkableMachine) != null
            );

            _browseBookmarksCommand = new ViewModelCommand(
                p => SelectBookmark(promptForBookmark),
                p => (ActiveMachine as IBookmarkableMachine) != null
            );

            _compactCommand = new ViewModelCommand(
                p => CompactFile(),
                p => (ActiveMachine as ICompactableMachine) != null
            );

            _renameCommand = new ViewModelCommand(
                p => RenameMachine(promptForName),
                p => (ActiveMachine as ICoreMachine) != null
            );
        }

        public ObservableCollection<Machine> Machines
        {
            get
            {
                return _model.Machines;
            }
        }

        public ObservableCollection<ReplayMachine> ReplayMachines
        {
            get
            {
                return _model.ReplayMachines;
            }
        }

        /// <summary>
        /// Represents the currently active item in the main window. Corresponds to the DataContext associated with the currently
        /// selected tab in the main window.
        /// </summary>
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

        /// <summary>
        /// If the currently selected tab corresponds to a Machine, this property will be a reference to that machine. Otherwise, this property is null.
        /// </summary>
        public ICoreMachine ActiveMachine
        {
            get
            {
                return _active as ICoreMachine;
            }

            set
            {
                _active = value;

                if (_active is Machine machine && machine.RequiresOpen)
                {
                    machine.Open();
                }

                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachine");
            }
        }

        public Machine NewMachine(PromptForFileDelegate promptForFile, IFileSystem fileSystem)
        {
            string filepath = promptForFile(FileTypes.Machine, false);

            Machine machine = _model.Add(filepath, fileSystem);
            if (machine != null)
            {
                machine.Start();
                ActiveMachine = machine;
            }

            return machine;
        }

        public Machine OpenMachine(PromptForFileDelegate promptForFile, string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                filepath = promptForFile(FileTypes.Machine, true);
            }

            Machine machine = _model.Add(filepath, fileSystem);
            if (machine != null)
            {
                ActiveMachine = machine;
            }

            return machine;
        }

        private void SelectBookmark(PromptForBookmarkDelegate promptForBookmark)
        {
            Machine machine = ActiveMachine as Machine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                HistoryEvent historyEvent = promptForBookmark();
                if (historyEvent != null)
                {
                    machine.SetCurrentEvent(historyEvent);
                    machine.Status = String.Format("Jumped to {0}", Helpers.GetTimeSpanFromTicks(machine.Core.Ticks).ToString(@"hh\:mm\:ss"));
                }
            }
        }

        private void RenameMachine(PromptForNameDelegate promptForName)
        {
            Machine machine = ActiveMachine as Machine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                string newName = promptForName(machine.Name);
                if (newName != null)
                {
                    machine.Name = newName;
                }
            }
        }

        public void EnableTurbo(bool enabled)
        {
            (ActiveMachine as ITurboableMachine)?.EnableTurbo(enabled);
        }

        private void Resume(ICoreMachine machine)
        {
            ((machine ?? ActiveMachine) as IPausableMachine)?.Start();
        }

        private void Pause(ICoreMachine machine)
        {
            ((machine ?? ActiveMachine) as IPausableMachine)?.Stop();
        }

        private void Reset(ICoreMachine machine)
        {
            ((machine ?? ActiveMachine) as IInteractiveMachine)?.Reset();
        }

        public void Close(ICoreMachine machine)
        {
            ((machine ?? ActiveMachine) as IClosableMachine)?.Close();
        }

        public void Key(byte key, bool down)
        {
            (ActiveMachine as IInteractiveMachine)?.Key(key, down);
        }

        private void AddBookmark()
        {
            (ActiveMachine as Machine)?.AddBookmark(false);
        }

        private void SeekToLastBookmark()
        {
            (ActiveMachine as Machine)?.SeekToLastBookmark();
        }

        private void CompactFile()
        {
            (ActiveMachine as Machine)?.Compact();
        }

        public void Remove(Machine machine)
        {
            _model.Remove(machine);
            machine.Close();
        }

        public void CloseAll()
        {
            foreach (Machine machine in _model.Machines)
            {
                machine.Close();
            }

            foreach (ReplayMachine machine in _model.ReplayMachines)
            {
                machine.Close();
            }
        }

        private void LoadDisc(byte drive, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            Machine machine = ActiveMachine as Machine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                byte[] image = PromptForMedia(true, fileSystem, promptForFile, selectItem);
                if (image != null)
                {
                    machine.LoadDisc(drive, image);
                }
            }
        }

        private void EjectDisc(byte drive)
        {
            (ActiveMachine as IInteractiveMachine)?.LoadDisc(drive, null);
        }

        private void LoadTape(IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            Machine machine = ActiveMachine as Machine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                byte[] image = PromptForMedia(false, fileSystem, promptForFile, selectItem);
                if (image != null)
                {
                    machine.LoadTape(image);
                }
            }
        }

        private void EjectTape()
        {
            (ActiveMachine as IInteractiveMachine)?.LoadTape(null);
        }

        private void ToggleRunning(IPausableMachine machine)
        {
            machine?.ToggleRunning();
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (Machines)
            {
                int samplesWritten = 0;
                foreach (Machine machine in Machines)
                {
                    // Play audio only from the currently active machine; for the rest, just
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
                foreach (ReplayMachine replayMachine in ReplayMachines)
                {
                    // Play audio only from the currently active machine; for the rest, just
                    // advance the audio playback position.
                    if (replayMachine == ActiveItem)
                    {
                        samplesWritten = replayMachine.ReadAudio(buffer, offset, samplesRequested);
                    }
                    else
                    {
                        replayMachine.Core.AdvancePlayback(samplesRequested);
                    }
                }

                return samplesWritten;
            }
        }

        private byte[] PromptForMedia(bool disc, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            string expectedExt = disc ? ".dsk" : ".cdt";
            FileTypes type = disc ? FileTypes.Disc : FileTypes.Tape;

            string filename = promptForFile(type, true);
            if (filename == null)
            {
                // Action was cancelled by the user.
                return null;
            }

            byte[] buffer = null;
            string ext = System.IO.Path.GetExtension(filename);
            if (ext.ToLower() == ".zip")
            {
                string entry = null;
                List<string> entries = fileSystem.GetZipFileEntryNames(filename);
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
                    entry = selectItem(extEntries);
                    if (entry == null)
                    {
                        // Action was cancelled by the user.
                        return null;
                    }
                }

                Diagnostics.Trace("Loading \"{0}\" from zip archive \"{1}\"", entry, filename);
                buffer = fileSystem.GetZipFileEntry(filename, entry);
            }
            else
            {
                Diagnostics.Trace("Loading \"{0}\"", filename);
                buffer = fileSystem.ReadBytes(filename);
            }

            return buffer;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
