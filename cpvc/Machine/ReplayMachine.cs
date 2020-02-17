using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public sealed class ReplayMachine : CoreMachine, IPausableMachine, ITurboableMachine, IPrerecordedMachine, IClosableMachine, INotifyPropertyChanged, IDisposable
    {
        private UInt64 _endTicks;

        public ReplayMachine(HistoryEvent historyEvent)
        {
            Name = "Replay!!!";
            Display = new Display();

            _endTicks = historyEvent.Ticks;

            List<CoreRequest> actions = new List<CoreRequest>();
            actions.Add(CoreRequest.RunUntilForce(_endTicks));
            actions.Add(CoreRequest.Quit());

            while (historyEvent != null)
            {
                if (historyEvent.CoreAction != null)
                {
                    actions.Insert(0, historyEvent.CoreAction);
                    actions.Insert(0, CoreRequest.RunUntilForce(historyEvent.Ticks));
                }

                historyEvent = historyEvent.Parent;
            }

            _core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            _core.BeginVSync += BeginVSync;
            _core.PropertyChanged += CorePropertyChanged;

            foreach (CoreRequest action in actions)
            {
                _core.PushRequest(action);
            }

            _core.SetScreenBuffer(Display.Buffer);
        }

        public string Name
        {
            get; set;
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        public void Close()
        {
            _core?.Dispose();
        }

        public void EnableTurbo(bool enabled)
        {
            _core.EnableTurbo(enabled);
        }

        public void Start()
        {
            if (_core.Ticks < _endTicks)
            {
                _core.Start();
            }
        }

        public void Stop()
        {
            _core.Stop();
        }

        public void ToggleRunning()
        {
            if (_core.Running)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        public void SeekToStart()
        {

        }

        public void SeekToEnd()
        {

        }

        public void SeekToPreviousBookmark()
        {

        }

        public void SeekToNextBookmark()
        {

        }

        private void CorePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Ticks")
            {
                OnPropertyChanged("Ticks");
            }
            else if (e.PropertyName == "Running")
            {
                OnPropertyChanged("Running");
            }
            else if (e.PropertyName == "Volume")
            {
                OnPropertyChanged("Volume");
            }
        }
    }
}
