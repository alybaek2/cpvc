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

        private Queue<byte[]> _actions;

        private SocketServer _server;

        public MachineServer(Machine machine)
        {
            _machine = machine;

            _machine.Auditors += MachineAuditor;

            _actions = new Queue<byte[]>();

            _server = new SocketServer();

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
            lock (_actions)
            {
                byte[] blob = SerializeAction(coreAction);

                String msg = String.Format("Sending @{0}: {1} StopTicks {2}", coreAction.Ticks, coreAction.Type, coreAction.StopTicks);
                Diagnostics.Trace(msg);

                _server.SendMessage(blob);

                _actions.Enqueue(blob);
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
