using System;
using System.ComponentModel;

namespace CPvC
{
    public abstract class CoreMachine
    {
        protected Core _core;
        protected string _filepath;
        private string _status;
        protected bool _running;
        protected int _autoPauseCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public CoreMachine()
        {
            _autoPauseCount = 0;
            _running = false;
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
                    value.SetScreen(Display.Buffer);

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
                OnPropertyChanged("Running");
                OnPropertyChanged("Volume");
            }
        }

        public string Filepath
        {
            get
            {
                return _filepath;
            }

            protected set
            {
                _filepath = value;
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

        public bool Running
        {
            get
            {
                return Core?.Running ?? false;
            }
        }

        public byte Volume
        {
            get
            {
                return Core?.Volume ?? 0;
            }

            set
            {
                Core.Volume = value;
            }
        }

        public void EnableTurbo(bool enabled)
        {
            _core.EnableTurbo(enabled);

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
                OnPropertyChanged("Status");
            }
        }

        // This really should be an event, so multiple subscribers can be supported. Or is this already supprted? Test this!
        public MachineAuditorDelegate Auditors { get; set; }

        public Display Display { get; protected set; }

        /// <summary>
        /// Delegate for VSync events.
        /// </summary>
        /// <param name="core">Core whose VSync signal went form low to high.</param>
        protected void BeginVSync(Core core)
        {
            Display.CopyFromBufferAsync();
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            return Core?.ReadAudio16BitStereo(buffer, offset, samplesRequested) ?? 0;
        }

        public void AdvancePlayback(int samples)
        {
            Core?.AdvancePlayback(samples);
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

            bool shouldRun = (_running && _autoPauseCount == 0);
            if (shouldRun && !_core.Running)
            {
                _core.Start();
            }
            else if (!shouldRun && _core.Running)
            {
                _core.Stop();
            }
        }

        public void Start()
        {
            _running = true;
            SetCoreRunning();
            Status = "Resumed";
        }

        public void Stop()
        {
            _running = false;
            SetCoreRunning();
            Status = "Paused";
        }

        public void ToggleRunning()
        {
            if (_running)
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
            private readonly CoreMachine _machine;

            public AutoPauser(CoreMachine machine)
            {
                _machine = machine;
                _machine._autoPauseCount++;
                _machine.SetCoreRunning();
            }

            public void Dispose()
            {
                _machine._autoPauseCount--;
                _machine.SetCoreRunning();
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

        protected void CorePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e.PropertyName);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
