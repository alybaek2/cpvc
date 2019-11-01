using System;

namespace CPvC
{
    /// <summary>
    /// Base class containing common code for CoreAction and CoreRequest. Probably should find a better name than "CoreActionBase".
    /// </summary>
    public class CoreActionBase
    {
        public enum Types
        {
            Reset,
            KeyPress,
            LoadDisc,
            LoadTape,
            RunUntil
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
        public byte[] MediaBuffer { get; protected set; }

        /// <summary>
        /// For a request, indicates the desired ticks to stop at. For an action represents the actual ticks value that the core stopped at.
        /// </summary>
        public UInt64 StopTicks { get; protected set; }

        /// <summary>
        /// For a request, a bitmask indicating the reasons that the core should running before reaching StopTicks. For an action, this is a bitmask indicating the actual reason why the core stopped.
        /// </summary>
        public byte StopReason { get; protected set; }

        public CoreActionBase(Types type)
        {
            Type = type;
        }
    }
}
