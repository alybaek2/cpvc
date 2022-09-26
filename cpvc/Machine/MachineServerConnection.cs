using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC
{
    public class MachineServerConnection
    {
        private IMachine _machine;
        private IEnumerable<IMachine> _machines;

        private IRemote _remote;

        public MachineServerConnection(IRemote remote, IEnumerable<IMachine> machines)
        {
            _remote = remote;
            _remote.ReceiveSelectMachine = ReceiveSelectMachine;
            _remote.ReceiveRequestAvailableMachines = ReceiveRequestAvailableMachines;
            _remote.ReceivePing = ReceivePing;
            _remote.ReceiveCoreRequest = ReceiveCoreRequest;

            _machines = machines;
        }

        private void ReceiveRequestAvailableMachines()
        {
            _remote.SendAvailableMachines(_machines.Select(m => m.Name));
        }

        private void ReceiveSelectMachine(string machineName)
        {
            IMachine machine = _machines.Where(m => m.Name == machineName).FirstOrDefault();
            if (machine == _machine)
            {
                return;
            }

            if (_machine != null)
            {
                // Quit sending to the client.
                _machine.Event -= OnMachineEvent;
                _machine = null;
            }

            _machine = machine;
            if (_machine != null)
            {
                using (machine.Lock())
                {
                    byte[] state = _machine.GetState();

                    IMachineAction loadCoreAction = new LoadCoreAction(0, MemoryBlob.Create(state), null);

                    _remote.SendName(_machine.Name);
                    _remote.SendCoreAction(loadCoreAction);

                    _machine.Event += OnMachineEvent;
                }
            }
        }

        private void OnMachineEvent(object sender, MachineEventArgs args)
        {
            MachineAuditor(args.Action);
        }

        private void ReceivePing(bool response, UInt64 id)
        {
            if (!response)
            {
                _remote.SendPing(true, id);
            }
        }

        private void ReceiveCoreRequest(MachineRequest request)
        {
            _machine.PushRequest(request);
        }

        private void MachineAuditor(IMachineAction coreAction)
        {
            _remote.SendCoreAction(coreAction);
        }
    }
}
