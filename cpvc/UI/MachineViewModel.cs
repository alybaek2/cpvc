using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineViewModel
    {
        public MachineViewModel(IMachine machine, IFileSystem fileSystem, Action<Action> canExecuteChangedInvoker)
        {
            Command CreateCommand(Action<object> execute, Predicate<object> canExecute)
            {
                Command command = new Command(execute, canExecute, canExecuteChangedInvoker);

                _allCommands.Add(command);

                return command;
            }

            _machine = machine;
            if (_machine != null)
            {
                _machine.PropertyChanged += Machine_PropertyChanged;
            }

            _allCommands = new List<Command>();

            _openCommand = CreateCommand(
                _ => (_machine as IPersistableMachine)?.OpenFromFile(fileSystem),
                _ => !(_machine as IPersistableMachine)?.IsOpen ?? false
            );

            _persistCommand = CreateCommand(
                _ => Persist(fileSystem, _machine as IPersistableMachine),
                _ =>
                {
                    if (_machine is IPersistableMachine pm)
                    {
                        return pm.PersistentFilepath == null;
                    }

                    return false;
                });

            _pauseCommand = CreateCommand(
                _ => (_machine as IPausableMachine)?.Stop(),
                _ => (_machine as IPausableMachine)?.CanStop ?? false
            );

            _resumeCommand = CreateCommand(
                _ => (_machine as IPausableMachine)?.Start(),
                _ => (_machine as IPausableMachine)?.CanStart ?? false
            );

            _resetCommand = CreateCommand(
                _ => (_machine as IInteractiveMachine)?.Reset(),
                _ => _machine is IInteractiveMachine
            );

            _driveACommand = CreateCommand(
                _ => LoadDisc(fileSystem, _machine as IInteractiveMachine, 0),
                _ => _machine is IInteractiveMachine
            );

            _driveAEjectCommand = CreateCommand(
                _ => (_machine as IInteractiveMachine)?.LoadDisc(0, null),
                _ => _machine is IInteractiveMachine
            );

            _driveBCommand = CreateCommand(
                _ => LoadDisc(fileSystem, _machine as IInteractiveMachine, 1),
                _ => _machine is IInteractiveMachine
            );

            _driveBEjectCommand = CreateCommand(
                _ => (_machine as IInteractiveMachine)?.LoadDisc(1, null),
                _ => _machine is IInteractiveMachine
            );

            _tapeCommand = CreateCommand(
                _ => LoadTape(fileSystem, _machine as IInteractiveMachine),
                _ => _machine is IInteractiveMachine
            );

            _tapeEjectCommand = CreateCommand(
                _ => (_machine as IInteractiveMachine)?.LoadTape(null),
                _ => _machine is IInteractiveMachine
            );

            _toggleRunningCommand = CreateCommand(
                _ => (_machine as IPausableMachine)?.ToggleRunning(),
                _ => _machine is IPausableMachine
            );

            _addBookmarkCommand = CreateCommand(
                _ => (_machine as IBookmarkableMachine)?.AddBookmark(false),
                _ => _machine is IBookmarkableMachine
            );

            _jumpToMostRecentBookmarkCommand = CreateCommand(
                _ => (_machine as IJumpableMachine)?.JumpToMostRecentBookmark(),
                _ => _machine is IJumpableMachine
            );

            _browseBookmarksCommand = CreateCommand(
                _ => SelectBookmark(_machine as IJumpableMachine),
                _ => _machine is IJumpableMachine
            );

            _compactCommand = CreateCommand(
                _ => (_machine as ICompactableMachine)?.Compact(fileSystem),
                _ => (_machine as ICompactableMachine)?.CanCompact ?? false
            );

            _renameCommand = CreateCommand(
                _ => RenameMachine(_machine),
                _ => _machine is IMachine
            );

            _seekToNextBookmarkCommand = CreateCommand(
                _ => (_machine as IPrerecordedMachine)?.SeekToNextBookmark(),
                _ => _machine is IPrerecordedMachine
            );

            _seekToPrevBookmarkCommand = CreateCommand(
                _ => (_machine as IPrerecordedMachine)?.SeekToPreviousBookmark(),
                _ => _machine is IPrerecordedMachine
            );

            _seekToStartCommand = CreateCommand(
                _ => (_machine as IPrerecordedMachine)?.SeekToStart(),
                _ => _machine is IPrerecordedMachine
            );

            _reverseStartCommand = CreateCommand(
                _ => (_machine as IReversibleMachine)?.Reverse(),
                _ => _machine is IReversibleMachine
            );

            _toggleSnapshotCommand = CreateCommand(
                _ => (_machine as IReversibleMachine)?.ToggleReversibilityEnabled(),
                _ => _machine is IReversibleMachine
            );
        }

        private void Machine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Machine.Ticks))
            {
                UpdateCommands(sender, e);
            }
        }

        public event PromptForFileEventHandler PromptForFile;
        public event SelectItemEventHandler SelectItem;
        public event PromptForBookmarkEventHandler PromptForBookmark;
        public event PromptForNameEventHandler PromptForName;

        public IMachine Machine
        {
            get
            {
                return _machine;
            }
        }

        public HistoryViewModel History
        {
            get
            {
                if (_machine is IHistoricalMachine historicalMachine)
                {
                    return HistoryViewModel.GetViewModel(historicalMachine.History);
                }

                return null;
            }
        }

        public Command ResetCommand
        {
            get { return _resetCommand; }
        }

        public Command DriveACommand
        {
            get { return _driveACommand; }
        }

        public Command DriveBCommand
        {
            get { return _driveBCommand; }
        }

        public Command DriveAEjectCommand
        {
            get { return _driveAEjectCommand; }
        }

        public Command DriveBEjectCommand
        {
            get { return _driveBEjectCommand; }
        }

        public Command TapeCommand
        {
            get { return _tapeCommand; }
        }

        public Command TapeEjectCommand
        {
            get { return _tapeEjectCommand; }
        }

        public Command PersistCommand
        {
            get { return _persistCommand; }
        }

        public Command OpenCommand
        {
            get { return _openCommand; }
        }

        public Command PauseCommand
        {
            get { return _pauseCommand; }
        }

        public Command ResumeCommand
        {
            get { return _resumeCommand; }
        }

        public Command ToggleRunningCommand
        {
            get { return _toggleRunningCommand; }
        }

        public Command AddBookmarkCommand
        {
            get { return _addBookmarkCommand; }
        }

        public Command JumpToMostRecentBookmarkCommand
        {
            get { return _jumpToMostRecentBookmarkCommand; }
        }

        public Command BrowseBookmarksCommand
        {
            get { return _browseBookmarksCommand; }
        }

        public Command CompactCommand
        {
            get { return _compactCommand; }
        }

        public Command RenameCommand
        {
            get { return _renameCommand; }
        }

        public Command SeekToNextBookmarkCommand
        {
            get { return _seekToNextBookmarkCommand; }
        }

        public Command SeekToPrevBookmarkCommand
        {
            get { return _seekToPrevBookmarkCommand; }
        }

        public Command SeekToStartCommand
        {
            get { return _seekToStartCommand; }
        }

        public Command ReverseStartCommand
        {
            get { return _reverseStartCommand; }
        }

        public Command ToggleReversibility
        {
            get { return _toggleSnapshotCommand; }
        }

        private void LoadDisc(IFileSystem fileSystem, IInteractiveMachine machine, byte drive)
        {
            if (machine == null)
            {
                return;
            }

            using (machine.Lock())
            {
                byte[] image = PromptForMedia(fileSystem, true);
                if (image != null)
                {
                    machine.LoadDisc(drive, image);
                }
            }
        }

        private void LoadTape(IFileSystem fileSystem, IInteractiveMachine machine)
        {
            if (machine == null)
            {
                return;
            }

            using (machine.Lock())
            {
                byte[] image = PromptForMedia(fileSystem, false);
                if (image != null)
                {
                    machine.LoadTape(image);
                }
            }
        }

        public void UpdateCommands(object sender, EventArgs e)
        {
            foreach (Command command in _allCommands)
            {
                command.InvokeCanExecuteChanged(sender, e);
            }
        }
        public void Persist(IFileSystem fileSystem, IPersistableMachine machine)
        {
            if (machine == null)
            {
                throw new ArgumentNullException(nameof(machine));
            }

            if (!String.IsNullOrEmpty(machine.PersistentFilepath))
            {
                // Should throw exception here?
                return;
            }

            PromptForFileEventArgs args = new PromptForFileEventArgs(FileTypes.Machine, false);
            PromptForFile?.Invoke(this, args);

            string filepath = args.Filepath;
            machine.Persist(fileSystem, filepath);
        }

        private void SelectBookmark(IJumpableMachine jumpableMachine)
        {
            if (jumpableMachine == null)
            {
                return;
            }

            using (jumpableMachine.Lock())
            {
                PromptForBookmarkEventArgs args = new PromptForBookmarkEventArgs();
                PromptForBookmark?.Invoke(this, args);

                bool updateStatus = true;
                HistoryEvent historyEvent = args.SelectedBookmark;
                switch (historyEvent)
                {
                    case BookmarkHistoryEvent bookmarkHistoryEvent:
                        jumpableMachine.JumpToBookmark(bookmarkHistoryEvent);
                        break;
                    case RootHistoryEvent _:
                        jumpableMachine.JumpToRoot();
                        break;
                    default:
                        updateStatus = false;
                        break;
                }

                // This should really be done in the call to JumpToBookmark/Root...
                if (updateStatus)
                {
                    (jumpableMachine as IMachine).Status = String.Format("Jumped to {0}", Helpers.GetTimeSpanFromTicks(historyEvent.Ticks).ToString(@"hh\:mm\:ss"));
                }
            }
        }

        private void RenameMachine(IMachine machine)
        {
            if (machine == null)
            {
                return;
            }

            using (machine.Lock())
            {
                PromptForNameEventArgs args = new PromptForNameEventArgs(machine.Name);
                PromptForName?.Invoke(this, args);

                string newName = args.SelectedName;
                if (newName != null)
                {
                    machine.Name = newName;
                }
            }
        }

        private byte[] PromptForMedia(IFileSystem fileSystem, bool disc)
        {
            string expectedExt = disc ? ".dsk" : ".cdt";
            FileTypes type = disc ? FileTypes.Disc : FileTypes.Tape;

            PromptForFileEventArgs promptForFileArgs = new PromptForFileEventArgs(type, true);
            PromptForFile?.Invoke(this, promptForFileArgs);

            string filename = promptForFileArgs.Filepath;
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
                    SelectItemEventArgs selectItemArgs = new SelectItemEventArgs(extEntries);
                    SelectItem?.Invoke(this, selectItemArgs);

                    entry = selectItemArgs.SelectedItem;
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

        private IMachine _machine;

        private readonly Command _driveACommand;
        private readonly Command _driveAEjectCommand;
        private readonly Command _driveBCommand;
        private readonly Command _driveBEjectCommand;
        private readonly Command _tapeCommand;
        private readonly Command _tapeEjectCommand;
        private readonly Command _resetCommand;
        private readonly Command _persistCommand;
        private readonly Command _openCommand;
        private readonly Command _pauseCommand;
        private readonly Command _resumeCommand;
        private readonly Command _toggleRunningCommand;
        private readonly Command _addBookmarkCommand;
        private readonly Command _jumpToMostRecentBookmarkCommand;
        private readonly Command _browseBookmarksCommand;
        private readonly Command _compactCommand;
        private readonly Command _renameCommand;
        private readonly Command _seekToNextBookmarkCommand;
        private readonly Command _seekToPrevBookmarkCommand;
        private readonly Command _seekToStartCommand;
        private readonly Command _reverseStartCommand;
        private readonly Command _toggleSnapshotCommand;

        private readonly List<Command> _allCommands;
    }
}
