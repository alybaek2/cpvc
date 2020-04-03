using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using static CPvC.UI.MainViewModel;

namespace CPvC
{
    public class MachineViewModel
    {
        private Command _driveACommand;
        private Command _driveAEjectCommand;
        private Command _driveBCommand;
        private Command _driveBEjectCommand;
        private Command _tapeCommand;
        private Command _tapeEjectCommand;
        private Command _resetCommand;
        private Command _openCommand;
        private Command _closeCommand;
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
        private ICommand _removeCommand;

        public MachineViewModel(ICoreMachine machine, IFileSystem fileSystem, PromptForFileDelegate promptForFile, PromptForBookmarkDelegate promptForBookmark, PromptForNameDelegate promptForName, SelectItemDelegate selectItem)
        {
            Machine = machine;
            if (machine != null)
            {
                Machine.PropertyChanged += MachinePropChanged;
            }

            _openCommand = new Command(
                p => (machine as IOpenableMachine)?.Open(),
                p => (machine as IOpenableMachine)?.RequiresOpen ?? false
            );

            _closeCommand = new Command(
                p => (machine as IClosableMachine)?.Close(),
                p => (machine as IClosableMachine)?.CanClose() ?? false
            );

            _pauseCommand = new Command(
                p => (machine as IPausableMachine)?.Stop(),
                p =>
                {
                    if (machine == null)
                    {
                        return false;
                    }

                    IOpenableMachine m = machine as IOpenableMachine;
                    if (machine.Running && (m == null || !m.RequiresOpen))
                    {
                        return true;
                    }

                    return false;
                }
            );

            _resumeCommand = new Command(
                p => (machine as IPausableMachine)?.Start(),
                p =>
                {
                    if (machine == null)
                    {
                        return false;
                    }

                    IOpenableMachine m = machine as IOpenableMachine;
                    if (!machine.Running && (m == null || !m.RequiresOpen))
                    {
                        return true;
                    }

                    return false;
                }
            );

            _resetCommand = new Command(
                p => (machine as IInteractiveMachine)?.Reset(),
                p => (machine as IInteractiveMachine) != null
            );

            _driveACommand = new Command(
                p => LoadDisc(machine as IInteractiveMachine, 0, fileSystem, promptForFile, selectItem),
                p => (machine as IInteractiveMachine) != null
            );

            _driveAEjectCommand = new Command(
                p => (machine as IInteractiveMachine)?.LoadDisc(0, null),
                p => (machine as IInteractiveMachine) != null
            );

            _driveBCommand = new Command(
                p => LoadDisc(machine as IInteractiveMachine, 1, fileSystem, promptForFile, selectItem),
                p => (machine as IInteractiveMachine) != null
            );

            _driveBEjectCommand = new Command(
                p => (machine as IInteractiveMachine)?.LoadDisc(1, null),
                p => (machine as IInteractiveMachine) != null
            );

            _tapeCommand = new Command(
                p => LoadTape(machine as IInteractiveMachine, fileSystem, promptForFile, selectItem),
                p => (machine as IInteractiveMachine) != null
            );

            _tapeEjectCommand = new Command(
                p => (machine as IInteractiveMachine)?.LoadTape(null),
                p => (machine as IInteractiveMachine) != null
            );

            _toggleRunningCommand = new Command(
                p => (machine as IPausableMachine)?.ToggleRunning(),
                p => (machine as IPausableMachine) != null
            );

            _addBookmarkCommand = new Command(
                p => (machine as IBookmarkableMachine)?.AddBookmark(false),
                p => (machine as IBookmarkableMachine) != null
            );

            _jumpToMostRecentBookmarkCommand = new Command(
                p => (machine as IBookmarkableMachine)?.JumpToMostRecentBookmark(),
                p => (machine as IBookmarkableMachine) != null
            );

            _browseBookmarksCommand = new Command(
                p => SelectBookmark(machine as Machine, promptForBookmark),
                p => (machine as IBookmarkableMachine) != null
            );

            _compactCommand = new Command(
                p => (machine as ICompactableMachine)?.Compact(false),
                p => (machine as ICompactableMachine) != null
            );

            _renameCommand = new Command(
                p => RenameMachine(machine as Machine, promptForName),
                p => machine != null
            );

            _seekToNextBookmarkCommand = new Command(
                p => (machine as IPrerecordedMachine)?.SeekToNextBookmark(),
                p => (machine as IPrerecordedMachine) != null
            );

            _seekToPrevBookmarkCommand = new Command(
                p => (machine as IPrerecordedMachine)?.SeekToPreviousBookmark(),
                p => (machine as IPrerecordedMachine) != null
            );

            _seekToStartCommand = new Command(
                p => (machine as IPrerecordedMachine)?.SeekToStart(),
                p => (machine as IPrerecordedMachine) != null
            );

            _keyDownCommand = new Command(
                p => (machine as IInteractiveMachine)?.Key((byte)p, true),
                p => (machine as IInteractiveMachine) != null
            );

            _keyUpCommand = new Command(
                p => (machine as IInteractiveMachine)?.Key((byte)p, false),
                p => (machine as IInteractiveMachine) != null
            );

            _turboCommand = new Command(
                p => (machine as ITurboableMachine)?.EnableTurbo((bool)p),
                p => (machine as ITurboableMachine) != null
            );
        }

        public ICoreMachine Machine { get; }

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

        public ICommand OpenCommand
        {
            get { return _openCommand; }
        }

        public ICommand CloseCommand
        {
            get { return _closeCommand; }
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

        public ICommand RemoveCommand
        {
            get { return _removeCommand; }
            set { _removeCommand = value; }
        }

        private void MachinePropChanged(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == "Running")
            {
                _resumeCommand.InvokeCanExecuteChanged(sender, args);
                _pauseCommand.InvokeCanExecuteChanged(sender, args);
            }
            else if (args.PropertyName == "RequiresOpen")
            {
                _openCommand.InvokeCanExecuteChanged(sender, args);
                _closeCommand.InvokeCanExecuteChanged(sender, args);
            }
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

        private void SelectBookmark(IInteractiveMachine machine, PromptForBookmarkDelegate promptForBookmark)
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
                    machine.SetCurrentEvent(historyEvent);
                    machine.Status = String.Format("Jumped to {0}", Helpers.GetTimeSpanFromTicks(machine.Core.Ticks).ToString(@"hh\:mm\:ss"));
                }
            }
        }

        private void RenameMachine(IInteractiveMachine machine, PromptForNameDelegate promptForName)
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
    }
}
