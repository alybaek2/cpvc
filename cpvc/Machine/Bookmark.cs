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

        public int Version { get; set; }

        /// <summary>
        /// The state of the CPvC instance as created by <c>Core.GetState</c>.
        /// </summary>
        public IBlob State { get; set; }

        public IBlob Screen { get; set; }

        public Bookmark(bool system, int version, IBlob state, IBlob screen)
        {
            System = system;
            Version = version;
            State = state;
            Screen = screen;
        }

        public Bookmark(bool system, int version, byte[] state, byte[] screen)
        {
            System = system;
            Version = version;
            State = new MemoryBlob(state);
            Screen = new MemoryBlob(screen);
        }

        public Bookmark Clone()
        {
            return new Bookmark(System, Version, State, Screen);
        }
    }
}
