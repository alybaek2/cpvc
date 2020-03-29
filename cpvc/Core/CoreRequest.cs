using System;

namespace CPvC
{
    /// <summary>
    /// Represents a request to the core thread.
    /// </summary>
    public class CoreRequest
    {
        public CoreRequest(Types type)
        {
            Type = type;
        }

        public enum Types
        {
            Reset,
            KeyPress,
            LoadDisc,
            LoadTape,
            CoreVersion,
            RunUntilForce,
            LoadCore,
            Quit
        }

        public Types Type { get; protected set; }

        public UInt64 ExpectedExecutionTime { get; set; }

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
        public UInt64 StopTicks { get; protected set; }

        /// <summary>
        /// For an action, this indicates the version of the core to switch to.
        /// </summary>
        public int Version { get; protected set; }

        /// <summary>
        /// For a request, this indicates the Core to be loaded. For an action represents the core than was actually loaded.
        /// </summary>
        public IBlob CoreState { get; protected set; }

        static public CoreRequest Reset()
        {
            return new CoreRequest(Types.Reset);
        }

        static public CoreRequest KeyPress(byte keycode, bool down)
        {
            CoreRequest request = new CoreRequest(Types.KeyPress)
            {
                KeyCode = keycode,
                KeyDown = down
            };

            return request;
        }

        static public CoreRequest RunUntilForce(UInt64 stopTicks)
        {
            CoreRequest request = new CoreRequest(Types.RunUntilForce)
            {
                StopTicks = stopTicks
            };

            return request;
        }

        static public CoreRequest LoadDisc(byte drive, byte[] buffer)
        {
            CoreRequest request = new CoreRequest(Types.LoadDisc)
            {
                Drive = drive,
                MediaBuffer = new MemoryBlob((byte[])buffer?.Clone())
            };

            return request;
        }

        static public CoreRequest LoadTape(byte[] buffer)
        {
            CoreRequest request = new CoreRequest(Types.LoadTape)
            {
                MediaBuffer = new MemoryBlob((byte[])buffer?.Clone())
            };

            return request;
        }

        static public CoreRequest CoreVersion(int version)
        {
            CoreRequest request = new CoreRequest(Types.CoreVersion)
            {
                Version = version
            };

            return request;
        }

        static public CoreRequest LoadCore(IBlob state)
        {
            CoreRequest request = new CoreRequest(Types.LoadCore)
            {
                CoreState = state
            };

            return request;
        }

        static public CoreRequest Quit()
        {
            return new CoreRequest(Types.Quit);
        }
    }
}
