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
        IInteractiveMachine,
        INotifyPropertyChanged,
        IDisposable
    {
        private string _name;
        private Remote _remote;
        private int _lastPing;
        private int _connectionLatency;
        private UInt64 _emulationLatency;

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
            _emulationLatency = 0;
        }

        public void ReceiveCoreAction(CoreAction coreAction)
        {
            if (_core == null)
            {
                Close();
                return;
            }

            _emulationLatency = coreAction.Ticks - Ticks;
            Status = String.Format("Emulation latency: {0} ms", _emulationLatency / 4000);
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

                //Status = String.Format("Latency: {0} ms", _connectionLatency);
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
    }
}
