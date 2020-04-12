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
        public RemoteMachine Machine { get; set; }

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

        public void GetMachines()
        {
            ManualResetEvent e = new ManualResetEvent(false);

            ObservableCollection<RemoteMachineInfo> remoteMachines = new ObservableCollection<RemoteMachineInfo>();

            IConnection connection = SocketConnection.ConnectToServer(ServerName, Port);
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

            e.WaitOne(2000);

            Machines = remoteMachines;
        }
    }

    public class RemoteViewModel : INotifyPropertyChanged
    {
        //public ObservableCollection<ServerInfo> Servers { get; set; }

        private RemoteMachineInfo _selectedMachine;
        private ServerInfo _serverInfo;

        public RemoteMachineInfo SelectedMachine
        {
            get
            {
                return _selectedMachine;
            }

            set
            {
                if (_selectedMachine != null)
                {
                    _selectedMachine?.Machine.Close();
                    _selectedMachine = null;
                }

                if (value != null)
                {
                    IConnection connection = SocketConnection.ConnectToServer(value.ServerInfo.ServerName, value.ServerInfo.Port);
                    Remote remote = new Remote(connection);
                    RemoteMachine machine = new RemoteMachine(remote);
                    remote.SendSelectMachine(value.MachineName);

                    value.Machine = machine;
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

        private ISettings _settings;

        public event PropertyChangedEventHandler PropertyChanged;

        public RemoteViewModel(ISettings settings)
        {
            Server = null;
            _settings = settings;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
