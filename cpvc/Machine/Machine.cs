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
        protected RunningState _requestedRunningState;
        protected RunningState _actualRunningState;
        
        protected int _lockCount;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler DisplayUpdated;

        protected readonly Queue<MachineRequest> _requests;
        protected ManualResetEvent _requestsAvailable;
        protected readonly Queue<MachineRequest> _coreRequests;
        protected ManualResetEvent _coreRequestsAvailable;

        private AudioBuffer _audioBuffer;

        protected Thread _machineThread;

        protected Dictionary<int, CoreSnapshot> _allCoreSnapshots;
        protected List<CoreSnapshot> _snapshots;
        protected CoreSnapshot _currentCoreSnapshot;
        protected int _nextCoreSnapshotId;

        /// <summary>
        /// Value indicating the relative volume level of rendered audio (0 = mute, 255 = loudest).
        /// </summary>
        private byte _volume;

        public Machine()
        {
            _lockCount = 0;
            _runningState = RunningState.Paused;
            _requestedRunningState = RunningState.Paused;
            _actualRunningState = RunningState.Paused;
            _volume = 80;

            _core = new Core(Core.LatestVersion, Core.Type.CPC6128);

            _coreRequests = new Queue<MachineRequest>();
            _coreRequestsAvailable = new ManualResetEvent(false);

            _requests = new Queue<MachineRequest>();
            _requestsAvailable = new ManualResetEvent(false);

            _audioBuffer = new AudioBuffer(48000);
            _audioBuffer.OverrunThreshold = int.MaxValue;

            _allCoreSnapshots = new Dictionary<int, CoreSnapshot>();
            _currentCoreSnapshot = null;
            _nextCoreSnapshotId = 1000000000;
            _snapshots = new List<CoreSnapshot>();

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
                return _actualRunningState;
            }

            private set
            {
                if (_actualRunningState == value)
                {
                    return;
                }

                _actualRunningState = value;

                OnPropertyChanged();
            }
        }

        public abstract WaitHandle CanProcessEvent
        {
            get;
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

        public event MachineEventHandler Event;

        public virtual int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            // We need to make sure that our overrun threshold is enough so that we can fully satisfy at
            // least the next callback from NAudio. Without this, CPvC playback can become "stuttery."
            _audioBuffer.OverrunThreshold = samplesRequested * 2;

            return _audioBuffer.Render16BitStereo(Volume, buffer, offset, samplesRequested, false);
        }

        public void AdvancePlayback(int samples)
        {
            _audioBuffer.Advance(samples);
        }

        public MachineRequest Start()
        {
            MachineRequest request = MachineRequest.Resume();
            PushRequest(request);

            return request;
        }

        public MachineRequest Stop()
        {
            MachineRequest request = MachineRequest.Pause();
            PushRequest(request);

            return request;
        }


        public MachineRequest ToggleRunning()
        {
            if (RunningState == RunningState.Running)
            {
                return Stop();
            }
            else
            {
                return Start();
            }
        }

        /// <summary>
        /// Helper class that pauses the machine on creation and resumes the machine when the object is disposed.
        /// </summary>
        private class AutoLocker : IDisposable
        {
            private readonly Machine _machine;

            public AutoLocker(Machine machine)
            {
                _machine = machine;

                MachineRequest request = MachineRequest.Lock();
                _machine.PushRequest(request);
                request.Wait(Timeout.Infinite);
            }

            public void Dispose()
            {
                MachineRequest request = MachineRequest.Unlock();
                _machine.PushRequest(request);
                request.Wait(Timeout.Infinite);
            }
        }

        /// <summary>
        /// Pauses the machine and returns an IDisposable which, when disposed of, causes the machine to resume (if it was running before).
        /// </summary>
        /// <returns>An IDisposable interface.</returns>
        public IDisposable Lock()
        {
            return new AutoLocker(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void PushRequest(MachineRequest request)
        {
            // Put core requests in their own queue, since while paused, core requests
            // shouldn't be processed but non-core requests should be.
            bool coreRequest = true;

            switch (request.Type)
            {
                case MachineRequest.Types.Quit:
                case MachineRequest.Types.Pause:
                case MachineRequest.Types.Resume:
                case MachineRequest.Types.Reverse:
                case MachineRequest.Types.Lock:
                case MachineRequest.Types.Unlock:
                    coreRequest = false;
                    break;
            }

            if (coreRequest)
            {
                lock (_coreRequests)
                {
                    _coreRequests.Enqueue(request);
                    _coreRequestsAvailable.Set();
                }
            }
            else
            {
                lock (_requests)
                {
                    _requests.Enqueue(request);
                    _requestsAvailable.Set();
                }
            }
        }

        private void ProcessRequest(MachineRequest request)
        {
            if (request != null)
            {
                switch (request.Type)
                {
                    case MachineRequest.Types.Pause:
                        _requestedRunningState = RunningState.Paused;
                        Status = "Paused";
                        break;
                    case MachineRequest.Types.Resume:
                        ProcessResume();
                        break;
                    case MachineRequest.Types.Reverse:
                        _requestedRunningState = RunningState.Reverse;
                        Status = "Reversing";
                        break;
                    case MachineRequest.Types.Lock:
                        Interlocked.Increment(ref _lockCount);
                        break;
                    case MachineRequest.Types.Unlock:
                        Interlocked.Decrement(ref _lockCount);
                        break;
                    default:
                        throw new Exception(String.Format("Unknown machine request type '{0}'.", request.Type));
                }

                // Make sure to set the ActualRunningState *before* marking this request as "processed".
                if (_lockCount > 0)
                {
                    RunningState = RunningState.Paused;
                }
                else
                {
                    RunningState = _requestedRunningState;
                }

                request.SetProcessed();
            }

        }

        private (bool, MachineAction) ProcessCoreRequest(MachineRequest request)
        {
            bool success = true;

            MachineAction action = null;

            UInt64 ticks = Ticks;
            switch (request.Type)
            {
                case MachineRequest.Types.KeyPress:
                    if (_core.KeyPressSync(request.KeyCode, request.KeyDown))
                    {
                        action = MachineAction.KeyPress(ticks, request.KeyCode, request.KeyDown);
                    }
                    break;
                case MachineRequest.Types.Reset:
                    _core.ResetSync();
                    action = MachineAction.Reset(ticks);
                    break;
                case MachineRequest.Types.LoadDisc:
                    _core.LoadDiscSync(request.Drive, request.MediaBuffer.GetBytes());
                    action = MachineAction.LoadDisc(ticks, request.Drive, request.MediaBuffer);
                    break;
                case MachineRequest.Types.LoadTape:
                    _core.LoadTapeSync(request.MediaBuffer.GetBytes());
                    action = MachineAction.LoadTape(ticks, request.MediaBuffer);
                    break;
                case MachineRequest.Types.RunUntil:
                    action = RunForAWhile(request.StopTicks);

                    success = request.StopTicks <= Ticks;

                    if (_currentCoreSnapshot != null && action != null)
                    {
                        _currentCoreSnapshot.AudioBuffer.Write(action.AudioSamples);
                    }

                    break;
                case MachineRequest.Types.CoreVersion:
                    _core.ProcessCoreVersion(request.Version);

                    action = MachineAction.CoreVersion(Ticks, request.Version);

                    break;
                case MachineRequest.Types.LoadCore:
                    _core.LoadState(request.CoreState.GetBytes());
                    action = MachineAction.LoadCore(ticks, request.CoreState);
                    break;
                case MachineRequest.Types.CreateSnapshot:
                    _core.CreateSnapshotSync(request.SnapshotId);
                    action = MachineAction.CreateSnapshot(Ticks, request.SnapshotId);

                    break;
                case MachineRequest.Types.DeleteSnapshot:
                    if (_core.DeleteSnapshotSync(request.SnapshotId))
                    {
                        action = MachineAction.DeleteSnapshot(Ticks, request.SnapshotId);
                    }

                    break;
                case MachineRequest.Types.RevertToSnapshot:
                    {
                        action = ProcessRevertToSnapshot(request);
                        success = action != null;
                    }

                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown core request type {0}. Ignoring request.", request.Type));
            }

            return (success, action);
        }

        public virtual void ProcessResume()
        {
            _requestedRunningState = RunningState.Running;
            Status = "Resumed";
        }

        public virtual MachineAction ProcessRevertToSnapshot(MachineRequest request)
        {
            MachineAction action = null;
            if (_core.RevertToSnapshotSync(request.SnapshotId))
            {
                action = MachineAction.RevertToSnapshot(Ticks, request.SnapshotId);

                RaiseDisplayUpdated();
            }

            return action;
        }

        public MachineRequest RunUntil(UInt64 ticks)
        {
            MachineRequest request = MachineRequest.RunUntil(ticks);
            PushRequest(request);

            return request;
        }

        public void KeyPress(byte keycode, bool down)
        {
            PushRequest(MachineRequest.KeyPress(keycode, down));
        }

        public void AllKeysUp()
        {
            for (byte keycode = 0; keycode < 80; keycode++)
            {
                KeyPress(keycode, false);
            }
        }

        private MachineAction RunForAWhile(UInt64 stopTicks)
        {
            UInt64 ticks = Ticks;

            // Only proceed on an audio buffer underrun.
            if (!_audioBuffer.WaitForUnderrun(20))
            {
                return null;
            }

            // Limit how long we can run for to reduce audio lag.
            stopTicks = Math.Min(stopTicks, Ticks + 1000);

            if (stopTicks <= Ticks)
            {
                return null;
            }

            List<UInt16> audioSamples = new List<UInt16>();
            byte stopReason = _core.RunUntil(stopTicks, StopReasons.VSync, audioSamples);

            foreach (UInt16 sample in audioSamples)
            {
                _audioBuffer.Write(sample);
            }

            if ((stopReason & StopReasons.VSync) != 0)
            {
                BeginVSync();
            }

            return MachineAction.RunUntil(ticks, Ticks, audioSamples);
        }

        protected virtual CoreSnapshot CreateCoreSnapshot(int id)
        {
            CoreSnapshot coreSnapshot = new CoreSnapshot(id);

            _allCoreSnapshots.Add(coreSnapshot.Id, coreSnapshot);
            _snapshots.Add(coreSnapshot);

            return coreSnapshot;
        }

        protected virtual void BeginVSync()
        {
            RaiseDisplayUpdated();
        }

        public void GetScreen(IntPtr buffer, UInt64 size)
        {
            _core?.GetScreen(buffer, size);
        }

        public byte[] GetScreen()
        {
            return _core?.GetScreen();
        }

        public void SetScreen(byte[] screen)
        {
            if (screen == null)
            {
                return;
            }

            _core?.SetScreen(screen);
            RaiseDisplayUpdated();
        }

        public byte[] GetState()
        {
            return _core.GetState();
        }

        protected void RaiseDisplayUpdated()
        {
            DisplayUpdated?.Invoke(this, null);
            OnPropertyChanged(nameof(Ticks));
        }

        protected void BlankScreen()
        {
            byte[] screen = new byte[Display.Pitch * Display.Height];
            SetScreen(screen);
        }

        protected virtual MachineRequest GetNextCoreRequest()
        {
            MachineRequest request = null;

            lock (_coreRequests)
            {
                if (_coreRequests.Count > 0)
                {
                    request = _coreRequests.Dequeue();
                    if (_coreRequests.Count == 0)
                    {
                        _coreRequestsAvailable.Reset();
                    }
                }
            }

            return request;
        }

        public void MachineThread()
        {
            bool quitThread = false;

            MachineRequest coreRequest = null;

            WaitHandle[] allEvents = new WaitHandle[]
            {
                _requestsAvailable,
                CanProcessEvent
            };

            while (true)
            {
                // Clear out non-core requests first.
                while (true)
                {
                    MachineRequest request = null;
                    lock (_requests)
                    {
                        if (_requests.Count > 0)
                        {
                            request = _requests.Dequeue();
                        }
                        else
                        {
                            _requestsAvailable.Reset();
                            break;
                        }
                    }

                    if (request != null)
                    {
                        if (request.Type == MachineRequest.Types.Quit)
                        {
                            quitThread = true;
                            request.SetProcessed();
                            break;
                        }

                        ProcessRequest(request);
                    }
                }

                if (quitThread)
                {
                    break;
                }

                if (_actualRunningState == RunningState.Paused)
                {
                    _requestsAvailable.WaitOne();

                    continue;
                }

                WaitHandle.WaitAny(allEvents);

                if (coreRequest == null)
                {
                    coreRequest = GetNextCoreRequest();
                    if (coreRequest == null)
                    {
                        continue;
                    }
                }

                (bool done, MachineAction action) = ProcessCoreRequest(coreRequest);

                CoreActionDone(coreRequest, action);

                if (done)
                {
                    coreRequest.SetProcessed();
                    coreRequest = null;
                }
            }
        }

        protected abstract void CoreActionDone(MachineRequest request, MachineAction action);

        public virtual void Close()
        {
            PushRequest(new MachineRequest(MachineRequest.Types.Quit));

            _machineThread?.Join();
            _machineThread = null;

            _core?.Dispose();
            _core = null;
        }

        protected void RaiseEvent(MachineAction action)
        {
            Event?.Invoke(this, new MachineEventArgs(action));
        }

        protected class CoreSnapshot
        {
            private int _id;
            private readonly AudioBuffer _audioBuffer;

            public CoreSnapshot(int id)
            {
                _id = id;
                _audioBuffer = new AudioBuffer(-1);
            }

            public int Id
            {
                get
                {
                    return _id;
                }
            }

            public AudioBuffer AudioBuffer
            {
                get
                {
                    return _audioBuffer;
                }
            }
        }
    }
}
