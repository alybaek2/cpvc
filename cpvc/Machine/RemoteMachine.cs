using System;
using System.ComponentModel;
using System.Threading;

namespace CPvC
{
    public sealed class RemoteMachine : Machine,
        IMachine,
        IInteractiveMachine,
        INotifyPropertyChanged,
        IDisposable
    {
        private string _name;
        private IRemote _remote;

        /// <summary>
        /// The name of the machine.
        /// </summary>
        public override string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;

                OnPropertyChanged();
            }
        }

        public override WaitHandle CanProcessEvent
        {
            get
            {
                return _coreRequestsAvailable;
            }
        }

        public RemoteMachine(IRemote remote)
        {
            BlankScreen();

            _core.Create(Core.LatestVersion, Core.Type.CPC6128);

            Start();

            _remote = remote;
            _remote.ReceiveCoreAction = ReceiveCoreAction;
            _remote.ReceiveName = ReceiveName;
            _remote.CloseConnection = CloseConnection;
        }

        private void CloseConnection()
        {
            Status = "Connection closed";
        }

        public MachineRequest ReceiveCoreAction(IMachineAction coreAction)
        {
            if (_core == null)
            {
                Close();
                return null;
            }

            RaiseEvent(coreAction);

            MachineRequest request = (MachineRequest)coreAction;
            PushRequest(request);

            return request;
        }

        public void ReceiveName(string machineName)
        {
            Name = String.Format("{0} (remote)", machineName);
        }

        public void Dispose()
        {
            Close();
        }

        public override void Close()
        {
            base.Close();
            _remote.Dispose();
        }

        public bool CanClose
        {
            get
            {
                return true;
            }
        }

        public MachineRequest Key(byte keycode, bool down)
        {
            MachineRequest request = new KeyPressRequest(keycode, down);
            _remote.SendCoreRequest(request);

            return request;
        }

        public MachineRequest LoadDisc(byte drive, byte[] diskBuffer)
        {
            MachineRequest request = new LoadDiscRequest(drive, MemoryBlob.Create(diskBuffer));
            _remote.SendCoreRequest(request);

            return request;
        }

        public MachineRequest LoadTape(byte[] tapeBuffer)
        {
            MachineRequest request = new LoadTapeRequest(MemoryBlob.Create(tapeBuffer));
            _remote.SendCoreRequest(request);
            return request;
        }

        public MachineRequest Reset()
        {
            MachineRequest request = new ResetRequest();
            _remote.SendCoreRequest(request);

            return request;
        }

        protected override void CoreActionDone(MachineRequest request, IMachineAction action)
        {
            if (action == null)
            {
                return;
            }

            if (action is RevertToSnapshotAction)
            {
                // Ensure to update the display.
                BeginVSync();
            }
        }
    }
}
