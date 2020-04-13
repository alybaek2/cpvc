﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineServerConnection
    {
        private Machine _machine;
        private IEnumerable<Machine> _machines;

        private Remote _remote;

        public MachineServerConnection(SocketConnection socket, IEnumerable<Machine> machines)
        {
            _remote = new Remote(socket);
            _remote.ReceiveSelectMachine = ReceiveSelectMachine;
            _remote.ReceiveRequestAvailableMachines = ReceiveRequestAvailableMachines;
            _remote.ReceivePing = ReceivePing;

            _machines = machines;
        }

        private void ReceiveRequestAvailableMachines()
        {
            _remote.SendAvailableMachines(_machines.Select(m => m.Name));
        }

        private void ReceiveSelectMachine(string machineName)
        {
            Machine machine = _machines.Where(m => m.Name == machineName).FirstOrDefault();
            if (machine == _machine)
            {
                return;
            }

            if (_machine != null)
            {
                // Quit sending to the client.
                _machine.Auditors -= MachineAuditor;
                _machine = null;
            }

            _machine = machine;
            if (_machine != null)
            {
                using (_machine.AutoPause())
                {
                    byte[] state = _machine.Core.GetState();

                    CoreAction loadCoreAction = CoreAction.LoadCore(0, new MemoryBlob(state));

                    _remote.SendName(_machine.Name);
                    _remote.SendCoreAction(loadCoreAction);

                    _machine.Auditors += MachineAuditor;
                }
            }
        }

        private void ReceivePing(bool response, UInt64 id)
        {
            if (!response)
            {
                _remote.SendPing(true, id);
            }
        }

        private void MachineAuditor(CoreAction coreAction)
        {
            _remote.SendCoreAction(coreAction);
        }
    }
}
