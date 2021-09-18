using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static CPvC.MainViewModel;

namespace CPvC
{
    public class MachineViewModel : INotifyPropertyChanged
    {
        private Command _driveACommand;
        private Command _driveAEjectCommand;
        private Command _driveBCommand;
        private Command _driveBEjectCommand;
        private Command _tapeCommand;
        private Command _tapeEjectCommand;
        private Command _resetCommand;
        private Command _persistCommand;
        private Command _openCommand;
        //private Command _closeCommand;
        private Command _pauseCommand;
        private Command _resumeCommand;
        private Command _toggleRunningCommand;
        private Command _addBookmarkCommand;
        private Command _jumpToMostRecentBookmarkCommand;
        private Command _browseBookmarksCommand;
        private Command _compactCommand;
        private Command _renameCommand;
        private Command _seekToNextBookmarkCommand;
        private Command _seekToPrevBookmarkCommand;
        private Command _seekToStartCommand;
        private Command _keyDownCommand;
        private Command _keyUpCommand;
        private Command _turboCommand;
        private Command _reverseStartCommand;
        private Command _reverseStopCommand;
        //private Command _removeCommand;
        private Command _toggleSnapshotCommand;

        private readonly  ICoreMachine _machine;

        private PromptForFileDelegate _promptForFile;

        public event PropertyChangedEventHandler PropertyChanged;

        public MachineViewModel(ICoreMachine machine, IFileSystem fileSystem, PromptForFileDelegate promptForFile, PromptForBookmarkDelegate promptForBookmark, PromptForNameDelegate promptForName, SelectItemDelegate selectItem, ConfirmCloseDelegate confirmClose, ReportErrorDelegate reportError)
        {
            _promptForFile = promptForFile;

            _machine = machine;

            _openCommand = new Command(
                p => Open(fileSystem),
                p => !(_machine as IPersistableMachine)?.IsOpen ?? false,
                _machine,
                new List<string> { nameof(IPersistableMachine.IsOpen) }
            );

            _persistCommand = new Command(
                p =>
                {
                    try
                    {
                        Persist(fileSystem, promptForFile);
                    }
                    catch (Exception ex)
                    {
                        reportError(ex.Message);
                    }
                },
                p =>
                {
                    IPersistableMachine pm = Machine as IPersistableMachine;
                    if (pm != null)
                    {
                        return pm.PersistantFilepath == null;
                    }

                    return false;
                },
                _machine,
                new List<string> { nameof(IPersistableMachine.PersistantFilepath) });

            _pauseCommand = new Command(
                p => (_machine as IPausableMachine)?.Stop(),
                p =>
                {
                    if (_machine == null)
                    {
                        return false;
                    }

                    return (_machine.RunningState == RunningState.Running);
                },
                _machine,
                new List<string> { nameof(IPausableMachine.RunningState) }
            );

            _resumeCommand = new Command(
                p => (Machine as IPausableMachine)?.Start(),
                p =>
                {
                    if (_machine == null)
                    {
                        return false;
                    }

                    // Should really add "CanResume" and "CanPause" to IPausableMachine?
                    IPersistableMachine pm = Machine as IPersistableMachine;
                    if (pm != null && !pm.IsOpen)
                    {
                        return false;
                    }

                    return (_machine.RunningState != RunningState.Running);
                },
                _machine,
                new List<string> { nameof(IPausableMachine.RunningState) }
            );

            _resetCommand = new Command(
                p => (Machine as IInteractiveMachine)?.Reset(),
                p => (Machine as IInteractiveMachine) != null
            );

            _driveACommand = new Command(
                p => LoadDisc(Machine as IInteractiveMachine, 0, fileSystem, promptForFile, selectItem),
                p => (Machine as IInteractiveMachine) != null
            );

            _driveAEjectCommand = new Command(
                p => (Machine as IInteractiveMachine)?.LoadDisc(0, null),
                p => (Machine as IInteractiveMachine) != null
            );

            _driveBCommand = new Command(
                p => LoadDisc(Machine as IInteractiveMachine, 1, fileSystem, promptForFile, selectItem),
                p => (Machine as IInteractiveMachine) != null
            );

            _driveBEjectCommand = new Command(
                p => (Machine as IInteractiveMachine)?.LoadDisc(1, null),
                p => (Machine as IInteractiveMachine) != null
            );

            _tapeCommand = new Command(
                p => LoadTape(Machine as IInteractiveMachine, fileSystem, promptForFile, selectItem),
                p => (Machine as IInteractiveMachine) != null
            );

            _tapeEjectCommand = new Command(
                p => (Machine as IInteractiveMachine)?.LoadTape(null),
                p => (Machine as IInteractiveMachine) != null
            );

            _toggleRunningCommand = new Command(
                p => (Machine as IPausableMachine)?.ToggleRunning(),
                p => (Machine as IPausableMachine) != null
            );

            _addBookmarkCommand = new Command(
                p => (Machine as IBookmarkableMachine)?.AddBookmark(false),
                p => (Machine as IBookmarkableMachine) != null
            );

            _jumpToMostRecentBookmarkCommand = new Command(
                p => (Machine as IJumpableMachine)?.JumpToMostRecentBookmark(),
                p => (Machine as IJumpableMachine) != null
            );

            _browseBookmarksCommand = new Command(
                p => SelectBookmark(Machine as IJumpableMachine, promptForBookmark),
                p => (Machine as IJumpableMachine) != null
            );

            _compactCommand = new Command(
                p => (Machine as ICompactableMachine)?.Compact(false),
                p => (Machine as ICompactableMachine) != null
            );

            _renameCommand = new Command(
                p => RenameMachine(Machine, promptForName),
                p => Machine != null
            );

            _seekToNextBookmarkCommand = new Command(
                p => (Machine as IPrerecordedMachine)?.SeekToNextBookmark(),
                p => (Machine as IPrerecordedMachine) != null
            );

            _seekToPrevBookmarkCommand = new Command(
                p => (Machine as IPrerecordedMachine)?.SeekToPreviousBookmark(),
                p => (Machine as IPrerecordedMachine) != null
            );

            _seekToStartCommand = new Command(
                p => (Machine as IPrerecordedMachine)?.SeekToStart(),
                p => (Machine as IPrerecordedMachine) != null
            );

            _keyDownCommand = new Command(
                p => (Machine as IInteractiveMachine)?.Key((byte)p, true),
                p => (Machine as IInteractiveMachine) != null
            );

            _keyUpCommand = new Command(
                p => (Machine as IInteractiveMachine)?.Key((byte)p, false),
                p => (Machine as IInteractiveMachine) != null
            );

            _turboCommand = new Command(
                p => (Machine as ITurboableMachine)?.EnableTurbo((bool)p),
                p => (Machine as ITurboableMachine) != null
            );

            _reverseStartCommand = new Command(
                p => (Machine as IReversibleMachine)?.Reverse(),
                p => (Machine as IReversibleMachine) != null
            );

            _reverseStopCommand = new Command(
                p => (Machine as IReversibleMachine)?.ReverseStop(),
                p => (Machine as IReversibleMachine) != null
            );

            _toggleSnapshotCommand = new Command(
                p => (Machine as IReversibleMachine)?.ToggleReversibilityEnabled(),
                p => (Machine as IReversibleMachine) != null
            );
        }

