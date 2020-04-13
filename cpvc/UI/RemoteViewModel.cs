using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

            IConnection connection = SocketConnection.ConnectToServer(ServerName, Port);
            if (connection == null)
            {
                return false;
            }

            Remote remote = new Remote(connection);
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

            e.WaitOne(10000);

            Machines = remoteMachines;

            return true;
        }
    }

    public class RemoteViewModel : INotifyPropertyChanged
    {
        private RemoteMachineInfo _selectedMachine;
        private ServerInfo _serverInfo;
        private IConnection _connection;
        private Remote _remote;
        private RemoteMachine _machine;

        public RemoteMachineInfo SelectedMachine
        {
            get
            {
                return _selectedMachine;
            }

            set
            {
                if (value != null)
                {
                    _remote.SendSelectMachine(value.MachineName);
                }
                else
                {
                    _remote.SendSelectMachine("");
                }

                _selectedMachine = value;

                OnPropertyChanged("SelectedMachine");
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

        private ISettings _settings;

        public event PropertyChangedEventHandler PropertyChanged;

        public RemoteViewModel(ServerInfo serverInfo, ISettings settings)
        {
            Server = serverInfo;
            _settings = settings;

            _connection = SocketConnection.ConnectToServer(serverInfo.ServerName, serverInfo.Port);
            _remote = new Remote(_connection);
            _machine = new RemoteMachine(_remote);

        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
