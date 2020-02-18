using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class CoreMachine : ICoreMachine
    {
        protected Core _core;
        protected string _filepath;

        public event PropertyChangedEventHandler PropertyChanged;

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
                    value.SetScreenBuffer(Display.Buffer);

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

        protected void CorePropertyChanged(object sender, PropertyChangedEventArgs e)
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

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
