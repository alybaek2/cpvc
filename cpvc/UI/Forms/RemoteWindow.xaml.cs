using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CPvC.UI.Forms
{
    /// <summary>
    /// Interaction logic for RemoteWindow.xaml
    /// </summary>
    public partial class RemoteWindow : Window
    {
        private RemoteMachine _remoteMachine;

        private RemoteViewModel _viewModel;

        public RemoteWindow(Window owner, ServerInfo serverInfo)
        {
            InitializeComponent();

            _viewModel = new RemoteViewModel(serverInfo, new Settings());

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

        private void _machineListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            RemoteMachineInfo info = _machineListBox.SelectedItem as RemoteMachineInfo;
            if (info == null)
            {
                return;
            }

            IConnection connection = SocketConnection.ConnectToServer(info.ServerInfo.ServerName, info.ServerInfo.Port);
            Remote remote = new Remote(connection);
            RemoteMachine machine = new RemoteMachine(remote);
            remote.SendSelectMachine(info.MachineName);

            _remoteMachine = machine;

            DialogResult = true;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Make sure we close any open live preview... might be better to handle this in Dispose()?
            if (_viewModel.Machine != null)
            {
                _viewModel.Machine.Close();
                _viewModel.Machine = null;
                _viewModel.SelectedMachine = null;
            }
        }
    }
}
