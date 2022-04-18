using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

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

        private Action<Action> _canExecuteChangedInvoker;

        public event PromptForFileEventHandler PromptForFile;
        public event SelectItemEventHandler SelectItem;
        public event PromptForBookmarkEventHandler PromptForBookmark;
        public event PromptForNameEventHandler PromptForName;
        public event SelectRemoteMachineEventHandler SelectRemoteMachine;
        public event SelectServerPortEventHandler SelectServerPort;
        public event ConfirmCloseEventHandler ConfirmClose;
        public event CreateSocketEventHandler CreateSocket;

        private List<Command> _allCommands;

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

        public MainViewModel(ISettings settings, IFileSystem fileSystem, Action<Action> canExecuteChangedInvoker)
        {
            _settings = settings;
            _fileSystem = fileSystem;
            _canExecuteChangedInvoker = canExecuteChangedInvoker;

            InitModel(new MainModel(settings, fileSystem));

            _machineServer = new MachineServerListener(Machines);

            LoadRecentServersSetting();

            _allCommands = new List<Command>();

            ActiveMachine = null;

            _openMachineCommand = CreateCommand(
                p => OpenMachine(),
                p => true
            );

            _newMachineCommand = CreateCommand(
                p => NewMachine(),
                p => true
            );

            _startServerCommand = CreateCommand(
                p => StartServer(6128),
                p => true
            );

            _stopServerCommand = CreateCommand(
                p => StopServer(),
                p => true
            );

            _connectCommand = CreateCommand(
                p => Connect(p as ServerInfo),
                p => true
            );

            _removeCommand = CreateCommand(
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
                    return pm?.PersistantFilepath != null;
                }
            );

            _closeCommand = CreateCommand(
                p => Close(p as IMachine, true),
                p => (p as IMachine)?.CanClose ?? false
            );

            _openCommand = CreateCommand(
                p => (p as IPersistableMachine)?.OpenFromFile(_fileSystem),
                p => !(p as IPersistableMachine)?.IsOpen ?? false
            );

            _persistCommand = CreateCommand(
                p => Persist(p as IPersistableMachine),
                p =>
                {
                    if (p is IPersistableMachine pm)
                    {
                        return pm.PersistantFilepath == null;
                    }

                    return false;
                });

            _pauseCommand = CreateCommand(
                p => (p as IPausableMachine)?.Stop(),
                p => (p as IPausableMachine)?.CanStop ?? false
            );

            _resumeCommand = CreateCommand(
                p => (p as IPausableMachine)?.Start(),
                p => (p as IPausableMachine)?.CanStart ?? false
            );

            _resetCommand = CreateCommand(
                p => (p as IInteractiveMachine)?.Reset(),
                p => p is IInteractiveMachine
            );

            _driveACommand = CreateCommand(
                p => LoadDisc(p as IInteractiveMachine, 0),
                p => p is IInteractiveMachine
            );

            _driveAEjectCommand = CreateCommand(
                p => (p as IInteractiveMachine)?.LoadDisc(0, null),
                p => p is IInteractiveMachine
            );

            _driveBCommand = CreateCommand(
                p => LoadDisc(p as IInteractiveMachine, 1),
                p => p is IInteractiveMachine
            );

            _driveBEjectCommand = CreateCommand(
                p => (p as IInteractiveMachine)?.LoadDisc(1, null),
                p => p is IInteractiveMachine
            );

            _tapeCommand = CreateCommand(
                p => LoadTape(p as IInteractiveMachine),
                p => p is IInteractiveMachine
            );

            _tapeEjectCommand = CreateCommand(
                p => (p as IInteractiveMachine)?.LoadTape(null),
                p => p is IInteractiveMachine
            );

            _toggleRunningCommand = CreateCommand(
                p => (p as IPausableMachine)?.ToggleRunning(),
                p => p is IPausableMachine
            );

            _addBookmarkCommand = CreateCommand(
                p => (p as IBookmarkableMachine)?.AddBookmark(false),
                p => p is IBookmarkableMachine
            );

            _jumpToMostRecentBookmarkCommand = CreateCommand(
                p => (p as IJumpableMachine)?.JumpToMostRecentBookmark(),
                p => p is IJumpableMachine
            );

            _browseBookmarksCommand = CreateCommand(
                p => SelectBookmark(p as IJumpableMachine),
                p => p is IJumpableMachine
            );

            _compactCommand = CreateCommand(
                p => (p as ICompactableMachine)?.Compact(_fileSystem),
                p => (p as ICompactableMachine)?.CanCompact ?? false
            );

            _renameCommand = CreateCommand(
                p => RenameMachine(p as IMachine),
                p => p is IMachine
            );

            _seekToNextBookmarkCommand = CreateCommand(
                p => (p as IPrerecordedMachine)?.SeekToNextBookmark(),
                p => p is IPrerecordedMachine
            );

            _seekToPrevBookmarkCommand = CreateCommand(
                p => (p as IPrerecordedMachine)?.SeekToPreviousBookmark(),
                p => p is IPrerecordedMachine
            );

            _seekToStartCommand = CreateCommand(
                p => (p as IPrerecordedMachine)?.SeekToStart(),
                p => p is IPrerecordedMachine
            );

            _reverseStartCommand = CreateCommand(
                p => (p as IReversibleMachine)?.Reverse(),
                p => p is IReversibleMachine
            );

            _reverseStopCommand = CreateCommand(
                p => (p as IReversibleMachine)?.ReverseStop(),
                p => p is IReversibleMachine
            );

            _toggleSnapshotCommand = CreateCommand(
                p => (p as IReversibleMachine)?.ToggleReversibilityEnabled(),
                p => p is IReversibleMachine
            );
        }

        private Command CreateCommand(Action<object> execute, Predicate<object> canExecute)
        {
            Command command = new Command(execute, canExecute, _canExecuteChangedInvoker);

            _allCommands.Add(command);

            return command;
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

            ((INotifyCollectionChanged)mainModel.Machines).CollectionChanged += Machines_CollectionChanged;

            foreach (Machine machine in mainModel.Machines)
            {
                machine.PropertyChanged += Machine_PropertyChanged;
            }
        }

        private void Machines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (IMachine machine in e.NewItems)
                    {
                        machine.PropertyChanged += Machine_PropertyChanged;
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (IMachine machine in e.OldItems)
                    {
                        machine.PropertyChanged -= Machine_PropertyChanged;
                    }

                    break;
            }

            UpdateCommands(sender, e);
        }

        private void Machine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(Machine.Ticks))
            {
                UpdateCommands(sender, e);
            }
        }

        private void UpdateCommands(object sender, EventArgs e)
        {
            foreach (Command command in _allCommands)
            {
                command.InvokeCanExecuteChanged(sender, e);
            }
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

        public Command StartServerCommand
        {
            get { return _startServerCommand; }
        }

        public Command StopServerCommand
        {
            get { return _stopServerCommand; }
        }

        public Command ConnectCommand
        {
            get { return _connectCommand; }
        }

        public Command RemoveCommand
        {
            get { return _removeCommand; }
        }

        public Command CloseCommand
        {
            get { return _closeCommand; }
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

        public Command ReverseStopCommand
        {
            get { return _reverseStopCommand; }
        }

        public Command ToggleReversibility
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

                UpdateCommands(this, null);
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

                UpdateCommands(this, null);
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
                    if (!(machine is IPersistableMachine persistableMachine) || persistableMachine.PersistantFilepath != null)
                    {
                        continue;
                    }

                    ConfirmCloseEventArgs args = new ConfirmCloseEventArgs("There are machines which haven't been persisted yet. Are you sure you want to exit?");
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
                    ConfirmCloseEventArgs args = new ConfirmCloseEventArgs(String.Format("Are you sure you want to close the \"{0}\" machine without persisting it?", coreMachine.Name));
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

            using ((machine as IPausableMachine)?.AutoPause())
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

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void StartServer(UInt16 defaultPort)
        {
            CreateSocketEventArgs createSocketArgs = new CreateSocketEventArgs();
            CreateSocket?.Invoke(this, createSocketArgs);
            ISocket socket = createSocketArgs.CreatedSocket;

            SelectServerPortEventArgs selectPortArgs = new SelectServerPortEventArgs(defaultPort);
            SelectServerPort?.Invoke(this, selectPortArgs);

            UInt16? port = selectPortArgs.SelectedPort;
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
            SelectRemoteMachineEventArgs args = new SelectRemoteMachineEventArgs(serverInfo);
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
