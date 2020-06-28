using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineServerListener
    {
        private IEnumerable<ICoreMachine> _machines;
        private SocketServer _server;
        private List<MachineServerConnection> _connections;

        public MachineServerListener(IEnumerable<ICoreMachine> machines)
        {
            _machines = machines;
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
            MachineServerConnection conn = new MachineServerConnection(new Remote(socket), _machines);

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
