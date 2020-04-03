using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineServerConnection : IRemoteReceiver
    {
        private Machine _machine;
        private IEnumerable<Machine> _machines;

        private Remote _remote;

        public MachineServerConnection(SocketConnection socket, IEnumerable<Machine> machines)
        {
            _remote = new Remote(socket, this);
            _machines = machines;
        }

        public void ReceiveCoreAction(CoreAction coreAction)
        {

        }

        public void ReceiveSelectMachine(string machineName)
        {
            Machine machine = _machines.Where(m => m.Name == machineName).FirstOrDefault();
            if (machine != null)
            {
                _machine = machine;

                using (_machine.AutoPause())
                {
                    byte[] state = _machine.Core.GetState();

                    CoreAction loadCoreAction = CoreAction.LoadCore(0, new MemoryBlob(state));

                    _remote.SendCoreAction(loadCoreAction);

                    _machine.Auditors += MachineAuditor;
                }
            }
        }

        public void ReceiveAvailableMachines(List<string> availableMachines)
        {

        }

        private void MachineAuditor(CoreAction coreAction)
        {
            _remote.SendCoreAction(coreAction);
        }

        public void StartHandshake()
        {
            _remote.SendAvailableMachines(_machines.Select(m => m.Name));
        }
    }
}
