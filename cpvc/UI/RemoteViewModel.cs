using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CPvC
{
    public class ServerInfo
    {
        public string ServerName { get; set; }
        public UInt16 Port { get; set; }

        public ServerInfo(string hostname, UInt16 port)
        {
            ServerName = hostname;
            Port = port;
        }
    }

    public class RemoteViewModel : INotifyPropertyChanged
    {
        private string _selectedMachineName;
        private ServerInfo _serverInfo;
        private IRemote _remote;
        private RemoteMachine _machine;
        private bool _enableLivePreview;

        private ObservableCollection<string> _machineNames;

        public ObservableCollection<string> MachineNames
        {
            get
            {
                return _machineNames;
            }
        }

        public string SelectedMachineName
        {
            get
            {
                return _selectedMachineName;
            }

            set
            {
                SetLivePreview(_enableLivePreview, value);
            }
        }

        public ServerInfo Server
        {
            get
            {
                return _serverInfo;
            }
        }

        public RemoteMachine Machine
        {
            get
            {
                return _machine;
            }
        }

        public bool LivePreviewEnabled
        {
            get
            {
                return _enableLivePreview;
            }

            set
            {
                SetLivePreview(value, _selectedMachineName);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public RemoteViewModel(ServerInfo serverInfo, IRemote remote)
        {
            _serverInfo = serverInfo;
            _enableLivePreview = false;

            _remote = remote;

            _machineNames = new ObservableCollection<string>();

            _remote.ReceiveAvailableMachines += m =>
            {
                _machineNames.Clear();

                foreach (string machineName in m)
                {
                    _machineNames.Add(machineName);
                }
            };

            _remote.SendRequestAvailableMachines();

            _machine = new RemoteMachine(_remote);
        }

        private void SetLivePreview(bool enableLivePreview, string machineName)
        {
            bool previous = (_enableLivePreview && _selectedMachineName != null);
            bool current = (enableLivePreview && machineName != null);

            if (current && (!previous || (_selectedMachineName != machineName)))
            {
                _remote.SendSelectMachine(machineName);
            }
            else if (previous && !current)
            {
                _remote.SendSelectMachine(String.Empty);
            }

            _enableLivePreview = enableLivePreview;
            _selectedMachineName = machineName;

            OnPropertyChanged(nameof(SelectedMachineName));
            OnPropertyChanged(nameof(LivePreviewEnabled));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
