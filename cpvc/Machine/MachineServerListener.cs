using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineServerListener
    {
        private IEnumerable<MachineViewModel> _machineViewModels;
        private SocketServer _server;
        private List<MachineServerConnection> _connections;

        public MachineServerListener(IEnumerable<MachineViewModel> machines)
        {
            _machineViewModels = machines;
            _connections = new List<MachineServerConnection>();

            _server = new SocketServer();
            _server.OnClientConnect += ClientConnect;
        }

        public IEnumerable<MachineServerConnection> Connections
        {
            get
            {
                return _connections;
            }
        }

        private void ClientConnect(SocketConnection socket)
        {
            IEnumerable<ICoreMachine> openMachines = _machineViewModels.Where(m => m.Machine?.Core != null).Select(m => m.Machine);
            MachineServerConnection conn = new MachineServerConnection(new Remote(socket), openMachines);

            _connections.Add(conn);
        }

        public void Start(ISocket socket, UInt16 port)
        {
            _server.Start(socket, port);
        }

        public void Stop()
        {
            _server.Stop();
        }
    }
}
