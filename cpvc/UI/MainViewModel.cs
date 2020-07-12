using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
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
        public delegate RemoteMachine SelectRemoteMachineDelegate(ServerInfo serverInfo);
        public delegate UInt16? SelectServerPortDelegate(UInt16 defaultPort);

        /// <summary>
        /// The data model associated with this view model.
        /// </summary>
        private readonly MainModel _model;

        private ObservableCollection<ServerInfo> _recentServers;
        private ObservableCollection<MachineViewModel> _machineViewModels;

        /// <summary>
        /// The currently active item. Will be either this instance (the Home tab) or a Machine.
        /// </summary>
        private object _active;

        private IFileSystem _fileSystem;
        private SelectItemDelegate _selectItem;
        private PromptForFileDelegate _promptForFile;
        private PromptForBookmarkDelegate _promptForBookmark;
        private PromptForNameDelegate _promptForName;
        private SelectRemoteMachineDelegate _selectRemoteMachine;
        private SelectServerPortDelegate _selectServerPort;

        private Command _openMachineCommand;
        private Command _newMachineCommand;
        private Command _startServerCommand;
        private Command _stopServerCommand;
        private Command _connectCommand;

        private MachineViewModel _nullMachineViewModel;
        public event PropertyChangedEventHandler PropertyChanged;

        private MachineServerListener _machineServer;

        private ISettings _settings;

        public MainViewModel(ISettings settings, IFileSystem fileSystem, SelectItemDelegate selectItem, PromptForFileDelegate promptForFile, PromptForBookmarkDelegate promptForBookmark, PromptForNameDelegate promptForName, ReportErrorDelegate reportError, SelectRemoteMachineDelegate selectRemoteMachine, SelectServerPortDelegate selectServerPort)
        {
            _settings = settings;
            _fileSystem = fileSystem;
            _selectItem = selectItem;
            _promptForFile = promptForFile;
            _promptForBookmark = promptForBookmark;
            _promptForName = promptForName;
            _selectRemoteMachine = selectRemoteMachine;
            _selectServerPort = selectServerPort;

            _nullMachineViewModel = new MachineViewModel(this, null, null, null, null, null, null);

            _model = new MainModel(settings, fileSystem);

            _machineViewModels = new ObservableCollection<MachineViewModel>();
            for (int i = 0; i < _model.Machines.Count; i++)
            {
                MachineViewModel machineViewModel = CreateMachineViewModel(_model.Machines[i]);
                AddMachineViewModel(machineViewModel);

                Command removeMachineCommand = new Command(
                    p => Remove(machineViewModel),
                    p => true
                );

                machineViewModel.RemoveCommand = removeMachineCommand;
            }

            _machineServer = new MachineServerListener(MachineViewModels.Select(vm => vm.Machine).Where(m => m.Core != null));

            LoadRecentServersSetting();

            ActiveItem = this;

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
        }

        public ObservableCollection<ServerInfo> RecentServers
        {
            get
            {
                return _recentServers;
            }
        }

        public ObservableCollection<Machine> Machines
        {
            get
            {
                return _model.Machines;
            }
        }

        public ObservableCollection<MachineViewModel> MachineViewModels
        {
            get
            {
                return _machineViewModels;
            }
        }

        public Command OpenMachineCommand
        {
            get
            {
                return _openMachineCommand;
            }
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
                OnPropertyChanged("ActiveMachineViewModel");
            }
        }

        public MachineViewModel ActiveMachineViewModel
        {
            get
            {
                return _active as MachineViewModel ?? _nullMachineViewModel;
            }

            set
            {
                if (value == null)
                {
                    _active = null;
                }
                else
                {
                    _active = value;

                    if (value.Machine is IOpenableMachine machine && machine.RequiresOpen)
                    {
                        machine.Open();
                    }
                }

                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachineViewModel");
            }
        }

        public void OpenReplayMachine(string name, HistoryEvent finalEvent)
        {
            ReplayMachine replayMachine = new ReplayMachine(finalEvent);
            replayMachine.Name = name;

            MachineViewModel machineViewModel = CreateMachineViewModel(replayMachine);
            AddMachineViewModel(machineViewModel);

            ActiveMachineViewModel = machineViewModel;
        }

        public Machine NewMachine(PromptForFileDelegate promptForFile, IFileSystem fileSystem)
        {
            string filepath = promptForFile(FileTypes.Machine, false);

            Machine machine = _model.Add(filepath, fileSystem);
            if (machine != null)
            {
                machine.Start();
                MachineViewModel machineViewModel = CreateMachineViewModel(machine);
                AddMachineViewModel(machineViewModel);
                ActiveMachineViewModel = machineViewModel;
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
                MachineViewModel machineViewModel = CreateMachineViewModel(machine);
                AddMachineViewModel(machineViewModel);
                ActiveMachineViewModel = machineViewModel;
            }

            return machine;
        }

        public void Remove(MachineViewModel viewModel)
        {
            if (viewModel.Machine is IClosableMachine closableMachine)
            {
                closableMachine.Close();
            }

            _model.Remove(viewModel.Machine as Machine);

            RemoveMachineViewModel(viewModel);
        }

        public void CloseAll()
        {
            foreach (Machine machine in _model.Machines)
            {
                machine.Close();
            }
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (MachineViewModels)
            {
                int samplesWritten = 0;

                foreach (MachineViewModel machineViewModel in MachineViewModels)
                {
                    ICoreMachine machine = machineViewModel.Machine;

                    // Play audio only from the currently active machine; for the rest, just
                    // advance the audio playback position.
                    if (machine == ActiveMachineViewModel.Machine)
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

        private MachineViewModel CreateMachineViewModel(ICoreMachine machine)
        {
            MachineViewModel machineViewModel = new MachineViewModel(this, machine, _fileSystem, _promptForFile, _promptForBookmark, _promptForName, _selectItem);

            Command removeMachineCommand = new Command(
                p => Remove(machineViewModel),
                p => true
            );

            machineViewModel.RemoveCommand = removeMachineCommand;

            return machineViewModel;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void StartServer(UInt16 defaultPort)
        {
            UInt16? port = _selectServerPort(defaultPort);
            if (port.HasValue)
            {
                _machineServer.Start(new Socket(), port.Value);
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

            UpdateRecentServersSettings();

            MachineViewModel machineViewModel = CreateMachineViewModel(remoteMachine);
            AddMachineViewModel(machineViewModel);

            ActiveMachineViewModel = machineViewModel;
        }

        private void AddMachineViewModel(MachineViewModel machineViewModel)
        {
            lock (MachineViewModels)
            {
                MachineViewModels.Add(machineViewModel);
            }
        }

        private void RemoveMachineViewModel(MachineViewModel machineViewModel)
        {
            lock (MachineViewModels)
            {
                MachineViewModels.Remove(machineViewModel);
            }
        }

        private void UpdateRecentServersSettings()
        {
            IEnumerable<string> serversAndPorts = RecentServers.Select(s => Helpers.JoinWithEscape(':', new string[] { s.ServerName, s.Port.ToString() }));

            string setting = Helpers.JoinWithEscape(';', serversAndPorts);

            _settings.RemoteServers = setting;
        }

        private void LoadRecentServersSetting()
        {
            if (_settings.RemoteServers == null)
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
