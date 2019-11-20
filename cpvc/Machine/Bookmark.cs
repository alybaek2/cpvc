using System;

namespace CPvC
{
    /// <summary>
    /// Represents a snapshot of a CPvC instance.
    /// </summary>
    public class Bookmark
    {
        /// <summary>
        /// The number of CPvC ticks that had elapsed when the bookmark was created.
        /// </summary>
        public UInt64 Ticks { get; }

        /// <summary>
        /// Indicates whether the bookmark was created by the system or the user.
        /// </summary>
        /// <remarks>
        /// A system-created bookmark is created when the CPvC instance is closed, in order for execution to continue where it left off the next time the CPvC instance is loaded.
        /// </remarks>
        public bool System { get; }

        /// <summary>
        /// The state of the CPvC instance as created by <c>Core.GetState</c>.
        /// </summary>
        public byte[] State { get; }

        public Bookmark(UInt64 ticks, bool system, byte[] state)
        {
            Ticks = ticks;
            System = system;
            State = state;
        }
    }
}
