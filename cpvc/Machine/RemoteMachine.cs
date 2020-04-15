using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public sealed class RemoteMachine : CoreMachine,
        IClosableMachine,
        INotifyPropertyChanged,
        IDisposable
    {
        private string _name;
        private Remote _remote;
        private int _lastPing;
        private int _connectionLatency;

        /// <summary>
        /// The name of the machine.
        /// </summary>
        public string Name
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

        public override string GetName()
        {
            return _name;
        }

        public OnCloseDelegate OnClose { get; set; }

        public RemoteMachine(Remote remote)
        {
            Display = new Display();
            Display.GetFromBookmark(null);

            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            core.KeepRunning = false;
            Core = core;
            core.Start();

            _remote = remote;
            _remote.ReceiveCoreAction = ReceiveCoreAction;
            _remote.ReceivePing = ReceivePing;
            _remote.ReceiveName = ReceiveName;

            _lastPing = 0;
            _connectionLatency = 0;
        }

        public void ReceiveCoreAction(CoreAction coreAction)
        {
            if (_core == null)
            {
                Close();
                return;
            }

            _core.PushRequest(coreAction);

            int ticks = System.Environment.TickCount;

            if ((ticks - _lastPing) > 100)
            {
                _remote.SendPing(false, (UInt64)ticks);

                _lastPing = ticks;
            }
        }

        public void ReceiveName(string machineName)
        {
            Name = String.Format("{0} (remote)", machineName);
        }

        public void ReceivePing(bool response, UInt64 id)
        {
            if (response)
            {
                int ticks = System.Environment.TickCount;

                int pingTicks = (int)id;

                _connectionLatency = ticks - pingTicks;
            }
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        public bool CanClose()
        {
            return true;
        }

        public void Close()
        {
            _remote.Close();
            _core?.Stop();
            Core = null;

            OnClose?.Invoke();
        }
    }
}
