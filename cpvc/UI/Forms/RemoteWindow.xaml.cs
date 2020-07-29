using System.Windows;
using System.Windows.Input;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for RemoteWindow.xaml
    /// </summary>
    public partial class RemoteWindow : Window
    {
        private RemoteMachine _remoteMachine;
        private ServerInfo _serverInfo;

        private RemoteViewModel _viewModel;

        public RemoteWindow(Window owner, ServerInfo serverInfo)
        {
            InitializeComponent();

            _serverInfo = serverInfo;
            IConnection connection = SocketConnection.ConnectToServer(new Socket(), serverInfo.ServerName, serverInfo.Port);
            IRemote remote = new Remote(connection);

            _viewModel = new RemoteViewModel(serverInfo, remote);

            Owner = owner;

            DataContext = _viewModel;
        }

        public RemoteMachine Machine
        {
            get
            {
                return _remoteMachine;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MachineListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!(_machineListBox.SelectedItem is string machineName))
            {
                return;
            }

            IConnection connection = SocketConnection.ConnectToServer(new Socket(), _serverInfo.ServerName, _serverInfo.Port);
            Remote remote = new Remote(connection);
            RemoteMachine machine = new RemoteMachine(remote);
            remote.SendSelectMachine(machineName);

            _remoteMachine = machine;

            DialogResult = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Make sure we close any open live preview... might be better to handle this in Dispose()?
            if (_viewModel.Machine != null)
            {
                _viewModel.Machine.Close();
            }
        }
    }
}
