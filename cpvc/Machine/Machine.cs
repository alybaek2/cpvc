using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CPvC
{
    // Should this also send the Machine, so the receiver can verify this event came from the right Machine?
    public delegate void MachineAuditorDelegate(CoreAction action);

    /// <summary>
    /// Represents a persistent instance of a CPvC machine.
    /// </summary>
    /// <remarks>
    /// The Machine class, in addition to encapsulating a running Core object, also maintains a file which contains the state of the machine.
    /// This allows a machine to be closed, and then resumed where it left off the next time it's opened.
    /// </remarks>
    public sealed class Machine : CoreMachine,
        ICoreMachine,
        IOpenableMachine,
        IInteractiveMachine,
        IBookmarkableMachine,
        IJumpableMachine,
        IPausableMachine,
        IReversibleMachine,
        ITurboableMachine,
        ICompactableMachine,
        IMachineFileReader,
        INotifyPropertyChanged,
        IDisposable
    {
        private string _name;

        public HistoryEvent CurrentEvent { get; private set; }
        public HistoryEvent RootEvent { get; private set; }
        private Dictionary<int, HistoryEvent> _historyEventById;

        private int _nextEventId;

        private readonly IFileSystem _fileSystem;
        private MachineFile _file;

        public Machine(string name, string machineFilepath, IFileSystem fileSystem)
        {
            _name = name;
            Filepath = machineFilepath;

            Display = new Display();

            _nextEventId = 0;
            _historyEventById = new Dictionary<int, HistoryEvent>();

            _fileSystem = fileSystem;
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        /// <summary>
        /// Opens an existing machine.
        /// </summary>
        /// <param name="name">The name of the machine. Not required if <c>lazy</c> is false.</param>
        /// <param name="machineFilepath">Filepath to the machine file.</param>
        /// <param name="fileSystem">File system interface.</param>
        /// <param name="lazy">If true, executes a minimal "lazy" load of the machine - only the <c>Name</c>, <c>Filepath</c>, and <c>Display.Bitmap</c> properties will be populated. A subsequent call to <c>Open</c> will be required to fully load the machine.</param>
        /// <returns>A Machine object in the same state it was in when it was previously closed, unless <c>lazy</c> is true..</returns>
        static public Machine Open(string name, string machineFilepath, IFileSystem fileSystem, bool lazy)
        {
            Machine machine = null;

            try
            {
                machine = new Machine(name, machineFilepath, fileSystem);

                machine.Open();
                if (lazy)
                {
                    machine.Close();
                }

                return machine;
            }
            catch (Exception)
            {
                machine.Dispose();

                throw;
            }
        }

        /// <summary>
        /// Fully opens a machine that was previously lazy-loaded.
        /// </summary>
        public void Open()
        {
            Core core = null;
            try
            {
                _file = new MachineFile(_fileSystem, Filepath);

                _file.ReadFile(this);

                if (CurrentEvent == null)
                {
                    // The machine file is either empty or has some other problem...
                    throw new Exception(String.Format("Unable to load file \"{0}\"; CurrentEvent is null!", Filepath));
                }

                // Rewind from the current event to the most recent one with a bookmark...
                HistoryEvent bookmarkEvent = CurrentEvent;
                while (bookmarkEvent != null && bookmarkEvent.Bookmark == null)
                {
                    bookmarkEvent = bookmarkEvent.Parent;
                }

                core = GetCore(bookmarkEvent?.Bookmark);

                CurrentEvent = bookmarkEvent ?? RootEvent;

                Display.EnableGreyscale(false);
                Display.GetFromBookmark(bookmarkEvent?.Bookmark);
                SetCore(core);

                if (bookmarkEvent?.Bookmark != null)
                {
                    Auditors?.Invoke(CoreAction.LoadCore(bookmarkEvent.Ticks, bookmarkEvent.Bookmark.State));
                }

                CoreAction action = CoreAction.CoreVersion(Core.Ticks, Core.LatestVersion);
                AddEvent(HistoryEvent.CreateCoreAction(NextEventId(), action), true);
            }
            catch (Exception)
            {
                core?.Dispose();
                Dispose();

                throw;
            }
        }

        /// <summary>
        /// Creates a new instance of a CPvC machine.
        /// </summary>
        /// <param name="name">Name of the new machine.</param>
        /// <param name="machineFilepath">Filepath of the new machine file.</param>
        /// <param name="fileSystem">File system interface.</param>
        /// <returns>A new instance of a CPvC machine.</returns>
        static public Machine New(string name, string machineFilepath, IFileSystem fileSystem)
        {
            Machine machine = null;
            try
            {
                machine = new Machine(null, machineFilepath, fileSystem);
                fileSystem.DeleteFile(machineFilepath);

                machine._file = new MachineFile(fileSystem, machine.Filepath);
                machine.Name = name;

                machine.SetCore(Core.Create(Core.LatestVersion, Core.Type.CPC6128));

                CoreAction action = CoreAction.CoreVersion(machine.Core.Ticks, Core.LatestVersion);
                machine.RootEvent = HistoryEvent.CreateCoreAction(machine.NextEventId(), action);
                machine._file.WriteHistoryEvent(machine.RootEvent);

                machine.CurrentEvent = machine.RootEvent;

                return machine;
            }
            catch (Exception)
            {
                machine.Dispose();

                throw;
            }
        }

        public bool CanClose()
        {
            return !RequiresOpen;
        }

        public void Close()
        {
            // No need to do this if we're in a "lazy-loaded" state.
            if (!RequiresOpen)
            {
                Stop();

                try
                {
                    // Create a system bookmark so the machine can resume from where it left off the next time it's loaded, but don't
                    // create one if we already have a system bookmark at the current event, or we're at the root event.
                    if ((CurrentEvent.Ticks != Ticks) ||
                        (CurrentEvent.Bookmark == null && CurrentEvent != RootEvent) ||
                        (CurrentEvent.Bookmark != null && !CurrentEvent.Bookmark.System))
                    {
                        AddCheckpointWithBookmarkEvent(true);
                    }
                }
                finally
                {
                    _file.Close();
                }
            }

            SetCore(null);

            CurrentEvent = null;
            RootEvent = null;

            Status = null;

            _nextEventId = 0;

            Display?.EnableGreyscale(true);
        }

        /// <summary>
        /// Delegate for logging core actions.
        /// </summary>
        /// <param name="core">The core the request was made for.</param>
        /// <param name="request">The original request.</param>
        /// <param name="action">The action taken.</param>
        private void RequestProcessed(Core core, CoreRequest request, CoreAction action)
        {
            if (core == _core && action != null)
            {
                Auditors?.Invoke(action);

                if (action.Type != CoreAction.Types.RunUntilForce && action.Type != CoreAction.Types.LoadSnapshot && action.Type != CoreAction.Types.SaveSnapshot)
                {
                    AddEvent(HistoryEvent.CreateCoreAction(NextEventId(), action), true);

                    switch (action.Type)
                    {
                        case CoreRequest.Types.LoadDisc:
                            Status = (action.MediaBuffer.GetBytes() != null) ? "Loaded disc" : "Ejected disc";
                            break;
                        case CoreRequest.Types.LoadTape:
                            Status = (action.MediaBuffer.GetBytes() != null) ? "Loaded tape" : "Ejected tape";
                            break;
                        case CoreRequest.Types.Reset:
                            Status = "Reset";
                            break;
                    }
                }
                else if (action.Type == CoreAction.Types.LoadSnapshot)
                {
                    // Ensure to update the display.
                    Display.CopyFromBufferAsync();

                    // Switch the CurrentEvent back if necessary...
                    HistoryEvent newCurrentEvent = CurrentEvent;
                    while (newCurrentEvent.Ticks > Ticks)
                    {
                        newCurrentEvent = newCurrentEvent.Parent;
                    }

                    if (CurrentEvent != newCurrentEvent)
                    {
                        _file.WriteCurrent(newCurrentEvent);
                        CurrentEvent = newCurrentEvent;
                    }
                }
            }
        }

        public bool RequiresOpen
        {
            get
            {
                return _core == null;
            }
        }

        private void SetCore(Core core)
        {
            if (Core == core)
            {
                return;
            }

            if (core == null)
            {
                Core.Auditors -= RequestProcessed;
                Core = null;
            }
            else
            {
                Core = core;
                Core.Auditors += RequestProcessed;
            }

            OnPropertyChanged("RequiresOpen");
        }

        /// <summary>
        /// The name of the machine.
        /// </summary>
        public override string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                _file.WriteName(_name);

                OnPropertyChanged("Name");
            }
        }

        public void Reset()
        {
            _core.Reset();
            Status = "Reset";
        }

        public void Key(byte keycode, bool down)
        {
            _core.KeyPress(keycode, down);
        }

        public void AddBookmark(bool system)
        {
            using (AutoPause())
            {
                HistoryEvent historyEvent = AddCheckpointWithBookmarkEvent(system);

                Diagnostics.Trace("Created bookmark at tick {0}", _core.Ticks);
                Status = String.Format("Bookmark added at {0}", Helpers.GetTimeSpanFromTicks(Core.Ticks).ToString(@"hh\:mm\:ss"));
            }
        }

        /// <summary>
        /// Rewind from the current event in the timeline to the most recent bookmark, and begin a new timeline branch from there.
        /// </summary>
        public void JumpToMostRecentBookmark()
        {
            HistoryEvent lastBookmarkEvent = CurrentEvent;
            while (lastBookmarkEvent != null)
            {
                if (lastBookmarkEvent.Type == HistoryEvent.Types.Checkpoint && lastBookmarkEvent.Bookmark != null && !lastBookmarkEvent.Bookmark.System && lastBookmarkEvent.Ticks != Core.Ticks)
                {
                    TimeSpan before = Helpers.GetTimeSpanFromTicks(Core.Ticks);
                    JumpToBookmark(lastBookmarkEvent);
                    TimeSpan after = Helpers.GetTimeSpanFromTicks(Core.Ticks);
                    Status = String.Format("Rewound to {0} (-{1})", after.ToString(@"hh\:mm\:ss"), (after - before).ToString(@"hh\:mm\:ss"));
                    return;
                }

                lastBookmarkEvent = lastBookmarkEvent.Parent;
            }

            // No bookmarks? Go all the way back to the root!
            JumpToBookmark(RootEvent);
            Status = "Rewound to start";
        }

        public void LoadDisc(byte drive, byte[] diskBuffer)
        {
            _core.LoadDisc(drive, diskBuffer);
        }

        public void LoadTape(byte[] tapeBuffer)
        {
            _core.LoadTape(tapeBuffer);
        }

        /// <summary>
        /// Changes the current position in the timeline.
        /// </summary>
        /// <param name="bookmarkEvent">The event to become the current event in the timeline. This event must have a bookmark.</param>
        public void JumpToBookmark(HistoryEvent bookmarkEvent)
        {
            // Add a checkpoint at the current position to properly mark the end of this branch...
            Core.Stop();
            SetCheckpoint();
            byte volume = Core.Volume;

            Display.GetFromBookmark(bookmarkEvent.Bookmark);
            SetCore(Machine.GetCore(bookmarkEvent.Bookmark));
            Core.Volume = volume;

            if (bookmarkEvent.Bookmark != null)
            {
                Auditors?.Invoke(CoreAction.LoadCore(bookmarkEvent.Ticks, bookmarkEvent.Bookmark.State));
            }
            else
            {
                Auditors?.Invoke(CoreAction.Reset(bookmarkEvent.Ticks));
            }

            _file.WriteCurrent(bookmarkEvent);
            CurrentEvent = bookmarkEvent;

            SetCoreRunning();
        }

        /// <summary>
        /// Deletes an event (and all its children) without changing the current event.
        /// </summary>
        /// <param name="historyEvent">Event to delete.</param>
        /// <param name="loading">Indicates whether the MachineFile is being loaded from a file.</param>
        private void DeleteEvent(HistoryEvent historyEvent, bool writeToFile)
        {
            if (historyEvent.Parent == null)
            {
                return;
            }

            using (AutoPause())
            {
                historyEvent.Parent.RemoveChild(historyEvent);

                // Remove the event and all its descendents from the lookup.
                foreach (HistoryEvent e in historyEvent.GetSelfAndDescendents())
                {
                    _historyEventById.Remove(e.Id);
                }

                if (writeToFile)
                {
                    _file.WriteDelete(historyEvent);
                }
            }
        }

        /// <summary>
        /// Removes a branch of the timeline.
        /// </summary>
        /// <param name="historyEvent">HistoryEvent object which belongs to the branch to be removed.</param>
        public void TrimTimeline(HistoryEvent historyEvent)
        {
            if (historyEvent == null || historyEvent.Children.Count != 0 || historyEvent == CurrentEvent || historyEvent.Parent == null)
            {
                return;
            }

            // Set a checkpoint. This is needed to ensure that walking up the tree stops at the correct node.
            // For example, if CurrentEvent has one child, and the current ticks is greater than CurrentEvent.Ticks,
            // then if TrimTimeline is called with that child, then we want to delete only that child. Without
            // setting a checkpoint, the following code will walk all the way up the tree to the nearest event with
            // more than one child, which isn't what we want.
            SetCheckpoint();

            // Walk up the tree to find the node to be removed...
            HistoryEvent parent = historyEvent.Parent;
            HistoryEvent child = historyEvent;
            while (parent != null && parent.Children.Count == 1)
            {
                child = parent;
                parent = parent.Parent;
            }

            if (parent != null)
            {
                DeleteEvent(child, true);
            }
        }

        public void SetCheckpoint()
        {
            using (AutoPause())
            {
                AddEvent(HistoryEvent.CreateCheckpoint(NextEventId(), _core.Ticks, DateTime.UtcNow, null), true);
            }
        }

        /// <summary>
        /// Rewrites the machine file according to the current in-memory representation of the timeline.
        /// </summary>
        /// <param name="diffsEnabled">Enable calculation of diffs. Setting this to true can cause compacting to take a long time.</param>
        /// <remarks>
        /// This is useful for compacting the size of the machine file, due to the fact that bookmark and timeline deletions don't actually
        /// remove anything from the machine file, but simply log the fact they happened.
        /// </remarks>
        /// 
        public void Compact(bool diffsEnabled)
        {
            using (AutoPause())
            {
                string tempname = Filepath + ".new";

                MachineFile tempfile = null;
                try
                {
                    tempfile = new MachineFile(_fileSystem, tempname);
                    tempfile.DiffsEnabled = diffsEnabled;

                    tempfile.WriteName(_name);
                    WriteEvent(tempfile, RootEvent);
                    tempfile.WriteCurrent(CurrentEvent);
                }
                finally
                {
                    tempfile?.Close();
                }

                _file.Close();

                Int64 newLength = _fileSystem.FileLength(tempname);
                Int64 oldLength = _fileSystem.FileLength(Filepath);

                _fileSystem.ReplaceFile(Filepath, tempname);

                RootEvent = null;
                CurrentEvent = null;
                _historyEventById.Clear();

                _file = new MachineFile(_fileSystem, Filepath);
                _file.ReadFile(this);

                Status = String.Format("Compacted machine file by {0}%", (Int64)(100 * ((double)(oldLength - newLength)) / ((double)oldLength)));
            }
        }

        /// <summary>
        /// Sets a bookmark for a given HistoryEvent object.
        /// </summary>
        /// <param name="historyEvent">The history event whose bookmark is to be set.</param>
        /// <param name="bookmark">The bookmark to be set, or null to remove an existing bookmark.</param>
        public void SetBookmark(HistoryEvent historyEvent, Bookmark bookmark)
        {
            if (historyEvent.Type != HistoryEvent.Types.Checkpoint)
            {
                return;
            }

            historyEvent.Bookmark = bookmark;

            _file.WriteBookmark(historyEvent.Id, bookmark);
        }

        private HistoryEvent AddCheckpointWithBookmarkEvent(bool system)
        {
            Bookmark bookmark = GetBookmark(system);
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(NextEventId(), _core.Ticks, DateTime.UtcNow, bookmark);
            AddEvent(historyEvent, true);

            return historyEvent;
        }

        /// <summary>
        /// Adds a historical event to the current event, and makes that event the new current event.
        /// </summary>
        /// <param name="historyEvent">The History event to be added.</param>
        /// <param name="writeToFile">Indicates whether this event should be written to the machine file. Should be false only when loading a machine.</param>
        private void AddEvent(HistoryEvent historyEvent, bool writeToFile)
        {
            if (RootEvent == null)
            {
                RootEvent = historyEvent;
                _historyEventById[historyEvent.Id] = historyEvent;
            }
            else
            {
                CurrentEvent.AddChild(historyEvent);
                _historyEventById[historyEvent.Id] = historyEvent;
            }

            if (writeToFile)
            {
                _file.WriteHistoryEvent(historyEvent);
            }

            CurrentEvent = historyEvent;
        }

        /// <summary>
        /// Create a bookmark at the current event.
        /// </summary>
        /// <param name="system">Indicates if this bookmark is to be marked as a system bookmark.</param>
        /// <returns>A bookmark of the current state of the machine.</returns>
        private Bookmark GetBookmark(bool system)
        {
            using (AutoPause())
            {
                byte[] screen = Display.GetBitmapBytes();
                byte[] core = _core.GetState();

                return new Bookmark(system, _core.Version, core, screen);
            }
        }

        /// <summary>
        /// Writes a history event, and its children, to a machine file. Used only by <c>RewriteMachineFile</c>.
        /// </summary>
        /// <param name="file">Machine file to write to.</param>
        /// <param name="historyEvent">History event to write.</param>
        private void WriteEvent(MachineFile file, HistoryEvent historyEvent)
        {
            // As the history tree could be very deep, keep a "stack" of history events in order to avoid recursive calls.
            List<HistoryEvent> historyEvents = new List<HistoryEvent>
            {
                historyEvent
            };

            HistoryEvent previousEvent = null;
            while (historyEvents.Count > 0)
            {
                HistoryEvent currentEvent = historyEvents[0];

                if (previousEvent != currentEvent.Parent && previousEvent != null)
                {
                    file.WriteCurrent(currentEvent.Parent);
                }

                // Don't write out non-root checkpoint nodes which have only one child and no bookmark.
                if (currentEvent == RootEvent || currentEvent.Children.Count != 1 || currentEvent.Type != HistoryEvent.Types.Checkpoint || currentEvent.Bookmark != null)
                {
                    file.WriteHistoryEvent(currentEvent);
                }

                historyEvents.RemoveAt(0);
                previousEvent = currentEvent;

                // Place the current event's children at the top of the "stack". This effectively means we're doing a depth-first traversion of the history tree.
                historyEvents.InsertRange(0, currentEvent.Children);
            }
        }

        /// <summary>
        /// Returns a new core based on a HistoryEvent.
        /// </summary>
        /// <param name="bookmark">Bookmark to create the core from. If null, then a new core is created.</param>
        /// <returns>If <c>bookmark</c> is not null, returns a core based on that bookmark. If the HistoryEvent is null, a newly-instantiated core is returned.</returns>
        static private Core GetCore(Bookmark bookmark)
        {
            if (bookmark != null)
            {
                Core core = Core.Create(Core.LatestVersion, bookmark.State.GetBytes());

                // Ensure all keys are in an "up" state.
                for (byte keycode = 0; keycode < 80; keycode++)
                {
                    core.KeyPress(keycode, false);
                }

                return core;
            }

            return Core.Create(Core.LatestVersion, Core.Type.CPC6128);
        }

        private int NextEventId()
        {
            return _nextEventId++;
        }

        // IMachineFileReader implementation.
        public void SetName(string name)
        {
            _name = name;
        }

        public void DeleteEvent(int id)
        {
            if (_historyEventById.TryGetValue(id, out HistoryEvent historyEvent) && historyEvent != null)
            {
                DeleteEvent(historyEvent, false);
            }
        }

        public void SetBookmark(int id, Bookmark bookmark)
        {
            if (_historyEventById.TryGetValue(id, out HistoryEvent historyEvent) && historyEvent != null)
            {
                historyEvent.Bookmark = bookmark;
            }
        }

        public void SetCurrentEvent(int id)
        {
            if (_historyEventById.TryGetValue(id, out HistoryEvent historyEvent) && historyEvent != null)
            {
                CurrentEvent = historyEvent;
            }
        }

        public void AddHistoryEvent(HistoryEvent historyEvent)
        {
            AddEvent(historyEvent, false);

            _nextEventId = Math.Max(_nextEventId, historyEvent.Id + 1);
        }

        public void Reverse()
        {
            if (_core.RunningState == RunningState.Reverse)
            {
                return;
            }

            SetCheckpoint();

            _runningState = RunningState.Reverse;
            SetCoreRunning();
            Status = "Reversing";
        }
    }
}
