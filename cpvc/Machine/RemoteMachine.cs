using System;
using System.ComponentModel;

namespace CPvC
{
    public sealed class RemoteMachine : CoreMachine,
        ICoreMachine,
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

                OnPropertyChanged("Name");
            }
        }

        public RemoteMachine(IRemote remote)
        {
            Display = new Display();
            Display.GetFromBookmark(null);

            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            Core = core;
            core.Auditors += RequestProcessed;

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

            _core.PushRequest(coreAction);
        }

        public void ReceiveName(string machineName)
        {
            Name = String.Format("{0} (remote)", machineName);
        }

        public void Dispose()
        {
            Close();
        }

        public bool CanClose()
        {
            return true;
        }

        public void Close()
        {
            _remote.Dispose();
            _core?.Stop();
            Core = null;
        }

        public void Key(byte keycode, bool down)
        {
            _remote.SendCoreRequest(CoreRequest.KeyPress(keycode, down));
        }

        public void LoadDisc(byte drive, byte[] diskBuffer)
        {
            _remote.SendCoreRequest(CoreRequest.LoadDisc(drive, diskBuffer));
        }

        public void LoadTape(byte[] tapeBuffer)
        {
            _remote.SendCoreRequest(CoreRequest.LoadTape(tapeBuffer));
        }

        public void Reset()
        {
            _remote.SendCoreRequest(CoreRequest.Reset());
        }

        private void RequestProcessed(Core core, CoreRequest request, CoreAction action)
        {
            if (core == _core && action != null)
            {
                if (action.Type == CoreAction.Types.RevertToSnapshot)
                {
                    // Ensure to update the display.
                    Display.CopyScreenAsync();
                }
            }
        }
    }
}
