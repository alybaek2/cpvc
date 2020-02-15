using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ReplayMachine : IPausableMachine, ITurboableMachine, IPrerecordedMachine, IBaseMachine
    {
        private Core _core;
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

            foreach (CoreRequest action in actions)
            {
                _core.PushRequest(action);
            }

            _core.SetScreenBuffer(Display.Buffer);
        }

        public Core Core
        {
            get
            {
                return _core;
            }
        }

        public string Filepath
        {
            get;
        }

        public string Name
        {
            get; private set;
        }

        public Display Display { get; private set; }


        private void BeginVSync(Core core)
        {
            // Only copy to the display if the VSync is from a core we're interesting in.
            if (core != null && _core == core)
            {
                Display.CopyFromBufferAsync();
            }
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            return _core?.ReadAudio16BitStereo(buffer, offset, samplesRequested) ?? 0;
        }

        public void AdvancePlayback(int samples)
        {
            _core?.AdvancePlayback(samples);
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

        public void Close()
        {
            _core?.Dispose();
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
    }
}
