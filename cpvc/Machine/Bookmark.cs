namespace CPvC
{
    /// <summary>
    /// Represents a snapshot of a CPvC instance.
    /// </summary>
    public class Bookmark
    {
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

        public Bookmark(bool system, byte[] state)
        {
            System = system;
            State = state;
        }
    }
}
