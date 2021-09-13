using System;
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
        private ConfirmCloseDelegate _confirmClose;
        private ReportErrorDelegate _reportError;

        private Command _openMachineCommand;
        private Command _newMachineCommand;
        private Command _startServerCommand;
        private Command _stopServerCommand;
        private Command _connectCommand;
        private Command _removeCommand;
        private Command _closeCommand;
        private Command _persistCommand;

        private MachineViewModel _nullMachineViewModel;
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

            _nullMachineViewModel = new MachineViewModel(null, null, null, null, null, null, null, null);

            InitModel(new MainModel(settings, fileSystem));

            _machineViewModels = new ObservableCollection<MachineViewModel>();

            foreach (ICoreMachine machine in _model.Machines)
            {
                AddMachine(machine);
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
                    MachineViewModel machineViewModel = (MachineViewModel)p;
                    if (machineViewModel.Close(confirmClose))
                    {
                        Remove(machineViewModel, _confirmClose);
                    }
                },
                p =>
                {
                    MachineViewModel machineViewModel = (MachineViewModel)p;
                    IPersistableMachine pm = (IPersistableMachine)machineViewModel?.Machine;
                    return (pm?.PersistantFilepath != null);
                }
            );

            _closeCommand = new Command(
                p =>
                {
                    MachineViewModel machineViewModel = (MachineViewModel)p;
                    if (machineViewModel.Close(confirmClose))
                    {
                        IPersistableMachine pm = machineViewModel.Machine as IPersistableMachine;
                        if (pm?.PersistantFilepath == null)
                        {
                            Remove(machineViewModel, confirmClose);
                        }
                    }
                },
                p =>
                {
                    MachineViewModel machineViewModel = (MachineViewModel)p;
                    if (machineViewModel == null)
                    {
                        return false;
                    }

                    IPersistableMachine pm = machineViewModel?.Machine as IPersistableMachine;
                    return (pm?.IsOpen ?? true);
                }
            );

            _persistCommand = new Command(
                p =>
                {
                    MachineViewModel machineViewModel = (MachineViewModel)p;
                    try
                    {
                        machineViewModel.Persist(fileSystem, promptForFile);
                    }
                    catch (Exception ex)
                    {
                        reportError(ex.Message);
                    }
                },
                p =>
                {
                    MachineViewModel machineViewModel = (MachineViewModel)p;
                    IPersistableMachine pm = machineViewModel.Machine as IPersistableMachine;
                    if (pm != null)
                    {
                        return pm.PersistantFilepath == null;
                    }

                    return false;
                });

        }

        private void InitModel(MainModel mainModel)
        {
            if (_model != null)
            {
                INotifyCollectionChanged machines = _model.Machines;
                machines.CollectionChanged -= Machines_CollectionChanged;
            }

            _model = mainModel;

            if (_model != null)
            {
                INotifyCollectionChanged machines = _model.Machines;
                machines.CollectionChanged += Machines_CollectionChanged;
            }
        }

        private void AddMachine(ICoreMachine machine)
        {
            MachineViewModel machineViewModel = CreateMachineViewModel(machine);
            AddMachineViewModel(machineViewModel);
        }

        private void RemoveMachine(ICoreMachine machine)
        {
            lock (_machineViewModels)
            {
                MachineViewModel machineViewModel = _machineViewModels.Where(m => m.Machine == machine).First();
                MachineViewModels.Remove(machineViewModel);
            }
        }

        private void Machines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (object item in e.NewItems)
                        {
                            AddMachine((ICoreMachine)item);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (object item in e.OldItems)
                        {
                            RemoveMachine((ICoreMachine)item);
                        }
                    }
                    break;
                default:
                    break;
            }
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

        public ObservableCollection<MachineViewModel> MachineViewModels
        {
            get { return _machineViewModels; }
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
                OnPropertyChanged();
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
                _active = value;

                OnPropertyChanged("ActiveItem");
                OnPropertyChanged();
            }
        }

        public void OpenReplayMachine(string name, HistoryEvent finalEvent)
        {
            ReplayMachine replayMachine = new ReplayMachine(finalEvent);
            replayMachine.Name = name;
            _model.AddMachine(replayMachine);

            SetActive(replayMachine);
            replayMachine.Start();
        }

        public void NewMachine(PromptForFileDelegate promptForFile, IFileSystem fileSystem)
        {
            Machine machine = Machine.Create("Untitled", null);
            _model.AddMachine(machine);

            SetActive(machine);
            machine.Start();
        }

        public MachineViewModel OpenMachine(PromptForFileDelegate promptForFile, string filepath, IFileSystem fileSystem)
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
            MachineViewModel machineViewModel = _machineViewModels.FirstOrDefault(
                m => {
                    if (m is IPersistableMachine pm)
                    {
                        return String.Compare(pm.PersistantFilepath, fullFilepath, true) == 0;
                    }

                    return false;
                });
            if (machineViewModel == null)
            {
                _model.AddMachine(filepath, fileSystem);
            }

            return machineViewModel;
        }

        public void Remove(MachineViewModel viewModel, ConfirmCloseDelegate confirmClose)
        {
            if (viewModel.Close(confirmClose))
            {
                _model.RemoveMachine(viewModel.Machine);
            }
        }

        public void CloseAll()
        {
            lock (MachineViewModels)
            {
                foreach (MachineViewModel model in MachineViewModels)
                {
                    model.Close(_confirmClose);
                }
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
                    if (machine == null)
                    {
                        continue;
                    }

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
            MachineViewModel machineViewModel = new MachineViewModel(machine, _fileSystem, _promptForFile, _promptForBookmark, _promptForName, _selectItem, _confirmClose, _reportError);

            return machineViewModel;
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

            SetActive(remoteMachine);
        }

        private void AddMachineViewModel(MachineViewModel machineViewModel)
        {
            lock (MachineViewModels)
            {
                MachineViewModels.Add(machineViewModel);
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

        private void SetActive(ICoreMachine machine)
        {
            lock (_machineViewModels)
            {
                MachineViewModel vm = _machineViewModels.FirstOrDefault(x => x.Machine == machine);

                ActiveMachineViewModel = vm;
            }
        }
    }
}
