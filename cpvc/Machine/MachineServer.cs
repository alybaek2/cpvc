using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineServer
    {
        private Machine _machine;

        private SocketServer _server;

        // Only one client allowed for now. Add more later?
        private List<IConnection> _clients;

        public MachineServer(Machine machine)
        {
            _machine = machine;
            _machine.Auditors += MachineAuditor;

            _clients = new List<IConnection>();

            _server = new SocketServer();
            _server.OnClientConnect += ClientConnectDelegate;
        }

        private void ClientConnectDelegate(SocketConnection socket)
        {
            using (_machine.AutoPause())
            {
                byte[] state = _machine.Core.GetState();

                CoreAction loadCoreAction = CoreAction.LoadCore(0, new MemoryBlob(state));

                byte[] msg = MachineServer.SerializeAction(loadCoreAction);
                socket.SendMessage(msg);

                _clients.Add(socket);
            }
        }

        private void MachineAuditor(CoreAction coreAction)
        {
            SendAction(coreAction);
        }

        public void Start(UInt16 port)
        {
            _server.Start(port);
        }

        public void Stop()
        {
            _server.Stop();
        }

        private void SendAction(CoreAction coreAction)
        {
            byte[] blob = SerializeAction(coreAction);

            foreach (IConnection client in _clients)
            {
                client.SendMessage(blob);
            }
        }

        static public byte[] SerializeAction(CoreAction action)
        {
            MemoryByteStream bs = new MemoryByteStream();
            MachineFile m = new MachineFile(bs);

            m.WriteCoreAction(0, action.Ticks, action);

            bs.Position = 0;
            byte[] msg = new byte[bs.Length];
            bs.ReadBytes(msg, msg.Length);
            bs.Clear();

            return msg;
        }

        static public CoreAction Deserialize(byte[] bytes)
        {
            MemoryByteStream bs = new MemoryByteStream(bytes);
            MachineFile m = new MachineFile(bs);

            CoreAction coreAction = m.ReadCoreAction();

            return coreAction;
        }
    }
}
