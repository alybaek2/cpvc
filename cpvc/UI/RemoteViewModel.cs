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
                info = new ServerInfo();
                info.ServerName = serverName;
                info.Port = port;
                info.Machines = new ObservableCollection<RemoteMachineInfo>();

                List<string> machineNames = GetMachines(serverName, port);

                foreach (string machineName in machineNames)
                {
                    RemoteMachineInfo remoteMachineInfo = new RemoteMachineInfo();
                    remoteMachineInfo.ServerInfo = info;
                    remoteMachineInfo.MachineName = machineName;

                    info.Machines.Add(remoteMachineInfo);
                }

                Servers.Add(info);

                UpdateSettings();
            }

            return info;
        }

        private List<string> GetMachines(string serverName, UInt16 port)
        {
            ManualResetEvent e = new ManualResetEvent(false);

            List<string> machineNames = new List<string>();
            IConnection connection = SocketConnection.ConnectToServer(serverName, port);
            Remote remote = new Remote(connection);
            remote.ReceiveAvailableMachines += m =>
            {
                machineNames = m;
                e.Set();
            };

            remote.SendRequestAvailableMachines();

            e.WaitOne(2000);

            return machineNames;
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

                    ServerInfo info = new ServerInfo();
                    info.ServerName = tokens[0];
                    info.Port = System.Convert.ToUInt16(tokens[1]);
                    info.Machines = new ObservableCollection<RemoteMachineInfo>();

                    List<string> machineNames = GetMachines(info.ServerName, info.Port);

                    foreach (string machineName in machineNames)
                    {
                        RemoteMachineInfo remoteMachineInfo = new RemoteMachineInfo();
                        remoteMachineInfo.ServerInfo = info;
                        remoteMachineInfo.MachineName = machineName;

                        info.Machines.Add(remoteMachineInfo);
                    }


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
