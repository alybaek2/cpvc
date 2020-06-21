using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IRemote
    {
        ReceiveCoreActionDelegate ReceiveCoreAction { get; set; }
        ReceiveSelectMachineDelegate ReceiveSelectMachine { get; set; }
        ReceiveRequestAvailableMachinesDelegate ReceiveRequestAvailableMachines { get; set; }
        ReceiveAvailableMachinesDelegate ReceiveAvailableMachines { get; set; }
        ReceivePingDelegate ReceivePing { get; set; }
        ReceiveNameDelegate ReceiveName { get; set; }
        ReceiveCoreRequestDelegate ReceiveCoreRequest { get; set; }

        CloseConnectionDelegate CloseConnection { get; set; }

        void SendCoreAction(CoreAction coreAction);
        void SendSelectMachine(string machineName);
        void SendRequestAvailableMachines();
        void SendAvailableMachines(IEnumerable<string> availableMachines);
        void SendPing(bool response, UInt64 id);
        void SendName(string name);
        void SendCoreRequest(CoreRequest coreRequest);
    }
}
