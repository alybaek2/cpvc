using System;
using System.ComponentModel;

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

        public RemoteMachine(IRemote remote)
        {
            Display.GetFromBookmark(null);

            _core.Create(Core.LatestVersion, Core.Type.CPC6128);
            _core.OnCoreAction += HandleCoreAction;

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

        public void Close()
        {
            _remote.Dispose();
            if (_core != null)
            {
                _core.Stop();
                _core.OnCoreAction -= HandleCoreAction;

                _core = null;
            }
        }

        public bool CanClose
        {
            get
            {
                return true;
            }
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

        private void HandleCoreAction(object sender, CoreEventArgs args)
        {
            if (!ReferenceEquals(_core, sender) || args.Action == null)
            {
                return;
            }

            if (args.Action.Type == CoreAction.Types.RevertToSnapshot)
            {
                // Ensure to update the display.
                Display.CopyScreenAsync();
            }
        }
    }
}
