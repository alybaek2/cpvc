using System;
using System.Threading;

namespace CPvC
{
    /// <summary>
    /// Represents a request to the core thread.
    /// </summary>
    public class MachineRequest
    {
        public MachineRequest(Types type)
        {
            Type = type;
            _processed = new ManualResetEvent(false);
        }

        public enum Types
        {
            Reset,
            KeyPress,
            LoadDisc,
            LoadTape,
            CoreVersion,
            RunUntil,
            LoadCore,
            CreateSnapshot,
            DeleteSnapshot,
            RevertToSnapshot,
            Quit,
            Pause,
            Resume,
            Reverse,
            Lock,
            Unlock

        }

        public Types Type { get; protected set; }

        /// <summary>
        /// The key that has been pressed. Key codes are encoded as a two digit decimal number; the first digit is the key bit and the second is the key line.
        /// </summary>
        public byte KeyCode { get; protected set; }

        /// <summary>
        /// Indicates whether the key is in the down state. If false, the key is "up" (ie. not pressed).
        /// </summary>
        public bool KeyDown { get; protected set; }

        /// <summary>
        /// Indicates the drive for LoadDisc; 0 is Drive A and 1 is Drive B.
        /// </summary>
        public byte Drive { get; protected set; }

        /// <summary>
        /// A buffer representing an uncompressed tape or disc image.
        /// </summary>
        public IBlob MediaBuffer { get; protected set; }

        /// <summary>
        /// For a request, indicates the desired ticks to stop at. For an action represents the actual ticks value that the core stopped at.
        /// </summary>
        public UInt64 StopTicks { get; set; }

        /// <summary>
        /// For an action, this indicates the version of the core to switch to.
        /// </summary>
        public int Version { get; protected set; }

        /// <summary>
        /// For a request, this indicates the Core to be loaded. For an action represents the core than was actually loaded.
        /// </summary>
        public IBlob CoreState { get; protected set; }

        /// <summary>
        /// For a load or save snapshot action, this indicates the id of the snapshot.
        /// </summary>
        public int SnapshotId { get; protected set; }

        private ManualResetEvent _processed;

        public void SetProcessed()
        {
            _processed.Set();
        }

        public bool Wait(int timeout)
        {
            return _processed.WaitOne(timeout);
        }

        public bool Wait()
        {
            return _processed.WaitOne();
        }   

        static public MachineRequest Reset()
        {
            return new MachineRequest(Types.Reset);
        }

        static public MachineRequest KeyPress(byte keycode, bool down)
        {
            MachineRequest request = new MachineRequest(Types.KeyPress)
            {
                KeyCode = keycode,
                KeyDown = down
            };

            return request;
        }

        static public MachineRequest RunUntil(UInt64 stopTicks)
        {
            MachineRequest request = new MachineRequest(Types.RunUntil)
            {
                StopTicks = stopTicks
            };

            return request;
        }

        static public MachineRequest LoadDisc(byte drive, byte[] buffer)
        {
            MachineRequest request = new MachineRequest(Types.LoadDisc)
            {
                Drive = drive,
                MediaBuffer = new MemoryBlob((byte[])buffer?.Clone())
            };

            return request;
        }

        static public MachineRequest LoadTape(byte[] buffer)
        {
            MachineRequest request = new MachineRequest(Types.LoadTape)
            {
                MediaBuffer = new MemoryBlob((byte[])buffer?.Clone())
            };

            return request;
        }

        static public MachineRequest CoreVersion(int version)
        {
            MachineRequest request = new MachineRequest(Types.CoreVersion)
            {
                Version = version
            };

            return request;
        }

        static public MachineRequest LoadCore(IBlob state)
        {
            MachineRequest request = new MachineRequest(Types.LoadCore)
            {
                CoreState = state
            };

            return request;
        }

        static public MachineRequest CreateSnapshot(Int32 parentSnapshotId)
        {
            MachineRequest request = new MachineRequest(Types.CreateSnapshot)
            {
                SnapshotId = parentSnapshotId
            };

            return request;
        }

        static public MachineRequest RevertToSnapshot(Int32 snapshotId)
        {
            MachineRequest request = new MachineRequest(Types.RevertToSnapshot)
            {
                SnapshotId = snapshotId
            };

            return request;
        }

        static public MachineRequest DeleteSnapshot(Int32 snapshotId)
        {
            MachineRequest request = new MachineRequest(Types.DeleteSnapshot)
            {
                SnapshotId = snapshotId
            };

            return request;
        }

        static public MachineRequest Pause()
        {
            MachineRequest request = new MachineRequest(Types.Pause);

            return request;
        }

        static public MachineRequest Resume()
        {
            MachineRequest request = new MachineRequest(Types.Resume);

            return request;
        }

        static public MachineRequest Reverse()
        {
            MachineRequest request = new MachineRequest(Types.Reverse);

            return request;
        }

        static public MachineRequest Lock()
        {
            MachineRequest request = new MachineRequest(Types.Lock);

            return request;
        }

        static public MachineRequest Unlock()
        {
            MachineRequest request = new MachineRequest(Types.Unlock);

            return request;
        }
    }
}
