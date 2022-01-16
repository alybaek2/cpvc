using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CPvC
{
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
    /// Delegate to be called by the core thread to get a request when none exists in the request queue.
    /// </summary>
    /// <returns>The CoreRequest that is to be processed.</returns>
    public delegate CoreRequest IdleRequestDelegate();

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
        private readonly Queue<CoreRequest> _requests;
        private object _lockObject;

        private bool _quitThread;
        private Thread _coreThread;

        public RequestProcessedDelegate Auditors { get; set; }
        public BeginVSyncDelegate BeginVSync { get; set; }
        public IdleRequestDelegate IdleRequest { get; set; }

        public AudioBuffer AudioBuffer
        {
            get
            {
                return _audioBuffer;
            }
        }

        private AutoResetEvent _audioReady;
        private AutoResetEvent _requestQueueNonEmpty;

        private ManualResetEvent _runningEvent;

        private AudioBuffer _audioBuffer;

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
            _requests = new Queue<CoreRequest>();
            _lockObject = new object();

            _quitThread = false;
            _coreThread = null;

            BeginVSync = null;

            SetScreen();

            _audioReady = new AutoResetEvent(true);
            _requestQueueNonEmpty = new AutoResetEvent(false);

            _runningEvent = new ManualResetEvent(false);

            _audioBuffer = new AudioBuffer(48000);

            _coreThread = new Thread(CoreThread);
            _coreThread.Start();
        }

        static private ICore CreateVersionedCore(int version)
        {
            switch (version)
            {
                case 1:
                    return new Corev1();
                default:
                    throw new ArgumentException(String.Format("Cannot instantiate CLR core version {0}.", version), nameof(version));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose()
        {
            Stop();

            _quitThread = true;
            _coreThread?.Join();
            _coreThread = null;

            Auditors = null;
            BeginVSync = null;

            _coreCLR?.Dispose();
            _coreCLR = null;

            _audioReady?.Dispose();
            _audioReady = null;

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
        /// Helper method that sets all keys into the "up" state.
        /// </summary>
        /// <remarks>
        /// Useful after loading a snapshot.
        /// </remarks>
        public void AllKeysUp()
        {
            for (byte keycode = 0; keycode < 80; keycode++)
            {
                KeyPress(keycode, false);
            }
        }

        /// <summary>
        /// Asynchronously performs a soft reset of the core.
        /// </summary>
        public void Reset()
        {
            PushRequest(CoreRequest.Reset());
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
            _audioBuffer.Advance(samples);
        }

        /// <summary>
        /// Sets width, height, and pitch of the screen.
        /// </summary>
        public void SetScreen()
        {
            lock (_lockObject)
            {
                _coreCLR.SetScreen(Display.Pitch, Display.Height, Display.Width);
            }
        }

        public void CopyScreen(IntPtr screenBuffer, UInt64 size)
        {
            lock (_lockObject)
            {
                _coreCLR?.CopyScreen(screenBuffer, size);
            }
        }

        /// <summary>
        /// Indicates the number of ticks that have elapsed since the core was started. Note that each tick is exactly 0.25 microseconds.
        /// </summary>
        public UInt64 Ticks
        {
            get
            {
                lock (_lockObject)
                {
                    return _coreCLR?.Ticks() ?? 0;
                }
            }
        }

        public bool Running
        {
            get
            {
                return _runningEvent.WaitOne(0);
            }

            set
            {
                if (value == Running)
                {
                    return;
                }

                if (value)
                {
                    _runningEvent.Set();
                }
                else
                {
                    _runningEvent.Reset();
                }

                OnPropertyChanged();
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
            _runningEvent.Set();
        }

        public void Stop()
        {
            _runningEvent.Reset();
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
                if (!_runningEvent.WaitOne(20))
                {
                    continue;
                }

                if (ProcessNextRequest())
                {
                    break;
                }
            }

            _quitThread = false;
        }

        public void PushRequest(CoreRequest request)
        {
            lock (_requests)
            {
                _requests.Enqueue(request);
                _requestQueueNonEmpty.Set();
            }
        }

        private CoreRequest FirstRequest()
        {
            lock (_requests)
            {
                if (_requests.Count > 0)
                {
                    return _requests.Peek();
                }

                return null;
            }
        }

        private void RemoveFirstRequest()
        {
            lock (_requests)
            {
                _requests.Dequeue();

                if (_requests.Count > 0)
                {
                    _requestQueueNonEmpty.Set();
                }
            }
        }

        private bool ProcessNextRequest()
        {
            bool success = true;

            CoreRequest firstRequest = FirstRequest();
            bool removeFirst = true;
            CoreRequest request = firstRequest;
            CoreAction action = null;

            if (request == null)
            {
                removeFirst = false;

                if (IdleRequest != null)
                {
                    request = IdleRequest();
                }

                if (request == null)
                {
                    _requestQueueNonEmpty.WaitOne(20);
                    return false;
                }
            }

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
                case CoreRequest.Types.RunUntil:
                    {
                        action = RunForAWhile(request.StopTicks);

                        success = (request.StopTicks <= Ticks);
                    }
                    break;
                case CoreRequest.Types.CoreVersion:
                    lock (_lockObject)
                    {
                        byte[] state = GetState();

                        ICore newCore = Core.CreateVersionedCore(request.Version);
                        newCore.LoadState(state);
                        newCore.SetScreen(Display.Pitch, Display.Height, Display.Width);

                        _coreCLR.Dispose();
                        _coreCLR = newCore;
                    }
                    break;
                case CoreRequest.Types.LoadCore:
                    lock (_lockObject)
                    {
                        _coreCLR.LoadState(request.CoreState.GetBytes());
                    }

                    action = CoreAction.LoadCore(ticks, request.CoreState);
                    break;
                case CoreRequest.Types.CreateSnapshot:
                    lock (_lockObject)
                    {
                        action = CreateSnapshot(request.SnapshotId);
                    }

                    break;
                case CoreRequest.Types.DeleteSnapshot:
                    lock (_lockObject)
                    {
                        action = DeleteSnapshot(request.SnapshotId);
                    }

                    break;
                case CoreRequest.Types.RevertToSnapshot:
                    lock (_lockObject)
                    {
                        action = RevertToSnapshot(request.SnapshotId);
                    }

                    break;
                default:
                    Diagnostics.Trace("Unknown core request type {0}. Ignoring request.", request.Type);
                    break;
            }

            if (removeFirst && success)
            {
                RemoveFirstRequest();
            }

            Auditors?.Invoke(this, request, action);

            return false;
        }

        private CoreAction CreateSnapshot(int id)
        {
            _coreCLR.CreateSnapshot(id);

            return CoreAction.CreateSnapshot(Ticks, id);
        }

        private CoreAction DeleteSnapshot(int id)
        {
            bool result = _coreCLR.DeleteSnapshot(id);
            if (!result)
            {
                return null;
            }

            return CoreAction.DeleteSnapshot(Ticks, id);
        }

        private CoreAction RevertToSnapshot(int id)
        {
            bool result = _coreCLR.RevertToSnapshot(id);
            if (!result)
            {
                return null;
            }

            OnPropertyChanged("Ticks");

            return CoreAction.RevertToSnapshot(Ticks, id);
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
            byte stopReason = RunUntil(stopTicks, StopReasons.VSync, audioSamples);

            foreach (UInt16 sample in audioSamples)
            {
                _audioBuffer.Write(sample);
            }

            if ((stopReason & StopReasons.VSync) != 0)
            {
                OnPropertyChanged("Ticks");
                BeginVSync?.Invoke(this);
            }

            return CoreAction.RunUntil(ticks, Ticks, audioSamples);
        }
    }
}
