using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;

namespace CPvC
{
    public class RemoteMachineInfo
    {
        public ServerInfo ServerInfo { get; set; }
        public string MachineName { get; set; }

        public RemoteMachineInfo(string name, ServerInfo server)
        {
            ServerInfo = server;
            MachineName = name;
        }
    }

    public class ServerInfo
    {
        private ObservableCollection<RemoteMachineInfo> _machines;

        public ObservableCollection<RemoteMachineInfo> Machines
        {
            get
            {
                return _machines;
            }

            set
            {
                _machines = value;
            }
        }

        public string ServerName { get; set; }
        public UInt16 Port { get; set; }

        public ServerInfo(string hostname, UInt16 port)
        {
            ServerName = hostname;
            Port = port;
        }

        public bool GetMachines()
        {
            ManualResetEvent e = new ManualResetEvent(false);

            ObservableCollection<RemoteMachineInfo> remoteMachines = new ObservableCollection<RemoteMachineInfo>();

            IConnection connection = SocketConnection.ConnectToServer(new Socket(), ServerName, Port);
            if (connection == null)
            {
                return false;
            }

            using (Remote remote = new Remote(connection))
            {
                remote.ReceiveAvailableMachines += machineNames =>
                {
                    foreach (string machineName in machineNames)
                    {
                        RemoteMachineInfo info = new RemoteMachineInfo(machineName, this);

                        remoteMachines.Add(info);
                    }

                    e.Set();
                };

                remote.SendRequestAvailableMachines();

                e.WaitOne(1000);
            }

            Machines = remoteMachines;

            return true;
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

            set
            {
                _serverInfo = value;
            }
        }

        public RemoteMachine Machine
        {
            get
            {
                return _machine;
            }

            set
            {
                _machine = value;
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
            Server = serverInfo;
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
                _remote.SendSelectMachine(machineName ?? String.Empty);
            }
            else if (previous && !current)
            {
                _remote.SendSelectMachine(String.Empty);
            }

            _enableLivePreview = enableLivePreview;
            _selectedMachineName = machineName;

            OnPropertyChanged("SelectedMachineName");
            OnPropertyChanged("LivePreviewEnabled");
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
