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
                return _requestsAvailable;
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

        public void ReceiveCoreAction(CoreAction coreAction)
        {
            if (_core == null)
            {
                Close();
                return;
            }

            Auditors?.Invoke(coreAction);

            PushRequest(coreAction);
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

        public CoreRequest Key(byte keycode, bool down)
        {
            CoreRequest request = CoreRequest.KeyPress(keycode, down);
            _remote.SendCoreRequest(request);

            return request;
        }

        public CoreRequest LoadDisc(byte drive, byte[] diskBuffer)
        {
            CoreRequest request = CoreRequest.LoadDisc(drive, diskBuffer);
            _remote.SendCoreRequest(request);

            return request;
        }

        public CoreRequest LoadTape(byte[] tapeBuffer)
        {
            CoreRequest request = CoreRequest.LoadTape(tapeBuffer);
            _remote.SendCoreRequest(request);
            return request;
        }

        public CoreRequest Reset()
        {
            CoreRequest request = CoreRequest.Reset();
            _remote.SendCoreRequest(request);

            return request;
        }

        protected override void CoreActionDone(CoreRequest request, CoreAction action)
        {
            if (action.Type == CoreAction.Types.RevertToSnapshot)
            {
                // Ensure to update the display.
                BeginVSync();
            }
        }
        public byte[] GetState()
        {
            return _core.GetState();
        }
    }
}
