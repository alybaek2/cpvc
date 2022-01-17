using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
        protected object _runningStateLock;

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
            _runningStateLock = new object();
            _volume = 80;
        }

        public Core Core
        {
            get
            {
                return _core;
            }

            set
            {
                if (_core == value)
                {
                    return;
                }

                if (_core != null)
                {
                    _core.Dispose();
                }

                if (value != null)
                {
                    value.SetScreen();
                    if (Display != null)
                    {
                        Display.Core = value;
                    }

                    if (_core != null)
                    {
                        value.Auditors += _core.Auditors;
                    }

                    value.BeginVSync += BeginVSync;

                    value.PropertyChanged += CorePropertyChanged;
                }

                _core = value;

                OnPropertyChanged("Core");
                OnPropertyChanged("Ticks");
                OnPropertyChanged("RunningState");
                OnPropertyChanged("Volume");
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
            lock (_runningStateLock)
            {
                if (_core?.AudioBuffer == null)
                {
                    return 0;
                }

                // We need to make sure that our overrun threshold is enough so that we can fully satisfy at
                // least the next callback from NAudio. Without this, CPvC playback can become "stuttery."
                _core.AudioBuffer.OverrunThreshold = samplesRequested * 2;

                return _core.AudioBuffer.Render16BitStereo(Volume, buffer, offset, samplesRequested, false);
            }
        }

        public void AdvancePlayback(int samples)
        {
            lock (_runningStateLock)
            {
                Core?.AdvancePlayback(samples);
            }
        }

        /// <summary>
        /// Sets the core to the appropriate running state, given the <c>_running</c> and <c>_autoPauseCount</c> members.
        /// </summary>
        protected void SetCoreRunning()
        {
            if (_core == null)
            {
                return;
            }

            if (_autoPauseCount > 0)
            {
                _core.Stop();
            }
            else
            {
                if (_runningState != RunningState.Paused)
                {
                    _core.Start();
                }
                else
                {
                    _core.Stop();
                }
            }

            OnPropertyChanged("RunningState");
        }

        public void Start()
        {
            SetRunningState(RunningState.Running);
            Status = "Resumed";
        }

        public void Stop()
        {
            SetRunningState(RunningState.Paused);
            Status = "Paused";
        }

        public void ToggleRunning()
        {
            if (_runningState == RunningState.Running)
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
            lock (_runningStateLock)
            {
                _autoPauseCount++;
                SetCoreRunning();
            }
        }

        private void DecrementAutoPause()
        {
            lock (_runningStateLock)
            {
                _autoPauseCount--;
                SetCoreRunning();
            }
        }

        public RunningState SetRunningState(RunningState runningState)
        {
            lock (_runningStateLock)
            {
                RunningState previousRunningState = _runningState;
                _runningState = runningState;
                SetCoreRunning();

                OnPropertyChanged("RunningState");

                return previousRunningState;
            }
        }

        protected void CorePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
