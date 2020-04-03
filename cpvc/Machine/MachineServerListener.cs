using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineServerListener
    {
        private IEnumerable<Machine> _machines;

        private SocketServer _server;

        public MachineServerListener(IEnumerable<Machine> machines)
        {
            _machines = machines;

            _server = new SocketServer();
            _server.OnClientConnect += ClientConnect;
        }

        private void ClientConnect(SocketConnection socket)
        {
            List<Machine> openMachines = _machines.Where(m => !m.RequiresOpen).ToList();
            MachineServerConnection conn = new MachineServerConnection(socket, openMachines);
            conn.StartHandshake();
        }

        public void Start(UInt16 port)
        {
            _server.Start(port);
        }

        public void Stop()
        {
            _server.Stop();
        }
    }
}
