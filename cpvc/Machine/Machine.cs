using System;
using System.Collections.Generic;
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
        private AutoResetEvent _runningStateChanged;

        protected int _autoPauseCount;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Queue<CoreRequest> _requests;

        private AudioBuffer _audioBuffer;

        private bool _quitThread;
        protected Thread _machineThread;

        /// <summary>
        /// Value indicating the relative volume level of rendered audio (0 = mute, 255 = loudest).
        /// </summary>
        private byte _volume;

        public Machine()
        {
            _autoPauseCount = 0;
            _runningState = RunningState.Paused;
            _requestedState = RunningState.Paused;
            _volume = 80;

            _core = new Core(Core.LatestVersion, Core.Type.CPC6128);

            Display = new Display();
            Display.Core = _core;

            _requests = new Queue<CoreRequest>();

            _runningStateChanged = new AutoResetEvent(false);

            _audioBuffer = new AudioBuffer(48000);

            _machineThread = new Thread(MachineThread);
            _machineThread.Start();
        }
        public abstract string Name { get; set; }

        public UInt64 Ticks
        {
            get
            {
                return _core?.Ticks ?? 0;
            }
        }

        public AudioBuffer AudioBuffer
        {
            get
            {
                return _audioBuffer;
            }
        }

        public RunningState RunningState
        {
            get
            {
                return _runningState;
            }

            private set
            {
                if (_runningState == value)
                {
                    return;
                }

                _runningState = value;
                _runningStateChanged.Set();

                OnPropertyChanged();
            }
        }

        public RunningState RequestedState
        {
            get
            {
                if (_autoPauseCount > 0 || _requestedState == RunningState.Paused)
                {
                    return RunningState.Paused;
                }

                return _requestedState;
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
            _audioBuffer.ReadSpeed = (byte)(enabled ? 10 : 1);

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
        //protected virtual void BeginVSync(Core core)
        //{
        //    Display.CopyScreenAsync();

        //    OnPropertyChanged(nameof(Ticks));
        //}


        public virtual int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            if (_audioBuffer == null)
            {
                return 0;
            }

            // We need to make sure that our overrun threshold is enough so that we can fully satisfy at
            // least the next callback from NAudio. Without this, CPvC playback can become "stuttery."
            _audioBuffer.OverrunThreshold = samplesRequested * 2;

            return _audioBuffer.Render16BitStereo(Volume, buffer, offset, samplesRequested, false);
        }

        public void AdvancePlayback(int samples)
        {
            _audioBuffer.Advance(samples);
        }

        /// <summary>
        /// Sets the core to the appropriate running state, given the <c>_running</c> and <c>_autoPauseCount</c> members.
        /// </summary>
        //protected void SetCoreRunning(bool request)
        //{
        //    if (_core == null)
        //    {
        //        return;
        //    }

        //    if (_autoPauseCount > 0 || _requestedState == RunningState.Paused)
        //    {
        //        if (request)
        //        {
        //            _core.RequestStop();
        //        }
        //        else
        //        {
        //            _core.Stop();
        //        }
        //    }
        //    else
        //    {
        //        _core.Start();
        //    }

        //    OnPropertyChanged("RunningState");
        //}

        public void Start()
        {
            SetRequestedState(RunningState.Running);
            Status = "Resumed";
        }

        public void RequestStop()
        {
            _requestedState = RunningState.Paused;
            //SetCoreRunning(true);
            Stop();
        }

        public void Stop()
        {
            SetRequestedState(RunningState.Paused);

            Status = "Paused";
        }

        public void RequestStopAndWait()
        {
            Stop();

            WaitForRequestedToMatchRunning();
        }

        public void WaitForRequestedToMatchRunning()
        {
            while (RequestedState != RunningState)
            {
                _runningStateChanged.WaitOne(20);
            }
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

            _runningStateChanged.Set();
            OnPropertyChanged(nameof(RunningState));

            WaitForRequestedToMatchRunning();
        }

        private void DecrementAutoPause()
        {
            Interlocked.Decrement(ref _autoPauseCount);

            _runningStateChanged.Set();
            OnPropertyChanged(nameof(RunningState));

            WaitForRequestedToMatchRunning();
        }

        public void SetRequestedState(RunningState runningState)
        {
            _requestedState = runningState;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void PushRequest(CoreRequest request)
        {
            lock (_requests)
            {
                _requests.Enqueue(request);
            }
        }

        private (bool, CoreAction) ProcessRequest(CoreRequest request)
        {
            bool success = true;

            CoreAction action = null;

            UInt64 ticks = Ticks;
            switch (request.Type)
            {
                case CoreRequest.Types.KeyPress:
                    if (_core.KeyPressSync(request.KeyCode, request.KeyDown))
                    {
                        action = CoreAction.KeyPress(ticks, request.KeyCode, request.KeyDown);
                    }
                    break;
                case CoreRequest.Types.Reset:
                    _core.ResetSync();
                    action = CoreAction.Reset(ticks);
                    break;
                case CoreRequest.Types.LoadDisc:
                    _core.LoadDiscSync(request.Drive, request.MediaBuffer.GetBytes());
                    action = CoreAction.LoadDisc(ticks, request.Drive, request.MediaBuffer);
                    break;
                case CoreRequest.Types.LoadTape:
                    _core.LoadTapeSync(request.MediaBuffer.GetBytes());
                    action = CoreAction.LoadTape(ticks, request.MediaBuffer);
                    break;
                case CoreRequest.Types.RunUntil:
                    action = RunForAWhile(request.StopTicks);

                    success = request.StopTicks <= Ticks;

                    break;
                case CoreRequest.Types.CoreVersion:
                    _core.ProcessCoreVersion(request.Version);

                    action = CoreAction.CoreVersion(Ticks, request.Version);

                    break;
                case CoreRequest.Types.LoadCore:
                    _core.LoadState(request.CoreState.GetBytes());
                    action = CoreAction.LoadCore(ticks, request.CoreState);
                    break;
                case CoreRequest.Types.CreateSnapshot:
                    _core.CreateSnapshotSync(request.SnapshotId);
                    action = CoreAction.CreateSnapshot(Ticks, request.SnapshotId);

                    break;
                case CoreRequest.Types.DeleteSnapshot:
                    if (_core.DeleteSnapshotSync(request.SnapshotId))
                    {
                        action = CoreAction.DeleteSnapshot(Ticks, request.SnapshotId);
                    }

                    break;
                case CoreRequest.Types.RevertToSnapshot:
                    if (_core.RevertToSnapshotSync(request.SnapshotId))
                    {
                        action = CoreAction.RevertToSnapshot(Ticks, request.SnapshotId);

                        Display.CopyScreenAsync();

                        OnPropertyChanged("Ticks");
                    }

                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown core request type {0}. Ignoring request.", request.Type));
            }

            return (success, action);
        }

        public CoreRequest RunUntil(UInt64 ticks)
        {
            CoreRequest request = CoreRequest.RunUntil(ticks);
            PushRequest(request);

            return request;
        }

        public void KeyPress(byte keycode, bool down)
        {
            PushRequest(CoreRequest.KeyPress(keycode, down));
        }

        public void AllKeysUp()
        {
            for (byte keycode = 0; keycode < 80; keycode++)
            {
                KeyPress(keycode, false);
            }
        }

        private CoreAction RunForAWhile(UInt64 stopTicks)
        {
            UInt64 ticks = Ticks;

            // Only proceed on an audio buffer underrun.
            if (!_audioBuffer.WaitForUnderrun(20))
            {
                return null;
            }

            // Limit how long we can run for to reduce audio lag.
            stopTicks = Math.Min(stopTicks, Ticks + 1000);

            List<UInt16> audioSamples = new List<UInt16>();
            byte stopReason = _core.RunUntil(stopTicks, StopReasons.VSync, audioSamples);

            foreach (UInt16 sample in audioSamples)
            {
                _audioBuffer.Write(sample);
            }

            if ((stopReason & StopReasons.VSync) != 0)
            {
                OnPropertyChanged("Ticks");
                BeginVSync();
            }

            return CoreAction.RunUntil(ticks, Ticks, audioSamples);
        }

        protected virtual void BeginVSync()
        {
            Display.CopyScreenAsync();
        }

        protected virtual CoreRequest GetNextRequest(int timeout)
        {
            CoreRequest request = null;

            lock (_requests)
            {
                if (_requests.Count > 0)
                {
                    request = _requests.Peek();
                }
            }

            return request;
        }

        public void MachineThread()
        {
            CoreRequest request = null;

            while (!_quitThread)
            {
                // Are we running?
                if (_autoPauseCount > 0 || _requestedState == RunningState.Paused)
                {
                    RunningState = RunningState.Paused;

                    _runningStateChanged.WaitOne(20);
                    continue;
                }

                RunningState = _requestedState;

                if (request == null)
                {
                    request = GetNextRequest(20);
                    if (request == null)
                    {
                        continue;
                    }
                }

                (bool done, CoreAction action) = ProcessRequest(request);

                CoreActionDone(request, action);

                if (done)
                {
                    request.Processed = true;

                    lock (_requests)
                    {
                        if (_requests.Count > 0 && _requests.Peek() == request)
                        {
                            _requests.Dequeue();
                        }
                    }

                    request = null;
                }
            }

            _quitThread = false;
        }

        protected virtual void CoreActionDone(CoreRequest request, CoreAction action)
        {

        }

        public virtual void Close()
        {
            _quitThread = true;

            _machineThread?.Join();
            _machineThread = null;

            _core?.Dispose();
            _core = null;
        }
    }
}
