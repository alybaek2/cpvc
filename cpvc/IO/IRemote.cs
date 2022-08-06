using System;
using System.Collections.Generic;

namespace CPvC
{
    public interface IRemote : IDisposable
    {
        ReceiveCoreActionDelegate ReceiveCoreAction { get; set; }
        ReceiveSelectMachineDelegate ReceiveSelectMachine { get; set; }
        ReceiveRequestAvailableMachinesDelegate ReceiveRequestAvailableMachines { get; set; }
        ReceiveAvailableMachinesDelegate ReceiveAvailableMachines { get; set; }
        ReceivePingDelegate ReceivePing { get; set; }
        ReceiveNameDelegate ReceiveName { get; set; }
        ReceiveCoreRequestDelegate ReceiveCoreRequest { get; set; }

        CloseConnectionDelegate CloseConnection { get; set; }

        void SendCoreAction(MachineAction coreAction);
        void SendSelectMachine(string machineName);
        void SendRequestAvailableMachines();
        void SendAvailableMachines(IEnumerable<string> availableMachines);
        void SendPing(bool response, UInt64 id);
        void SendName(string name);
        void SendCoreRequest(MachineRequest coreRequest);
    }
}
