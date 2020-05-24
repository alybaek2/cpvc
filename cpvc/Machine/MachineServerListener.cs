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

        private void ClientConnect(SocketConnection socket)
        {
            List<CoreMachine> openMachines = _machineViewModels.Where(m =>
            {
                CoreMachine cm = m.Machine as CoreMachine;
                if (cm is IOpenableMachine om)
                {
                    return !om.RequiresOpen;
                }

                if (cm is Machine)
                {
                    return true;
                }

                return false;
            }).Select(m => m.Machine as CoreMachine).ToList();
            MachineServerConnection conn = new MachineServerConnection(socket, openMachines);

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
