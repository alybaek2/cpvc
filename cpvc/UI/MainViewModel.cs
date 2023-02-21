using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
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
        /// The currently active machine view model.
        /// </summary>
        private MachineViewModel _activeMachine;

        private IFileSystem _fileSystem;

        private Action<Action> _canExecuteChangedInvoker;

        public event PromptForFileEventHandler PromptForFile;
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

        public event PropertyChangedEventHandler PropertyChanged;

        private MachineServerListener _machineServer;

        private ISettings _settings;

        private ViewModelObservableCollection<IMachine, MachineViewModel> _machineViewModels;

        public MainViewModel(ISettings settings, IFileSystem fileSystem, ViewModelFactory<IMachine, MachineViewModel> machineViewModelFactory, Action<Action> canExecuteChangedInvoker)
        {
            _settings = settings;
            _fileSystem = fileSystem;
            _canExecuteChangedInvoker = canExecuteChangedInvoker;

            InitModel(new MainModel(settings, fileSystem));

            _machineServer = new MachineServerListener(Machines);

            LoadRecentServersSetting();

            _allCommands = new List<Command>();

            _machineViewModels = new ViewModelObservableCollection<IMachine, MachineViewModel>(_model.Machines, machineViewModelFactory);
            ActiveMachineViewModel = null;

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
                    return pm?.PersistentFilepath != null;
                }
            );

            _closeCommand = CreateCommand(
                p => Close(p as IMachine, true),
                p => (p as IMachine)?.CanClose ?? false
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

        public ViewModelObservableCollection<IMachine, MachineViewModel> MachineViewModels
        {
            get
            {
                return _machineViewModels;
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

        public MachineViewModel ActiveMachineViewModel
        {
            get
            {
                return _activeMachine;
            }

            set
            {
                _activeMachine = value;

                OnPropertyChanged();

                UpdateCommands(this, null);
                value?.UpdateCommands(this, null);
            }
        }

        public void OpenReplayMachine(string name, HistoryEvent finalEvent)
        {
            ReplayMachine replayMachine = new ReplayMachine(finalEvent);
            replayMachine.Name = name;
            _model.AddMachine(replayMachine);

            ActiveMachineViewModel = _machineViewModels.Get(replayMachine);
            replayMachine.Start();
        }

        private void NewMachine()
        {
            LocalMachine machine = LocalMachine.New("Untitled", null);
            _model.AddMachine(machine);

            ActiveMachineViewModel = _machineViewModels.Get(machine);
            machine.Start().Wait();
        }

        private IMachine OpenMachine()
        {
            PromptForFileEventArgs args = new PromptForFileEventArgs(FileTypes.Machine, true);
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
                        return String.Compare(pm.PersistentFilepath, fullFilepath, true) == 0;
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
            List<IMachine> machines = null;
            lock (Machines)
            {
                machines = Machines.ToList();
            }

            foreach (IMachine machine in machines)
            {
                if (!(machine is IPersistableMachine persistableMachine) || persistableMachine.PersistentFilepath != null)
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

            foreach (IMachine machine in machines)
            {
                Close(machine, false);
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
            if (pm != null && pm.PersistentFilepath == null)
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
            List<IMachine> machines = null;
            lock (Machines)
            {
                machines = Machines.ToList();
            }

            int samplesWritten = 0;

            foreach (IMachine machine in machines)
            {
                // Play audio only from the currently active machine; for the rest, just
                // advance the audio playback position.
                if (ReferenceEquals(machine, ActiveMachineViewModel?.Machine))
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

        public void KeyPress(IInteractiveMachine machine, byte keyCode, bool down)
        {
            machine.Key(keyCode, down);
        }

        public void EnableTurbo(ITurboableMachine machine, bool enable)
        {
            machine.EnableTurbo(enable);
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

            ActiveMachineViewModel = _machineViewModels.Get(remoteMachine);
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
