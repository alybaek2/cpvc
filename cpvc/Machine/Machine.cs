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
        protected ManualResetEvent _quitThread;
        protected readonly Queue<MachineRequest> _coreRequests;
        protected ManualResetEvent _coreRequestsAvailable;

        private AudioBuffer _audioBuffer;

        protected Thread _machineThread;

        protected Dictionary<int, CoreSnapshot> _allCoreSnapshots;
        protected List<int> _snapshots;
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
            _quitThread = new ManualResetEvent(false);

            _audioBuffer = new AudioBuffer(48000);
            _audioBuffer.OverrunThreshold = int.MaxValue;

            _allCoreSnapshots = new Dictionary<int, CoreSnapshot>();
            _currentCoreSnapshot = null;
            _nextCoreSnapshotId = 1;
            _snapshots = new List<int>();

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
            MachineRequest request = new ResumeRequest();
            PushRequest(request);

            return request;
        }

        public MachineRequest Stop()
        {
            MachineRequest request = new PauseRequest();
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

                MachineRequest request = new LockRequest();
                _machine.PushRequest(request);
                request.Wait(Timeout.Infinite);
            }

            public void Dispose()
            {
                MachineRequest request = new UnlockRequest();
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
            if (request is CoreRequest)
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

        protected virtual void ProcessRequest(MachineRequest request)
        {
            if (request != null)
            {
                switch (request)
                {
                    case PauseRequest _:
                        _requestedRunningState = RunningState.Paused;
                        Status = "Paused";
                        break;
                    case ResumeRequest _:
                        ProcessResume();
                        break;
                    case ReverseRequest _:
                        _requestedRunningState = RunningState.Reverse;
                        Status = "Reversing";
                        break;
                    case LockRequest _:
                        Interlocked.Increment(ref _lockCount);
                        break;
                    case UnlockRequest _:
                        Interlocked.Decrement(ref _lockCount);
                        break;
                    default:
                        throw new Exception(String.Format("Unknown machine request type '{0}'.", request.GetType()));
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

        protected (bool, IMachineAction) ProcessCoreRequest(MachineRequest request)
        {
            bool success = true;

            IMachineAction action = null;

            UInt64 ticks = Ticks;
            switch (request)
            {
                case KeyPressRequest keyPressRequest:
                    if (_core.KeyPressSync(keyPressRequest.KeyCode, keyPressRequest.KeyDown))
                    {
                        action = new KeyPressAction(ticks, keyPressRequest.KeyCode, keyPressRequest.KeyDown);
                    }
                    break;
                case ResetRequest _:
                    _core.ResetSync();
                    action = new ResetAction(ticks);
                    break;
                case LoadDiscRequest loadDiscRequest:
                    _core.LoadDiscSync(loadDiscRequest.Drive, loadDiscRequest.MediaBuffer.GetBytes());
                    action = new LoadDiscAction(ticks, loadDiscRequest.Drive, loadDiscRequest.MediaBuffer);
                    break;
                case LoadTapeRequest loadTapeRequest:
                    _core.LoadTapeSync(loadTapeRequest.MediaBuffer.GetBytes());
                    action = new LoadTapeAction(ticks, loadTapeRequest.MediaBuffer);
                    break;
                case RunUntilRequest runUntilRequest:
                    action = RunForAWhile(runUntilRequest.StopTicks);

                    success = runUntilRequest.StopTicks <= Ticks;

                    if (_currentCoreSnapshot != null && action is RunUntilAction runUntilAction)
                    {
                        _currentCoreSnapshot.AudioBuffer.Write(runUntilAction.AudioSamples);
                    }

                    break;
                case CoreVersionRequest coreVersionRequest:
                    _core.ProcessCoreVersion(coreVersionRequest.Version);

                    action = new CoreVersionAction(Ticks, coreVersionRequest.Version);

                    break;
                case LoadCoreRequest loadCoreRequest:
                    _core.LoadState(loadCoreRequest.State.GetBytes());
                    if (loadCoreRequest.Screen != null)
                    {
                        _core.SetScreen(loadCoreRequest.Screen.GetBytes());
                        RaiseDisplayUpdated();
                    }
                    OnPropertyChanged(nameof(Ticks));

                    action = new LoadCoreAction(ticks, loadCoreRequest.State, loadCoreRequest.Screen);
                    break;
                case CreateSnapshotRequest snapshotRequest:
                    _core.CreateSnapshotSync(snapshotRequest.SnapshotId);
                    action = new CreateSnapshotAction(Ticks, snapshotRequest.SnapshotId);

                    break;
                case DeleteSnapshotRequest snapshotRequest:
                    if (_core.DeleteSnapshotSync(snapshotRequest.SnapshotId))
                    {
                        action = new DeleteSnapshotAction(Ticks, snapshotRequest.SnapshotId);
                    }

                    break;
                case RevertToSnapshotRequest snapshotRequest:
                    {
                        action = ProcessRevertToSnapshot(snapshotRequest);
                        success = action != null;
                    }

                    break;
                default:
                    throw new ArgumentException(String.Format("Unknown core request type {0}. Ignoring request.", request.GetType()));
            }

            return (success, action);
        }

        public virtual void ProcessResume()
        {
            _requestedRunningState = RunningState.Running;
            Status = "Resumed";
        }

        public virtual IMachineAction ProcessRevertToSnapshot(RevertToSnapshotRequest request)
        {
            IMachineAction action = null;
            if (_core.RevertToSnapshotSync(request.SnapshotId))
            {
                action = MachineAction.RevertToSnapshot(Ticks, request.SnapshotId);

                RaiseDisplayUpdated();

                DeleteCoreSnapshot(request.SnapshotId);
            }

            return action;
        }

        public MachineRequest RunUntil(UInt64 ticks)
        {
            MachineRequest request = new RunUntilRequest(ticks);
            PushRequest(request);

            return request;
        }

        public void KeyPress(byte keycode, bool down)
        {
            PushRequest(new KeyPressRequest(keycode, down));
        }

        public void AllKeysUp()
        {
            for (byte keycode = 0; keycode < 80; keycode++)
            {
                KeyPress(keycode, false);
            }
        }

        private IMachineAction RunForAWhile(UInt64 stopTicks)
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

            return new RunUntilAction(ticks, Ticks, audioSamples);
        }

        protected virtual CoreSnapshot CreateCoreSnapshot(int id)
        {
            CoreSnapshot coreSnapshot = new CoreSnapshot(id);

            lock (_snapshots)
            {
                _allCoreSnapshots.Add(id, coreSnapshot);
                _snapshots.Add(id);
            }

            return coreSnapshot;
        }

        protected void DeleteCoreSnapshot(int id)
        {
            lock (_snapshots)
            {
                _snapshots.Remove(id);
                _allCoreSnapshots.Remove(id);

                _core.DeleteSnapshotSync(id);

                IMachineAction action = new DeleteSnapshotAction(Ticks, id);
                RaiseEvent(action);
            }
        }

        protected void DeleteAllCoreSnapshots()
        {
            lock (_snapshots)
            {
                List<int> snapshotIds = new List<int>(_snapshots);

                foreach (int id in snapshotIds)
                {
                    DeleteCoreSnapshot(id);
                }
            }
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
            MachineRequest coreRequest = null;

            WaitHandle[] allEvents = new WaitHandle[]
            {
                _requestsAvailable,
                _quitThread,
                CanProcessEvent
            };

            WaitHandle[] pausedEvents = new WaitHandle[]
            {
                _requestsAvailable,
                _quitThread
            };

            while (!_quitThread.WaitOne(0))
            {
                // Clear out non-core requests first.
                while (!_quitThread.WaitOne(0))
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
                        ProcessRequest(request);
                    }
                }

                if (_quitThread.WaitOne(0))
                {
                    break;
                }

                if (_actualRunningState == RunningState.Paused)
                {
                    WaitHandle.WaitAny(pausedEvents);

                    continue;
                }

                do
                {
                    WaitHandle.WaitAny(allEvents);

                    if (coreRequest == null)
                    {
                        coreRequest = GetNextCoreRequest();
                        if (coreRequest == null)
                        {
                            continue;
                        }
                    }

                    (bool done, IMachineAction action) = ProcessCoreRequest(coreRequest);

                    CoreActionDone(coreRequest, action);

                    if (done)
                    {
                        coreRequest.SetProcessed();
                        coreRequest = null;
                    }
                }
                while (coreRequest != null && !_quitThread.WaitOne(0));
            }

            _quitThread.Reset();
        }

        protected abstract void CoreActionDone(MachineRequest request, IMachineAction action);

        public virtual void Close()
        {
            _quitThread.Set();

            _machineThread?.Join();
            _machineThread = null;

            _core?.Dispose();
            _core = null;
        }

        protected void RaiseEvent(IMachineAction action)
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
