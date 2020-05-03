using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void ReceiveCoreActionDelegate(CoreAction action);
    public delegate void ReceiveSelectMachineDelegate(string machineName);
    public delegate void ReceiveRequestAvailableMachinesDelegate();
    public delegate void ReceiveAvailableMachinesDelegate(List<string> availableMachines);
    public delegate void ReceivePingDelegate(bool response, UInt64 id);
    public delegate void ReceiveNameDelegate(string name);
    public delegate void ReceiveCoreRequestDelegate(CoreRequest request);

    public class Remote: IDisposable
    {
        private const byte _idAvailableMachines = 0x01;
        private const byte _idSelectMachine = 0x02;
        private const byte _idCoreAction = 0x03;
        private const byte _idPing = 0x04;
        private const byte _idRequestAvailableMachines = 0x05;
        private const byte _idName = 0x06;
        private const byte _idCoreRequest = 0x07;

        private IConnection _connection;

        public Remote(IConnection connection)
        {
            _connection = connection;
            _connection.OnNewMessage += OnNewMessage;
        }

        public ReceiveCoreActionDelegate ReceiveCoreAction { get; set; }
        public ReceiveSelectMachineDelegate ReceiveSelectMachine{ get; set; }
        public ReceiveRequestAvailableMachinesDelegate ReceiveRequestAvailableMachines { get; set; }
        public ReceiveAvailableMachinesDelegate ReceiveAvailableMachines { get; set; }
        public ReceivePingDelegate ReceivePing { get; set; }
        public ReceiveNameDelegate ReceiveName { get; set; }
        public ReceiveCoreRequestDelegate ReceiveCoreRequest { get; set; }

        public void Close()
        {
            lock (_connection)
            {
                _connection.Close();
            }
        }

        public void Dispose()
        {
            Close();
        }

        public void SendCoreAction(CoreAction coreAction)
        {
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(_idCoreAction);
            Serializer.CoreActionToBytes(bs, coreAction);

            SendMessage(bs.AsBytes());
        }

        public void SendSelectMachine(string machineName)
        {
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(_idSelectMachine);
            Serializer.SelectMachineToBytes(bs, machineName);

            SendMessage(bs.AsBytes());
        }

        public void SendRequestAvailableMachines()
        {
            byte[] msg = { _idRequestAvailableMachines };

            SendMessage(msg);
        }

        public void SendAvailableMachines(IEnumerable<string> availableMachines)
        {
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(_idAvailableMachines);
            Serializer.AvailableMachinesToBytes(bs, availableMachines);

            SendMessage(bs.AsBytes());
        }

        public void SendPing(bool response, UInt64 id)
        {
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(_idPing);
            bs.Write(response);
            bs.Write(id);

            SendMessage(bs.AsBytes());
        }

        public void SendName(string name)
        {
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(_idName);
            bs.Write(name);

            SendMessage(bs.AsBytes());
        }

        public void SendCoreRequest(CoreRequest coreRequest)
        {
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(_idCoreRequest);
            Serializer.CoreRequestToBytes(bs, coreRequest);

            SendMessage(bs.AsBytes());
        }

        private void SendMessage(byte[] msg)
        {
            lock (_connection)
            {
                if (_connection.IsConnected)
                {
                    _connection.SendMessage(msg);
                }
            }
        }

        private void OnNewMessage(byte[] msg)
        {
            if (_connection == null)
            {
                return;
            }

            MemoryByteStream bs = new MemoryByteStream(msg);

            byte id = bs.ReadByte();

            switch (id)
            {
                case _idSelectMachine:
                    {
                        string machineName = Serializer.SelectMachineFromBytes(bs);
                        ReceiveSelectMachine?.Invoke(machineName);
                    }
                    break;
                case _idAvailableMachines:
                    {
                        List<string> machines = Serializer.AvailableMachinesFromBytes(bs);
                        ReceiveAvailableMachines?.Invoke(machines);
                    }
                    break;
                case _idCoreAction:
                    {
                        CoreAction coreAction = Serializer.CoreActionFromBytes(bs);
                        ReceiveCoreAction?.Invoke(coreAction);
                    }
                    break;
                case _idPing:
                    {
                        bool response = bs.ReadBool();
                        UInt64 pid = bs.ReadUInt64();
                        ReceivePing?.Invoke(response, pid);
                    }
                    break;
                case _idRequestAvailableMachines:
                    {
                        ReceiveRequestAvailableMachines?.Invoke();
                    }
                    break;
                case _idName:
                    {
                        string machineName = bs.ReadString();
                        ReceiveName?.Invoke(machineName);
                    }
                    break;
                case _idCoreRequest:
                    {
                        CoreRequest coreRequest = Serializer.CoreRequestFromBytes(bs);
                        ReceiveCoreRequest?.Invoke(coreRequest);
                    }
                    break;
            }
        }
    }
}
