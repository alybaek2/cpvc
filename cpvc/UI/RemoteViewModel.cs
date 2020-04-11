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

            GetMachines(hostname, port);
        }

        private void GetMachines(string serverName, UInt16 port)
        {
            ManualResetEvent e = new ManualResetEvent(false);

            ObservableCollection<RemoteMachineInfo> remoteMachines = new ObservableCollection<RemoteMachineInfo>();

            IConnection connection = SocketConnection.ConnectToServer(serverName, port);
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
        public ObservableCollection<ServerInfo> Servers { get; set; }

        private ServerInfo _selectedServer;
        public ServerInfo SelectedServer
        {
            get
            {
                return _selectedServer;
            }

            set
            {
                _selectedServer = value;

                OnPropertyChanged("SelectedServer");
            }
        }

        private ISettings _settings;

        public event PropertyChangedEventHandler PropertyChanged;

        public RemoteViewModel(ISettings settings)
        {
            Servers = new ObservableCollection<ServerInfo>();
            SelectedServer = null;
            _settings = settings;

            LoadFromSettings();
        }

        public ServerInfo AddServer(string serverName, UInt16 port)
        {
            ServerInfo info = null;
            if (serverName.Length > 0 && !Servers.Any(s => s.ServerName == serverName)) //_serverComboBox.Items.Contains(serverName))
            {
                info = new ServerInfo(serverName, port);

                Servers.Add(info);

                UpdateSettings();
            }

            return info;
        }

        private void LoadFromSettings()
        {
            IEnumerable<string> serversAndPorts = Helpers.SplitWithEscape(';', _settings.RemoteServers);

            lock (Servers)
            {
                foreach (string serverStr in serversAndPorts)
                {
                    List<string> tokens = Helpers.SplitWithEscape(':', serverStr);
                    if (tokens.Count < 2)
                    {
                        continue;
                    }

                    ServerInfo info = new ServerInfo(tokens[0], System.Convert.ToUInt16(tokens[1]));
                    
                    Servers.Add(info);
                }
            }
        }

        private void UpdateSettings()
        {
            IEnumerable<string> serversAndPorts = Servers.Select(s => Helpers.JoinWithEscape(':', new string[] { s.ServerName, s.Port.ToString() }));

            string setting = Helpers.JoinWithEscape(';', serversAndPorts);

            _settings.RemoteServers = setting;
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
