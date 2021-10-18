﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CPvC
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
        public delegate void ReportErrorDelegate(string message);
        public delegate bool ConfirmCloseDelegate(string message);
        public delegate RemoteMachine SelectRemoteMachineDelegate(ServerInfo serverInfo);
        public delegate UInt16? SelectServerPortDelegate(UInt16 defaultPort);
        public delegate ISocket CreateSocketDelegate();

        /// <summary>
        /// The data model associated with this view model.
        /// </summary>
        private MainModel _model;

        private ObservableCollection<ServerInfo> _recentServers;

        /// <summary>
        /// The currently active item. Will be either null (the Home tab) or a Machine.
        /// </summary>
        private object _activeItem;

        private IFileSystem _fileSystem;
        private SelectItemDelegate _selectItem;
        private PromptForFileDelegate _promptForFile;
        private PromptForBookmarkDelegate _promptForBookmark;
        private PromptForNameDelegate _promptForName;
        private SelectRemoteMachineDelegate _selectRemoteMachine;
        private SelectServerPortDelegate _selectServerPort;
        private ConfirmCloseDelegate _confirmClose;
        private ReportErrorDelegate _reportError;

        private Command _openMachineCommand;
        private Command _newMachineCommand;
        private Command _startServerCommand;
        private Command _stopServerCommand;
        private Command _connectCommand;
        private Command _removeCommand;
        private Command _closeCommand;

        private Command _driveACommand;
        private Command _driveAEjectCommand;
        private Command _driveBCommand;
        private Command _driveBEjectCommand;
        private Command _tapeCommand;
        private Command _tapeEjectCommand;
        private Command _resetCommand;
        private Command _persistCommand;
        private Command _openCommand;
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
        private Command _reverseStartCommand;
        private Command _reverseStopCommand;
        private Command _toggleSnapshotCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        private MachineServerListener _machineServer;

        private ISettings _settings;

        public MainViewModel(ISettings settings, IFileSystem fileSystem, SelectItemDelegate selectItem, PromptForFileDelegate promptForFile, PromptForBookmarkDelegate promptForBookmark, PromptForNameDelegate promptForName, ReportErrorDelegate reportError, SelectRemoteMachineDelegate selectRemoteMachine, SelectServerPortDelegate selectServerPort, CreateSocketDelegate createSocket, ConfirmCloseDelegate confirmClose)
        {
            _settings = settings;
            _fileSystem = fileSystem;
            _selectItem = selectItem;
            _promptForFile = promptForFile;
            _promptForBookmark = promptForBookmark;
            _promptForName = promptForName;
            _selectRemoteMachine = selectRemoteMachine;
            _selectServerPort = selectServerPort;
            _confirmClose = confirmClose;
            _reportError = reportError;

            InitModel(new MainModel(settings, fileSystem));

            _machineServer = new MachineServerListener(Machines.Where(m => m.Core != null));

            LoadRecentServersSetting();

            ActiveMachine = null;

            _openMachineCommand = new Command(
                p =>
                {
                    try
                    {
                        OpenMachine(promptForFile, null, _fileSystem);
                    }
                    catch (Exception ex)
                    {
                        reportError(ex.Message);
                    }
                },
                p => true
            );

            _newMachineCommand = new Command(
                p =>
                {
                    try
                    {
                        NewMachine(promptForFile, _fileSystem);
                    }
                    catch (Exception ex)
                    {
                        reportError(ex.Message);
                    }
                },
                p => true
            );

            _startServerCommand = new Command(
                p => StartServer(6128, createSocket()),
                p => true
            );

            _stopServerCommand = new Command(
                p => StopServer(),
                p => true
            );

            _connectCommand = new Command(
                p => Connect(p as ServerInfo),
                p => true
            );

            _removeCommand = new Command(
                p =>
                {
                    ICoreMachine coreMachine = (ICoreMachine)p;
                    if (Close(coreMachine, confirmClose))
                    {
                        _model.RemoveMachine(coreMachine);
                    }
                },
                p =>
                {
                    IPersistableMachine pm = p as IPersistableMachine;
                    return (pm?.PersistantFilepath != null);
                }
            );

            _closeCommand = new Command(
                p =>
                {
                    Close((ICoreMachine)p, confirmClose);
                },
                p =>
                {
                    IPersistableMachine pm = p as IPersistableMachine;
                    return (pm?.IsOpen ?? true);
                }
            );


            _openCommand = new Command(
                p => (p as IPersistableMachine)?.OpenFromFile(fileSystem),
                p => !(p as IPersistableMachine)?.IsOpen ?? false
            );

            _persistCommand = new Command(
                p =>
                {
                    try
                    {
                        Persist(p, fileSystem);
                    }
                    catch (Exception ex)
                    {
                        reportError(ex.Message);
                    }
                },
                p =>
                {
                    IPersistableMachine pm = p as IPersistableMachine;
                    if (pm != null)
                    {
                        return pm.PersistantFilepath == null;
                    }

                    return false;
                });

            _pauseCommand = new Command(
                p => (p as IPausableMachine)?.Stop(),
                p => (p as IPausableMachine)?.CanStop ?? false
            );

            _resumeCommand = new Command(
                p => (p as IPausableMachine)?.Start(),
                p => (p as IPausableMachine)?.CanStart ?? false
            );

            _resetCommand = new Command(
                p => (p as IInteractiveMachine)?.Reset(),
                p => (p as IInteractiveMachine) != null
            );

            _driveACommand = new Command(
                p => LoadDisc(p as IInteractiveMachine, 0, fileSystem, promptForFile, selectItem),
                p => (p as IInteractiveMachine) != null
            );

            _driveAEjectCommand = new Command(
                p => (p as IInteractiveMachine)?.LoadDisc(0, null),
                p => (p as IInteractiveMachine) != null
            );

            _driveBCommand = new Command(
                p => LoadDisc(p as IInteractiveMachine, 1, fileSystem, promptForFile, selectItem),
                p => (p as IInteractiveMachine) != null
            );

            _driveBEjectCommand = new Command(
                p => (p as IInteractiveMachine)?.LoadDisc(1, null),
                p => (p as IInteractiveMachine) != null
            );

            _tapeCommand = new Command(
                p => LoadTape(p as IInteractiveMachine, fileSystem, promptForFile, selectItem),
                p => (p as IInteractiveMachine) != null
            );

            _tapeEjectCommand = new Command(
                p => (p as IInteractiveMachine)?.LoadTape(null),
                p => (p as IInteractiveMachine) != null
            );

            _toggleRunningCommand = new Command(
                p => (p as IPausableMachine)?.ToggleRunning(),
                p => (p as IPausableMachine) != null
            );

            _addBookmarkCommand = new Command(
                p => (p as IBookmarkableMachine)?.AddBookmark(false),
                p => (p as IBookmarkableMachine) != null
            );

            _jumpToMostRecentBookmarkCommand = new Command(
                p => (p as IJumpableMachine)?.JumpToMostRecentBookmark(),
                p => (p as IJumpableMachine) != null
            );

            _browseBookmarksCommand = new Command(
                p => SelectBookmark(p as IJumpableMachine, promptForBookmark),
                p => (p as IJumpableMachine) != null
            );

            _compactCommand = new Command(
                p => (p as ICompactableMachine)?.Compact(fileSystem, false),
                p => (p as ICompactableMachine) != null
            );

            _renameCommand = new Command(
                p => RenameMachine(p as ICoreMachine, promptForName),
                p => p != null
            );

            _seekToNextBookmarkCommand = new Command(
                p => (p as IPrerecordedMachine)?.SeekToNextBookmark(),
                p => (p as IPrerecordedMachine) != null
            );

            _seekToPrevBookmarkCommand = new Command(
                p => (p as IPrerecordedMachine)?.SeekToPreviousBookmark(),
                p => (p as IPrerecordedMachine) != null
            );

            _seekToStartCommand = new Command(
                p => (p as IPrerecordedMachine)?.SeekToStart(),
                p => (p as IPrerecordedMachine) != null
            );

            _reverseStartCommand = new Command(
                p => (p as IReversibleMachine)?.Reverse(),
                p => (p as IReversibleMachine) != null
            );

            _reverseStopCommand = new Command(
                p => (p as IReversibleMachine)?.ReverseStop(),
                p => (p as IReversibleMachine) != null
            );

            _toggleSnapshotCommand = new Command(
                p => (p as IReversibleMachine)?.ToggleReversibilityEnabled(),
                p => (p as IReversibleMachine) != null
            );

        }

        private void InitModel(MainModel mainModel)
        {
            _model = mainModel;
        }

        public ObservableCollection<ServerInfo> RecentServers
        {
            get
            {
                return _recentServers;
            }
        }

        public ReadOnlyObservableCollection<ICoreMachine> Machines
        {
            get
            {
                return _model.Machines;
            }
        }
        public Command OpenMachineCommand
        {
            get { return _openMachineCommand; }
        }

        public Command NewMachineCommand
        {
            get { return _newMachineCommand; }
        }

        public ICommand StartServerCommand
        {
            get { return _startServerCommand; }
        }

        public ICommand StopServerCommand
        {
            get { return _stopServerCommand; }
        }

        public ICommand ConnectCommand
        {
            get { return _connectCommand; }
        }

        public ICommand RemoveCommand
        {
            get { return _removeCommand; }
        }

        public ICommand CloseCommand
        {
            get { return _closeCommand; }
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

        ///// <summary>
        ///// Represents the currently active item in the main window. Corresponds to the DataContext associated with the currently
        ///// selected tab in the main window.
        ///// </summary>
        public object ActiveItem
        {
            get
            {
                return _activeItem;
            }

            set
            {
                _activeItem = value;
                OnPropertyChanged();
                OnPropertyChanged("ActiveMachine");
            }
        }

        public ICoreMachine ActiveMachine
        {
            get
            {
                return _activeItem as ICoreMachine;
            }

            set
            {
                _activeItem = value;

                OnPropertyChanged();
                OnPropertyChanged("ActiveItem");
            }
        }

        public void OpenReplayMachine(string name, HistoryEvent finalEvent)
        {
            ReplayMachine replayMachine = new ReplayMachine(finalEvent);
            replayMachine.Name = name;
            _model.AddMachine(replayMachine);

            ActiveMachine = replayMachine;
            replayMachine.Start();
        }

        public void NewMachine(PromptForFileDelegate promptForFile, IFileSystem fileSystem)
        {
            Machine machine = Machine.New("Untitled", null);
            _model.AddMachine(machine);

            ActiveMachine = machine;
            machine.Start();
        }

        public ICoreMachine OpenMachine(PromptForFileDelegate promptForFile, string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                filepath = promptForFile(FileTypes.Machine, true);
                if (filepath == null)
                {
                    return null;
                }
            }

            string fullFilepath = System.IO.Path.GetFullPath(filepath);
            ICoreMachine machine = Machines.FirstOrDefault(
                m =>
                {
                    if (m is IPersistableMachine pm)
                    {
                        return String.Compare(pm.PersistantFilepath, fullFilepath, true) == 0;
                    }

                    return false;
                });
            if (machine == null)
            {
                machine = _model.AddMachine(filepath, fileSystem, true);
            }

            return machine;
        }

        public void CloseAll()
        {
            lock (Machines)
            {
                foreach (ICoreMachine machine in Machines)
                {
                    Close(machine, _confirmClose);
                }
            }
        }

        public bool Close(ICoreMachine coreMachine, ConfirmCloseDelegate confirmClose)
        {
            if (coreMachine != null)
            {
                IPersistableMachine pm = coreMachine as IPersistableMachine;
                if (pm != null && pm.PersistantFilepath == null)
                {
                    if (!confirmClose(String.Format("Are you sure you want to close the \"{0}\" machine without persisting it?", coreMachine.Name)))
                    {
                        return false;
                    }
                }

                coreMachine.Close();
            }

            return true;
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (Machines)
            {
                int samplesWritten = 0;

                foreach (ICoreMachine machine in Machines)
                {
                    if (machine == null)
                    {
                        continue;
                    }

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

                return samplesWritten;
            }
        }

        public void KeyPress(IInteractiveMachine machine, byte keyCode, bool down)
        {
            machine.Key(keyCode, down);
        }

        public void EnableTurbo(ITurboableMachine machine, bool enable)
        {
            machine.EnableTurbo(enable);
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

        public void Persist(object p, IFileSystem fileSystem)
        {
            if (!(p is IPersistableMachine machine))
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

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void StartServer(UInt16 defaultPort, ISocket socket)
        {
            UInt16? port = _selectServerPort(defaultPort);
            if (port.HasValue)
            {
                _machineServer.Start(socket, port.Value);
            }
        }

        public void StopServer()
        {
            _machineServer.Stop();
        }

        public void Connect(ServerInfo serverInfo)
        {
            RemoteMachine remoteMachine = _selectRemoteMachine(serverInfo);
            if (remoteMachine == null)
            {
                return;
            }

            _model.AddMachine(remoteMachine);
            UpdateRecentServersSettings();

            ActiveMachine = remoteMachine;
        }

        private void UpdateRecentServersSettings()
        {
            IEnumerable<string> serversAndPorts = RecentServers.Select(s => Helpers.JoinWithEscape(':', new string[] { s.ServerName, s.Port.ToString() }));

            string setting = Helpers.JoinWithEscape(';', serversAndPorts);

            _settings.RemoteServers = setting;
        }

        private void LoadRecentServersSetting()
        {
            if (_settings?.RemoteServers == null)
            {
                return;
            }

            _recentServers = new ObservableCollection<ServerInfo>();
            IEnumerable<string> serversAndPorts = Helpers.SplitWithEscape(';', _settings.RemoteServers);

            lock (RecentServers)
            {
                foreach (string serverStr in serversAndPorts)
                {
                    List<string> tokens = Helpers.SplitWithEscape(':', serverStr);
                    if (tokens.Count < 2)
                    {
                        continue;
                    }

                    ServerInfo info = new ServerInfo(tokens[0], System.Convert.ToUInt16(tokens[1]));

                    RecentServers.Add(info);
                }
            }
        }
    }
}
