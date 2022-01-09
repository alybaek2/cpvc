using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public event PromptForFileEventHandler PromptForFile;
        public event SelectItemEventHandler SelectItem;
        public event PromptForBookmarkEventHandler PromptForBookmark;
        public event PromptForNameEventHandler PromptForName;
        public event SelectRemoteMachineEventHandler SelectRemoteMachine;
        public event SelectServerPortEventHandler SelectServerPort;
        public event ConfirmCloseEventHandler ConfirmClose;
        public event CreateSocketEventHandler CreateSocket;

        private readonly Command _openMachineCommand;
        private readonly Command _newMachineCommand;
        private readonly Command _startServerCommand;
        private readonly Command _stopServerCommand;
        private readonly Command _connectCommand;
        private readonly Command _removeCommand;
        private readonly Command _closeCommand;

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
        private readonly Command _reverseStopCommand;
        private readonly Command _toggleSnapshotCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        private MachineServerListener _machineServer;

        private ISettings _settings;

        public MainViewModel(ISettings settings, IFileSystem fileSystem)
        {
            _settings = settings;
            _fileSystem = fileSystem;

            InitModel(new MainModel(settings, fileSystem));

            _machineServer = new MachineServerListener(Machines.Where(m => m.Core != null));

            LoadRecentServersSetting();

            ActiveMachine = null;

            _openMachineCommand = new Command(
                p => OpenMachine(),
                p => true
            );

            _newMachineCommand = new Command(
                p => NewMachine(),
                p => true
            );

            _startServerCommand = new Command(
                p => StartServer(6128),
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
                    IMachine machine = p as IMachine;
                    if (Close(machine, true))
                    {
                        _model.RemoveMachine(machine);
                    }
                },
                p =>
                {
                    IPersistableMachine pm = p as IPersistableMachine;
                    return (pm?.PersistantFilepath != null);
                }
            );

            _closeCommand = new Command(
                p => Close(p as IMachine, true),
                p => (p as IMachine)?.CanClose ?? false
            );

            _openCommand = new Command(
                p => (p as IPersistableMachine)?.OpenFromFile(_fileSystem),
                p => !(p as IPersistableMachine)?.IsOpen ?? false
            );

            _persistCommand = new Command(
                p => Persist(p as IPersistableMachine),
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
                p => p is IInteractiveMachine
            );

            _driveACommand = new Command(
                p => LoadDisc(p as IInteractiveMachine, 0),
                p => p is IInteractiveMachine
            );

            _driveAEjectCommand = new Command(
                p => (p as IInteractiveMachine)?.LoadDisc(0, null),
                p => p is IInteractiveMachine
            );

            _driveBCommand = new Command(
                p => LoadDisc(p as IInteractiveMachine, 1),
                p => p is IInteractiveMachine
            );

            _driveBEjectCommand = new Command(
                p => (p as IInteractiveMachine)?.LoadDisc(1, null),
                p => p is IInteractiveMachine
            );

            _tapeCommand = new Command(
                p => LoadTape(p as IInteractiveMachine),
                p => p is IInteractiveMachine
            );

            _tapeEjectCommand = new Command(
                p => (p as IInteractiveMachine)?.LoadTape(null),
                p => p is IInteractiveMachine
            );

            _toggleRunningCommand = new Command(
                p => (p as IPausableMachine)?.ToggleRunning(),
                p => p is IPausableMachine
            );

            _addBookmarkCommand = new Command(
                p => (p as IBookmarkableMachine)?.AddBookmark(false),
                p => p is IBookmarkableMachine
            );

            _jumpToMostRecentBookmarkCommand = new Command(
                p => (p as IJumpableMachine)?.JumpToMostRecentBookmark(),
                p => p is IJumpableMachine
            );

            _browseBookmarksCommand = new Command(
                p => SelectBookmark(p as IJumpableMachine),
                p => p is IJumpableMachine
            );

            _compactCommand = new Command(
                p => (p as ICompactableMachine)?.Compact(_fileSystem),
                p => (p as ICompactableMachine)?.CanCompact() ?? false
            );

            _renameCommand = new Command(
                p => RenameMachine(p as IMachine),
                p => p is IMachine
            );

            _seekToNextBookmarkCommand = new Command(
                p => (p as IPrerecordedMachine)?.SeekToNextBookmark(),
                p => p is IPrerecordedMachine
            );

            _seekToPrevBookmarkCommand = new Command(
                p => (p as IPrerecordedMachine)?.SeekToPreviousBookmark(),
                p => p is IPrerecordedMachine
            );

            _seekToStartCommand = new Command(
                p => (p as IPrerecordedMachine)?.SeekToStart(),
                p => p is IPrerecordedMachine
            );

            _reverseStartCommand = new Command(
                p => (p as IReversibleMachine)?.Reverse(),
                p => p is IReversibleMachine
            );

            _reverseStopCommand = new Command(
                p => (p as IReversibleMachine)?.ReverseStop(),
                p => p is IReversibleMachine
            );

            _toggleSnapshotCommand = new Command(
                p => (p as IReversibleMachine)?.ToggleReversibilityEnabled(),
                p => p is IReversibleMachine
            );
        }

        public MainModel Model
        {
            get
            {
                return _model;
            }
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

        public ReadOnlyObservableCollection<IMachine> Machines
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

        public IMachine ActiveMachine
        {
            get
            {
                return _activeItem as IMachine;
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

        private void NewMachine()
        {
            LocalMachine machine = LocalMachine.New("Untitled", null);
            _model.AddMachine(machine);

            ActiveMachine = machine;
            machine.Start();
        }

        private IMachine OpenMachine()
        {
            PromptForFileEventArgs args = new PromptForFileEventArgs(FileTypes.Machine, true);
            //args.FileType = FileTypes.Machine;
            //args.Existing = true;
            PromptForFile?.Invoke(this, args);

            string filepath = args.Filepath;
            if (filepath == null)
            {
                return null;
            }

            string fullFilepath = System.IO.Path.GetFullPath(filepath);
            IMachine machine = Machines.FirstOrDefault(
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
                machine = _model.AddMachine(fullFilepath, _fileSystem, true);
            }

            return machine;
        }

        public bool CloseAll()
        {
            lock (Machines)
            {
                foreach (IMachine machine in Machines)
                {
                    IPersistableMachine persistableMachine = machine as IPersistableMachine;
                    if (persistableMachine == null || persistableMachine.PersistantFilepath != null)
                    {
                        continue;
                    }

                    ConfirmCloseEventArgs args = new ConfirmCloseEventArgs();
                    args.Message = "There are machines which haven't been persisted yet. Are you sure you want to exit?";
                    ConfirmClose?.Invoke(this, args);

                    if (!args.Result)
                    {
                        return false;
                    }

                    break;
                }

                List<IMachine> machines = Machines.ToList();
                foreach (IMachine machine in machines)
                {
                    Close(machine, false);
                }
            }

            return true;
        }

        public bool Close(IMachine coreMachine, bool prompt)
        {
            if (coreMachine == null)
            {
                return false;
            }

            IPersistableMachine pm = coreMachine as IPersistableMachine;
            bool remove = pm == null;
            if (pm != null && pm.PersistantFilepath == null)
            {
                if (prompt)
                {
                    ConfirmCloseEventArgs args = new ConfirmCloseEventArgs();
                    args.Message = String.Format("Are you sure you want to close the \"{0}\" machine without persisting it?", coreMachine.Name);
                    ConfirmClose?.Invoke(this, args);

                    if (!args.Result)
                    {
                        return false;
                    }
                }

                remove = true;
            }

            if (remove)
            {
                _model.RemoveMachine(coreMachine);
            }

            coreMachine.Close();

            return true;
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (Machines)
            {
                int samplesWritten = 0;

                foreach (IMachine machine in Machines)
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

        private void LoadDisc(IInteractiveMachine machine, byte drive)
        {
            if (machine == null)
            {
                return;
            }

            using ((machine as IPausableMachine)?.AutoPause())
            {
                byte[] image = PromptForMedia(true);
                if (image != null)
                {
                    machine.LoadDisc(drive, image);
                }
            }
        }

        private void LoadTape(IInteractiveMachine machine)
        {
            if (machine == null)
            {
                return;
            }

            using ((machine as IPausableMachine)?.AutoPause())
            {
                byte[] image = PromptForMedia(false);
                if (image != null)
                {
                    machine.LoadTape(image);
                }
            }
        }

        private byte[] PromptForMedia(bool disc)
        {
            string expectedExt = disc ? ".dsk" : ".cdt";
            FileTypes type = disc ? FileTypes.Disc : FileTypes.Tape;

            PromptForFileEventArgs args = new PromptForFileEventArgs(type, true);
            PromptForFile?.Invoke(this, args);

            string filename = args.Filepath;
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
                    SelectItemEventArgs args2 = new SelectItemEventArgs();
                    args2.Items = extEntries;
                    SelectItem?.Invoke(this, args2);

                    entry = args2.SelectedItem;
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

        public void Persist(IPersistableMachine machine)
        {
            if (machine == null)
            {
                throw new ArgumentNullException(nameof(machine));
            }

            if (!String.IsNullOrEmpty(machine.PersistantFilepath))
            {
                // Should throw exception here?
                return;
            }

            PromptForFileEventArgs args = new PromptForFileEventArgs(FileTypes.Machine, false);
            PromptForFile?.Invoke(this, args);

            string filepath = args.Filepath;
            machine.Persist(_fileSystem, filepath);
        }

        private void SelectBookmark(IJumpableMachine jumpableMachine)
        {
            if (jumpableMachine == null)
            {
                return;
            }

            using ((jumpableMachine as IPausableMachine)?.AutoPause())
            {
                PromptForBookmarkEventArgs args = new PromptForBookmarkEventArgs();
                PromptForBookmark?.Invoke(this, args);

                HistoryEvent historyEvent = args.SelectedBookmark;
                if (historyEvent != null)
                {
                    jumpableMachine.JumpToBookmark(historyEvent);
                    if (jumpableMachine is IMachine machine)
                    {
                        machine.Status = String.Format("Jumped to {0}", Helpers.GetTimeSpanFromTicks(historyEvent.Ticks).ToString(@"hh\:mm\:ss"));
                    }
                }
            }
        }

        private void RenameMachine(IMachine machine)
        {
            if (machine == null)
            {
                return;
            }

            using ((machine as IPausableMachine)?.AutoPause())
            {
                PromptForNameEventArgs args = new PromptForNameEventArgs();
                args.ExistingName = machine.Name;
                PromptForName?.Invoke(this, args);

                string newName = args.SelectedName;
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

        private void StartServer(UInt16 defaultPort)
        {
            CreateSocketEventArgs args2 = new CreateSocketEventArgs();
            CreateSocket?.Invoke(this, args2);
            ISocket socket = args2.CreatedSocket;

            SelectServerPortEventArgs args = new SelectServerPortEventArgs();
            args.DefaultPort = defaultPort;
            SelectServerPort?.Invoke(this, args);

            UInt16? port = args.SelectedPort;
            if (port.HasValue)
            {
                _machineServer.Start(socket, port.Value);
            }
        }

        private void StopServer()
        {
            _machineServer.Stop();
        }

        public void Connect(ServerInfo serverInfo)
        {
            SelectRemoteMachineEventArgs args = new SelectRemoteMachineEventArgs();
            args.ServerInfo = serverInfo;
            SelectRemoteMachine?.Invoke(this, args);

            RemoteMachine remoteMachine = args.SelectedMachine;
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
