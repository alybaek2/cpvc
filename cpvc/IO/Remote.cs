using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class Remote
    {
        private IConnection _connection;
        private IRemoteReceiver _receiver;

        public Remote(IConnection connection, IRemoteReceiver receiver)
        {
            _receiver = receiver;
            _connection = connection;
            _connection.OnNewMessage += OnNewMessage;
        }

        public void SendCoreAction(CoreAction coreAction)
        {
            MemoryByteStream bs = new MemoryByteStream();
            Serializer.CoreActionToBytes(bs, coreAction);

            _connection.SendMessage(bs.AsBytes());
        }

        public void SendSelectMachine(string machineName)
        {
            MemoryByteStream bs = new MemoryByteStream();
            Serializer.SelectMachineToBytes(bs, machineName);

            _connection.SendMessage(bs.AsBytes());
        }

        public void SendAvailableMachines(IEnumerable<string> availableMachines)
        {
            MemoryByteStream bs = new MemoryByteStream();
            Serializer.AvailableMachinesToBytes(bs, availableMachines);

            _connection.SendMessage(bs.AsBytes());
        }

        private void OnNewMessage(byte[] msg)
        {
            MemoryByteStream bs = new MemoryByteStream(msg);

            byte id = msg[0];

            switch (id)
            {
                case Serializer._idSelectMachine:
                    {
                        string machineName = Serializer.SelectMachineFromBytes(bs);

                        _receiver?.ReceiveSelectMachine(machineName);
                    }
                    break;
                case Serializer._idAvailableMachines:
                    {
                        List<string> machines = Serializer.AvailableMachinesFromBytes(bs);

                        _receiver?.ReceiveAvailableMachines(machines);
                    }
                    break;
                case Serializer._idKeyPress:
                case Serializer._idReset:
                case Serializer._idLoadDisc:
                case Serializer._idLoadTape:
                case Serializer._idRunUntil:
                case Serializer._idLoadCore:
                case Serializer._idCoreVersion:
                    {
                        CoreAction coreAction = Serializer.CoreActionFromBytes(bs);
                        _receiver?.ReceiveCoreAction(coreAction);
                    }
                    break;
            }
        }
    }
}
