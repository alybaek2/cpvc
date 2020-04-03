using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IRemoteReceiver
    {
        void ReceiveCoreAction(CoreAction coreAction);
        void ReceiveSelectMachine(string machineName);
        void ReceiveAvailableMachines(List<string> availableMachines);
    }
}