        public ICoreMachine Machine
        {
            get
            {
                return _machine;
            }
        }

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

        public Command PersistCommand
        {
            get { return _persistCommand; }
        }

        public ICommand OpenCommand
        {
            get { return _openCommand; }
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

        public ICommand JumpToMostRecentBookmarkCommand
        {
            get { return _jumpToMostRecentBookmarkCommand; }
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

        public ICommand SeekToNextBookmarkCommand
        {
            get { return _seekToNextBookmarkCommand; }
        }

        public ICommand SeekToPrevBookmarkCommand
        {
            get { return _seekToPrevBookmarkCommand; }
        }

        public ICommand SeekToStartCommand
        {
            get { return _seekToStartCommand; }
        }

        public ICommand KeyDownCommand
        {
            get { return _keyDownCommand; }
        }

        public ICommand KeyUpCommand
        {
            get { return _keyUpCommand; }
        }

        public ICommand TurboCommand
        {
            get { return _turboCommand; }
        }

        public ICommand ReverseStartCommand
        {
            get { return _reverseStartCommand; }
        }

        public ICommand ReverseStopCommand
        {
            get { return _reverseStopCommand; }
        }

        public ICommand ToggleReversibility
        {
            get { return _toggleSnapshotCommand; }
        }

        private void LoadDisc(IInteractiveMachine machine, byte drive, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
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

        private void LoadTape(IInteractiveMachine machine, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
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

        private void SelectBookmark(IJumpableMachine machine, PromptForBookmarkDelegate promptForBookmark)
        {
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                HistoryEvent historyEvent = promptForBookmark();
                if (historyEvent != null)
                {
                    machine.JumpToBookmark(historyEvent);
                    machine.Status = String.Format("Jumped to {0}", Helpers.GetTimeSpanFromTicks(historyEvent.Ticks).ToString(@"hh\:mm\:ss"));
                }
            }
        }

        private void RenameMachine(ICoreMachine machine, PromptForNameDelegate promptForName)
        {
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

        public void Open(IFileSystem fileSystem)
        {
            Machine machine = Machine as Machine;
            if (machine == null)
            {
                return;
            }

            machine.OpenFromFile(fileSystem);
            machine.Start();
        }

        public void Persist(IFileSystem fileSystem, PromptForFileDelegate promptForFile)
        {
            if (!(_machine is IPersistableMachine machine))
            {
                throw new InvalidOperationException("Cannot persist this machine!");
            }

            if (!String.IsNullOrEmpty(machine.PersistantFilepath))
            {
                // Should throw exception here?
                return;
            }

            string filepath = _promptForFile(FileTypes.Machine, false);
            machine.Persist(fileSystem, filepath);
        }

        public bool Close(ConfirmCloseDelegate confirmClose)
        {
            if (Machine != null)
            {
                IPersistableMachine pm = Machine as IPersistableMachine;
                if (pm != null && pm.PersistantFilepath == null)
                {
                    if (!confirmClose(String.Format("Are you sure you want to close the \"{0}\" machine without persisting it?", Machine.Name)))
                    {
                        return false;
                    }
                }

                Machine.Close();
            }

            return true;
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
