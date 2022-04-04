using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CPvC
{
    public enum RunningState
    {
        Paused,
        Running,
        Reverse
    }

    public abstract class Machine
    {
        protected Core _core;
        private string _status;

        protected RunningState _runningState;
        protected RunningState _requestedState;
        protected bool _runningDirection;

        protected int _autoPauseCount;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Value indicating the relative volume level of rendered audio (0 = mute, 255 = loudest).
        /// </summary>
        private byte _volume;

        public Machine()
        {
            _autoPauseCount = 0;
            _runningState = RunningState.Paused;
            _requestedState = RunningState.Paused;
            _runningDirection = true; // Forward!
            _volume = 80;

            _core = new Core(Core.LatestVersion, Core.Type.CPC6128);
            _core.OnBeginVSync += (sender, args) =>
            {
                BeginVSync(sender as Core);
            };

            Display = new Display();
            Display.Core = _core;

            _core.PropertyChanged += CorePropertyChanged;
        }

        public Core Core
        {
            get
            {
                return _core;
            }
        }

        public abstract string Name { get; set; }

        public UInt64 Ticks
        {
            get
            {
                return Core?.Ticks ?? 0;
            }
        }

        public RunningState RunningState
        {
            get
            {
                if (_autoPauseCount >= 1)
                {
                    return RunningState.Paused;
                }
                else
                {
                    return _runningState;
                }
            }
        }

        public byte Volume
        {
            get
            {
                return _volume;
            }

            set
            {
                if (_volume == value)
                {
                    return;
                }

                _volume = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enables or disables turbo mode.
        /// </summary>
        /// <param name="enabled">Indicates whether turbo mode is to be enabled.</param>
        public void EnableTurbo(bool enabled)
        {
            Core.AudioBuffer.ReadSpeed = (byte)(enabled ? 10 : 1);

            Status = enabled ? "Turbo enabled" : "Turbo disabled";
        }

        public string Status
        {
            get
            {
                return _status;
            }

            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        // This really should be an event, so multiple subscribers can be supported. Or is this already supprted? Test this!
        public MachineAuditorDelegate Auditors { get; set; }

        public Display Display { get; protected set; }

        /// <summary>
        /// Delegate for VSync events.
        /// </summary>
        /// <param name="core">Core whose VSync signal went from low to high.</param>
        protected virtual void BeginVSync(Core core)
        {
            Display.CopyScreenAsync();
        }

        public virtual int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            // Ensure that while we're reading audio, the running state of the machine can't be changed.
            if (_core?.AudioBuffer == null || !_core.IsOpen)
            {
                return 0;
            }

            // We need to make sure that our overrun threshold is enough so that we can fully satisfy at
            // least the next callback from NAudio. Without this, CPvC playback can become "stuttery."
            _core.AudioBuffer.OverrunThreshold = samplesRequested * 2;

            return _core.AudioBuffer.Render16BitStereo(Volume, buffer, offset, samplesRequested, false);
        }

        public void AdvancePlayback(int samples)
        {
            Core?.AdvancePlayback(samples);
        }

        /// <summary>
        /// Sets the core to the appropriate running state, given the <c>_running</c> and <c>_autoPauseCount</c> members.
        /// </summary>
        protected void SetCoreRunning(bool request)
        {
            if (_core == null)
            {
                return;
            }

            if (_autoPauseCount > 0 || _requestedState == RunningState.Paused)
            {
                if (request)
                {
                    _core.RequestStop();
                }
                else
                {
                    _core.Stop();
                }
            }
            else
            {
                _core.Start();
            }

            OnPropertyChanged("RunningState");
        }

        public void Start()
        {
            SetRequestedState(RunningState.Running);
            Status = "Resumed";
        }

        public void RequestStop()
        {
            _requestedState = RunningState.Paused;
            SetCoreRunning(true);
        }

        public void Stop()
        {
            SetRequestedState(RunningState.Paused);

            Status = "Paused";
        }

        public void ToggleRunning()
        {
            if (_requestedState == RunningState.Running)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        /// <summary>
        /// Helper class that pauses the machine on creation and resumes the machine when the object is disposed.
        /// </summary>
        private class AutoPauser : IDisposable
        {
            private readonly Machine _machine;

            public AutoPauser(Machine machine)
            {
                _machine = machine;
                _machine.IncrementAutoPause();
            }

            public void Dispose()
            {
                _machine.DecrementAutoPause();
            }
        }

        /// <summary>
        /// Pauses the machine and returns an IDisposable which, when disposed of, causes the machine to resume (if it was running before).
        /// </summary>
        /// <returns>A IDisposable interface.</returns>
        public IDisposable AutoPause()
        {
            return new AutoPauser(this);
        }

        private void IncrementAutoPause()
        {
            Interlocked.Increment(ref _autoPauseCount);
            SetCoreRunning(false);
        }

        private void DecrementAutoPause()
        {
            Interlocked.Decrement(ref _autoPauseCount);
            SetCoreRunning(false);
        }

        public void SetRequestedState(RunningState runningState)
        {
            _requestedState = runningState;
            SetCoreRunning(false);
        }

        protected void UpdateRunningState()
        {
            if (!_core.Running)
            {
                _runningState = RunningState.Paused;
            }
            else
            {
                _runningState = _runningDirection ? RunningState.Running : RunningState.Reverse;
            }

            OnPropertyChanged("RunningState");

        }

        protected void CorePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Core.Running))
            {
                UpdateRunningState();
            }

            OnPropertyChanged(e.PropertyName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
