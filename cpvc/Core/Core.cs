using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace CPvC
{
    public enum RunningState
    {
        Paused,
        Running,
        Reverse
    }

    /// <summary>
    /// Delegate for auditing the actions taken by a Core.
    /// </summary>
    /// <param name="request">The original request.</param>
    /// <param name="action">The action taken.</param>
    public delegate void RequestProcessedDelegate(Core core, CoreRequest request, CoreAction action);

    /// <summary>
    /// Delegate to be called whenever the VSync signal goes from low to high.
    /// </summary>
    /// <param name="core">The Core whose VSync has gone high.</param>
    public delegate void BeginVSyncDelegate(Core core);

    /// <summary>
    /// Class that wraps the CoreCLR class and provides callbacks for auditors and vsync events. Also runs the core in a background thread.
    /// </summary>
    public sealed class Core : INotifyPropertyChanged, IDisposable
    {
        // Core versions.
        public const int LatestVersion = 1;
        private class Corev1 : CoreCLR, ICore { }

        public enum Type
        {
            CPC6128
        }

        private ICore _coreCLR;
        private int _version;
        private readonly List<CoreRequest> _requests;
        private object _lockObject;

        private bool _quitThread;
        private Thread _coreThread;

        private RunningState _runningState;

        private class SnapshotInfo
        {
            private UInt64 _ticks;
            private int _snapshotId;

            public SnapshotInfo(UInt64 ticks, int snapshotId)
            {
                _ticks = ticks;
                _snapshotId = snapshotId;
            }

            public UInt64 Ticks
            {
                get
                {
                    return _ticks;
                }
            }

            public int SnapshotId
            {
                get
                {
                    return _snapshotId;
                }
            }
        }

        private List<SnapshotInfo> _snapshots;
        private const int _snapshotLimit = 500;

        private bool _keepRunning;

        /// <summary>
        /// Frequency (in samples per second) at which the Core will populate its audio buffers.
        /// </summary>
        /// <remarks>
        /// Note that this rate divided by the rate audio samples are read gives the speed at which the CPvC instance will run.
        /// </remarks>
        private UInt32 _audioSamplingFrequency;

        /// <summary>
        /// Value indicating the relative volume level of rendered audio (0 = mute, 255 = loudest).
        /// </summary>
        private byte _volume;

        public RequestProcessedDelegate Auditors { get; set; }
        public BeginVSyncDelegate BeginVSync { get; set; }

        public AudioBuffer AudioSamples
        {
            get
            {
                return _audioSamples;
            }
        }

        private AutoResetEvent _audioReady;
        private AutoResetEvent _requestQueueEmpty;
        private AutoResetEvent _requestQueueNonEmpty;

        private AudioBuffer _audioSamples;

        // AY volume table (c) by Hacker KAY (http://kay27.narod.ru/ay.html)
        private static readonly ushort[] _amplitudes = {
            0, 836, 1212, 1773, 2619, 3875, 5397, 8823,
            10392, 16706, 23339, 29292, 36969, 46421, 55195, 65535
        };

        private const UInt64 _totalSnapshots = 500;
        private SynchronizationContext _syncContext;

        public byte Volume
        {
            get
            {
                return _volume;
            }

            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    OnPropertyChanged("Volume");
                }
            }
        }

        public int Version
        {
            get
            {
                return _version;
            }
        }

        private Core(int version)
        {
            _version = version;
            _coreCLR = CreateVersionedCore(version);
            _requests = new List<CoreRequest>();
            _lockObject = new object();

            _quitThread = false;
            _coreThread = null;

            _keepRunning = true;

            BeginVSync = null;

            _snapshots = new List<SnapshotInfo>();

            SetScreen(IntPtr.Zero);

            _audioSamplingFrequency = 48000;
            _volume = 80;
            EnableTurbo(false);

            _audioReady = new AutoResetEvent(true);
            _requestQueueEmpty = new AutoResetEvent(true);
            _requestQueueNonEmpty = new AutoResetEvent(false);

            _audioSamples = new AudioBuffer();

            // Ensure any OnPropChanged calls are executed on the main thread. There may be a better way of
            // doing this, such as wrapping add and remove for Command.CanExecuteChanged in a lambda that
            // calls the target on its own SynchronizationContext.
            _syncContext = SynchronizationContext.Current;

            RunningState = RunningState.Paused;
        }

        static private ICore CreateVersionedCore(int version)
        {
            switch (version)
            {
                case 1:
                    return new Corev1();
                default:
                    throw new Exception(String.Format("Cannot instantiate CLR core version {0}.", version));
            }
        }

        private UInt64 _lastTicksNotified = 0;
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                if (_syncContext != null)
                {
                    _syncContext.Post(_ => PropertyChanged(this, new PropertyChangedEventArgs(name)), null);
                }
                else
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public void Dispose()
        {
            Stop();

            Auditors = null;
            BeginVSync = null;

            _coreCLR?.Dispose();
            _coreCLR = null;

            _audioReady?.Dispose();
            _audioReady = null;

            _requestQueueEmpty?.Dispose();
            _requestQueueEmpty = null;

            _requestQueueNonEmpty?.Dispose();
            _requestQueueNonEmpty = null;
        }

        /// <summary>
        /// Asynchronously updates the state of a key.
        /// </summary>
        /// <param name="keycode">Key whose state is to be changed.</param>
        /// <param name="down">Indicates the desired state of the key. True indicates down, false indicates up.</param>
        public void KeyPress(byte keycode, bool down)
        {
            PushRequest(CoreRequest.KeyPress(keycode, down));
        }

        /// <summary>
        /// Asynchronously performs a soft reset of the core.
        /// </summary>
        public void Reset()
        {
            PushRequest(CoreRequest.Reset());
        }

        public void Quit()
        {
            PushRequest(CoreRequest.Quit());
        }

        public void LoadCoreState(IBlob state)
        {
            PushRequest(CoreRequest.LoadCore(state));
        }

        /// <summary>
        /// Asynchronously loads a disc.
        /// </summary>
        /// <param name="drive">Specifies which drive to load. 0 for drive A and 1 for drive B.</param>
        /// <param name="discImage">A byte array containing an uncompressed .DSK image.</param>
        public void LoadDisc(byte drive, byte[] discImage)
        {
            PushRequest(CoreRequest.LoadDisc(drive, discImage));
        }

        /// <summary>
        /// Asynchronously loads a tape and begins playing it.
        /// </summary>
        /// <param name="tapeImage">A byte array containing an uncompressed .CDT image.</param>
        public void LoadTape(byte[] tapeImage)
        {
            PushRequest(CoreRequest.LoadTape(tapeImage));
        }

        /// <summary>
        /// Advances the audio playback position by the given number of samples.
        /// </summary>
        /// <param name="samples"></param>
        public void AdvancePlayback(int samples)
        {
            for (int i = 0; i < samples; i++)
            {
                if (!_audioSamples.Pop(out UInt16 sample))
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Sets a Pointer to a block of unmanaged memory to be used by the core for video rendering.
        /// </summary>
        public void SetScreen(IntPtr screenBuffer)
        {
            lock (_lockObject)
            {
                _coreCLR.SetScreen(screenBuffer, Display.Pitch, Display.Height, Display.Width);
            }
        }

        public IntPtr GetScreen()
        {
            lock (_lockObject)
            {
                return _coreCLR.GetScreen();
            }
        }

        /// <summary>
        /// Indicates the current running state of the core.
        /// </summary>
        public RunningState RunningState
        {
            get
            {
                return _runningState;
            }

            set
            {
                if (_runningState == value)
                {
                    return;
                }

                _runningState = value;
                OnPropertyChanged("RunningState");
            }
        }

        public bool KeepRunning
        {
            set
            {
                _keepRunning = value;
            }
        }

        /// <summary>
        /// Indicates the number of ticks that have elapsed since the core was started. Note that each tick is exactly 0.25 microseconds.
        /// </summary>
        public UInt64 Ticks
        {
            get
            {
                // No need to lock _coreCLR just to get the ticks.
                return _coreCLR.Ticks();
            }
        }

        /// <summary>
        /// Serializes the core to a byte array.
        /// </summary>
        /// <returns>A byte array containing the serialized core.</returns>
        public byte[] GetState()
        {
            lock (_lockObject)
            {
                return _coreCLR.GetState();
            }
        }

        /// <summary>
        /// Deserializes the core from a byte array.
        /// </summary>
        /// <param name="state">A byte array created by <c>GetState</c>.</param>
        public void LoadState(byte[] state)
        {
            lock (_lockObject)
            {
                _coreCLR.LoadState(state);
            }
        }

        /// <summary>
        /// Creates a core based on the result of a previous <c>GetState</c> call.
        /// </summary>
        /// <param name="version">Version of the core to create.</param>
        /// <param name="state">A byte array created by <c>GetState</c>.</param>
        /// <returns>The newly created core.</returns>
        static public Core Create(int version, byte[] state)
        {
            Core core = new Core(version);
            core.LoadState(state);

            return core;
        }

        /// <summary>
        /// Creates a new core.
        /// </summary>
        /// <param name="type">The model of CPC the core should emulate.</param>
        /// <returns>A core of the specified type.</returns>
        static public Core Create(int version, Type type)
        {
            switch (type)
            {
                case Type.CPC6128:
                    {
                        Core core = new Core(version);

                        core.SetLowerROM(Resources.OS6128);
                        core.SetUpperROM(0, Resources.Basic6128);
                        core.SetUpperROM(7, Resources.Amsdos6128);

                        return core;
                    }
                default:
                    throw new ArgumentException(String.Format("Unknown core type {0}", type));
            }
        }

        public void Start()
        {
            SetRunning(RunningState.Running);
        }

        public void Stop()
        {
            SetRunning(RunningState.Paused);
        }

        public void SetLowerROM(byte[] lowerROM)
        {
            _coreCLR.LoadLowerROM(lowerROM);
        }

        public void SetUpperROM(byte slot, byte[] upperROM)
        {
            _coreCLR.LoadUpperROM(slot, upperROM);
        }

        /// <summary>
        /// Executes instruction cycles until the core's clock is equal to or greater than <c>ticks</c>.
        /// </summary>
        /// <param name="ticks">Clock value to run core until.</param>
        /// <param name="stopReason">Bitmask specifying what conditions will force execution to stop prior to the clock reaching <c>ticks</c>.</param>
        /// <returns>A bitmask specifying why the execution stopped. See <c>StopReasons</c> for a list of values.</returns>
        public byte RunUntil(UInt64 ticks, byte stopReason, List<UInt16> audioSamples)
        {
            lock (_lockObject)
            {
                return _coreCLR.RunUntil(ticks, stopReason, audioSamples);
            }
        }

        /// <summary>
        /// Executes instruction cycles for a given number of VSync events.
        /// </summary>
        /// <param name="vsyncCount">The number of VSync events.</param>
        public void RunForVSync(int vsyncCount)
        {
            while (vsyncCount > 0)
            {
                byte stopReason = RunUntil(Ticks + 20000, StopReasons.VSync, null);
                if (stopReason == StopReasons.VSync)
                {
                    vsyncCount--;
                }
            }
        }

        /// <summary>
        /// Thread for continuously running the core.
        /// </summary>
        /// <remarks>
        /// The purpose of running the core in a background thread is primarily to prevent the foreground (UI) thread from being blocked. 
        /// The core thread processes requests in order from the internal request queue. When no requests are available, the thread will
        /// perform "RunUntil" requests until new requests are available in the request queue.
        /// </remarks>
        public void CoreThread()
        {
            while (!_quitThread)
            {
                if (RunningState == RunningState.Running)
                {
                    if (ProcessNextRequest())
                    {
                        break;
                    }
                }
            }

            _quitThread = false;
            RunningState = RunningState.Paused;
        }

        public int RenderAudio16BitStereo(byte[] buffer, int offset, int samplesRequested, AudioBuffer samples)
        {
            // Each sample requires four bytes, so take the size of the buffer to be the largest multiple
            // of 4 less than or equal to the length of the buffer.
            int bufferSize = 4 * (buffer.Length / 4);

            // Skew the volume factor so the volume control presents a more balanced range of volumes.
            double volumeFactor = Math.Pow(_volume / 255.0, 3);

            int samplesWritten = 0;

            while (samplesWritten < samplesRequested && offset < bufferSize)
            {
                if (!samples.Pop(out UInt16 sample))
                {
                    break;
                }

                byte channelA = (byte) (sample & 0x000f);
                byte channelB = (byte)((sample & 0x00f0) >> 4);
                byte channelC = (byte)((sample & 0x0f00) >> 8);

                // Treat Channel A as "Left", Channel B as "Centre", and Channel C as "Right".
                UInt32 left = (UInt32)(((2 * _amplitudes[channelA]) + _amplitudes[channelB]) * volumeFactor) / 3;
                UInt32 right = (UInt32)(((2 * _amplitudes[channelC]) + _amplitudes[channelB]) * volumeFactor) / 3;

                // Divide by two to deal with the fact NAudio requires signed 16-bit samples.
                left = (UInt16)(left / 2);
                right = (UInt16)(right / 2);

                buffer[offset] = (byte)(left & 0xFF);
                buffer[offset + 1] = (byte)(left >> 8);
                buffer[offset + 2] = (byte)(right & 0xFF);
                buffer[offset + 3] = (byte)(right >> 8);

                offset += 4;
                samplesWritten++;
            }

            return samplesWritten;
        }

        /// <summary>
        /// Enables or disables turbo mode.
        /// </summary>
        /// <param name="enabled">Indicates whether turbo mode is to be enabled.</param>
        public void EnableTurbo(bool enabled)
        {
            // For now, hard-code turbo mode to 10 times normal speed.
            UInt32 frequency = enabled ? (_audioSamplingFrequency / 10) : _audioSamplingFrequency;

            lock (_lockObject)
            {
                _coreCLR.AudioSampleFrequency(frequency);
            }
        }

        public void PushRequest(CoreRequest request)
        {
            lock (_requests)
            {
                _requests.Add(request);
                _requestQueueEmpty.Reset();
                _requestQueueNonEmpty.Set();
            }
        }

        private CoreRequest FirstRequest()
        {
            lock (_requests)
            {
                if (_requests.Count > 0)
                {
                    return _requests[0];
                }

                return null;
            }
        }

        private void RemoveFirstRequest()
        {
            lock (_requests)
            {
                if (_requests.Count > 0)
                {
                    _requests.RemoveAt(0);

                    if (_requests.Count == 0)
                    {
                        _requestQueueEmpty.Set();
                    }
                    else
                    {
                        _requestQueueNonEmpty.Set();
                    }
                }
            }
        }

        public void SetRunningState(RunningState runningState)
        {
            SetRunning(runningState);
        }

        private void SetRunning(RunningState runningState)
        {
            if (runningState == RunningState)
            {
                return;
            }

            switch (runningState)
            {
                case RunningState.Reverse:
                case RunningState.Paused:
                    if (_coreThread != null && _coreThread.IsAlive)
                    {
                        _quitThread = true;
                        _coreThread.Join();
                        _coreThread = null;
                    }
                    break;
                case RunningState.Running:
                    if (_coreThread == null || !_coreThread.IsAlive)
                    {
                        _coreThread = new Thread(CoreThread);
                        _coreThread.Start();
                    }
                    break;
            }

            RunningState = runningState;
        }

        private bool ProcessNextRequest()
        {
            bool success = true;

            CoreRequest request = FirstRequest();
            CoreAction action = null;

            if (request == null)
            {
                if (_keepRunning)
                {
                    action = RunForAWhile(Ticks + 1000);
                }
                else
                {
                    _requestQueueNonEmpty.WaitOne(10);
                    return false;
                }
            }
            else
            {
                UInt64 ticks = Ticks;
                switch (request.Type)
                {
                    case CoreRequest.Types.KeyPress:
                        lock (_lockObject)
                        {
                            if (_coreCLR.KeyPress(request.KeyCode, request.KeyDown))
                            {
                                action = CoreAction.KeyPress(ticks, request.KeyCode, request.KeyDown);
                            }
                        }
                        break;
                    case CoreRequest.Types.Reset:
                        lock (_lockObject)
                        {
                            _coreCLR.Reset();
                        }
                        action = CoreAction.Reset(ticks);
                        break;
                    case CoreRequest.Types.LoadDisc:
                        lock (_lockObject)
                        {
                            _coreCLR.LoadDisc(request.Drive, request.MediaBuffer.GetBytes());
                        }
                        action = CoreAction.LoadDisc(ticks, request.Drive, request.MediaBuffer);
                        break;
                    case CoreRequest.Types.LoadTape:
                        lock (_lockObject)
                        {
                            _coreCLR.LoadTape(request.MediaBuffer.GetBytes());
                        }
                        action = CoreAction.LoadTape(ticks, request.MediaBuffer);
                        break;
                    case CoreRequest.Types.RunUntilForce:
                        {
                            action = RunForAWhile(request.StopTicks);

                            success = (request.StopTicks <= Ticks);
                            //action = CoreAction.RunUntilForce(ticks, Ticks, null);
                        }
                        break;
                    case CoreRequest.Types.CoreVersion:
                        lock (_lockObject)
                        {
                            byte[] state = GetState();

                            ICore newCore = Core.CreateVersionedCore(request.Version);
                            newCore.LoadState(state);

                            IntPtr pScr = _coreCLR.GetScreen();

                            _coreCLR.Dispose();
                            _coreCLR = newCore;

                            SetScreen(pScr);
                        }
                        break;
                    case CoreRequest.Types.LoadCore:
                        lock (_lockObject)
                        {
                            _coreCLR.LoadState(request.CoreState.GetBytes());
                        }

                        action = CoreAction.LoadCore(ticks, request.CoreState);
                        break;
                    case CoreRequest.Types.SaveSnapshot:
                        lock (_lockObject)
                        {
                            _coreCLR.SaveSnapshot(request.SnapshotId);
                        }

                        action = CoreAction.SaveSnapshot(ticks, request.SnapshotId);
                        break;
                    case CoreRequest.Types.LoadSnapshot:
                        lock (_lockObject)
                        {
                            _coreCLR.LoadSnapshot(request.SnapshotId);
                        }

                        _lastTicksNotified = 0;
                        OnPropertyChanged("Ticks");

                        action = CoreAction.LoadSnapshot(ticks, request.SnapshotId);
                        break;
                    case CoreRequest.Types.Quit:
                        RemoveFirstRequest();
                        return true;
                    default:
                        Diagnostics.Trace("Unknown core request type {0}. Ignoring request.", request.Type);
                        break;
                }
            }

            Auditors?.Invoke(this, request, action);

            if (request != null && success)
            {
                RemoveFirstRequest();
            }

            return false;
        }

        public void SaveSnapshot(int snapshotId)
        {
            _coreCLR.SaveSnapshot(snapshotId);
            Auditors?.Invoke(this, null, CoreAction.SaveSnapshot(Ticks, snapshotId));
        }

        public bool LoadSnapshot(int snapshotId)
        {
            if (_coreCLR.LoadSnapshot(snapshotId))
            {
                _lastTicksNotified = 0;

                Auditors?.Invoke(this, null, CoreAction.LoadSnapshot(Ticks, snapshotId));
                return true;
            }

            return false;
        }

        private CoreAction RunForAWhile(UInt64 stopTicks)
        {
            UInt64 ticks = Ticks;

            // Check for audio overrun.
            if (_audioSamples.Overrun())
            {
                if (!_audioSamples.WaitForUnderrun(10))
                {
                    return null;
                }
            }

            List<UInt16> audioSamples = new List<UInt16>();
            byte stopReason = RunUntil(stopTicks, (byte)(StopReasons.VSync), audioSamples);

            foreach (UInt16 sample in audioSamples)
            {
                _audioSamples.Push(sample);
            }

            if ((stopReason & StopReasons.VSync) != 0)
            {
                BeginVSync?.Invoke(this);
            }

            // Don't fire a Ticks notification more than once a second. Otherwise, this can fire
            // a hundred times per second, consuming a lot of CPU and causing the machine to 
            // lock up whenever turbo mode is enabled.
            if (Ticks > (_lastTicksNotified + 4000000))
            {
                OnPropertyChanged("Ticks");
                _lastTicksNotified = Ticks;
            }

            return CoreAction.RunUntilForce(ticks, Ticks, audioSamples);
        }
    }
}
