using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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

        public MachineHistory History
        {
            get
            {
                return _history;
            }
        }

        private RunningState _previousRunningState;

        private readonly IFileSystem _fileSystem;
        private MachineFile _file;
        private MachineHistory _history;

        private const int _snapshotLimit = 500;
        private List<SnapshotInfo> _snapshots;
        private int _lastTakenSnapshotId = -1;
        private List<SnapshotInfo> _newSnapshots;

        public Machine(string name, string machineFilepath, IFileSystem fileSystem)
        {
            _name = name;
            Filepath = machineFilepath;

            Display = new Display();

            _previousRunningState = RunningState.Paused;

            _fileSystem = fileSystem;

            _snapshots = new List<SnapshotInfo>();
            _newSnapshots = new List<SnapshotInfo>();

            _history = new MachineHistory();
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        private class SnapshotInfo
        {
            public SnapshotInfo(int id)
            {
                Id = id;
                AudioBuffer = new AudioBuffer();
            }

            public int Id { get; }

            public AudioBuffer AudioBuffer { get; }
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

                if (_history.CurrentEvent == null)
                {
                    // The machine file is either empty or has some other problem...
                    throw new Exception(String.Format("Unable to load file \"{0}\"; CurrentEvent is null!", Filepath));
                }

                // Rewind from the current event to the most recent one with a bookmark...
                HistoryEvent bookmarkEvent = _history.CurrentEvent;
                while (bookmarkEvent != null && bookmarkEvent.Bookmark == null)
                {
                    bookmarkEvent = bookmarkEvent.Parent;
                }

                core = GetCore(bookmarkEvent?.Bookmark);

                _history.CurrentEvent = bookmarkEvent ?? _history.RootEvent;

                Display.EnableGreyscale(false);
                Display.GetFromBookmark(bookmarkEvent?.Bookmark);
                SetCore(core);

                if (bookmarkEvent?.Bookmark != null)
                {
                    Auditors?.Invoke(CoreAction.LoadCore(bookmarkEvent.Ticks, bookmarkEvent.Bookmark.State));
                }

                CoreAction action = CoreAction.CoreVersion(Core.Ticks, Core.LatestVersion);
                AddEvent(HistoryEvent.CreateCoreAction(_history.NextEventId(), action), true);
            }
            catch (Exception)
            {
                core?.Dispose();
                Dispose();

                throw;
            }
        }

        public void AddEvent(HistoryEvent historyEvent, bool writeToFile)
        {
            _history.AddEvent(historyEvent);

            if (writeToFile)
            {
                _file.WriteHistoryEvent(historyEvent);
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

                Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
                core.IdleRequest = machine.IdleRequest;
                machine.SetCore(core);

                CoreAction action = CoreAction.CoreVersion(machine.Core.Ticks, Core.LatestVersion);
                machine._history.RootEvent = HistoryEvent.CreateCoreAction(machine._history.NextEventId(), action);
                machine._file.WriteHistoryEvent(machine._history.RootEvent);

                machine._history.CurrentEvent = machine._history.RootEvent;

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
                    if ((_history.CurrentEvent.Ticks != Ticks) ||
                        (_history.CurrentEvent.Bookmark == null && _history.CurrentEvent != _history.RootEvent) ||
                        (_history.CurrentEvent.Bookmark != null && !_history.CurrentEvent.Bookmark.System))
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

            _history.CurrentEvent = null;
            _history.RootEvent = null;

            Status = null;

            Display?.EnableGreyscale(true);
        }

        /// <summary>
        /// Delegate for VSync events.
        /// </summary>
        /// <param name="core">Core whose VSync signal went from low to high.</param>
        protected override void BeginVSync(Core core)
        {
            TakeSnapshot();

            base.BeginVSync(core);
        }

        public override int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (_runningStateLock)
            {
                // Drive Reverse mode from here...
                if (RunningState == RunningState.Reverse)
                {
                    return ReverseReadAudio(buffer, offset, samplesRequested);
                }

                return base.ReadAudio(buffer, offset, samplesRequested);
            }
        }

        private int ReverseReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            int totalSamplesWritten = 0;
            int currentSamplesRequested = samplesRequested;

            SnapshotInfo currentSnapshot = _newSnapshots.LastOrDefault();
            while (totalSamplesWritten < samplesRequested && currentSnapshot != null)
            {
                int samplesWritten = currentSnapshot.AudioBuffer.Render16BitStereo(Volume, buffer, offset, currentSamplesRequested, true);
                if (samplesWritten == 0)
                {
                    _core.PushRequest(CoreRequest.RevertToSnapshot(currentSnapshot.Id));

                    // Ensure all keys are "up" once we come out of Reverse mode.
                    _core.AllKeysUp();

                    if (_newSnapshots.Count <= 1)
                    {
                        // We've reached the last snapshot.
                        break;
                    }

                    _core.PushRequest(CoreRequest.DeleteSnapshot(currentSnapshot.Id));
                    _newSnapshots.RemoveAt(_newSnapshots.Count - 1);
                    currentSnapshot = _newSnapshots.LastOrDefault();
                }

                totalSamplesWritten += samplesWritten;
                currentSamplesRequested -= samplesWritten;
                offset += 4 * samplesWritten;
            }

            return totalSamplesWritten;
        }

        private void TakeSnapshot()
        {
            _core.PushRequest(CoreRequest.CreateSnapshot(_lastTakenSnapshotId));
        }

        /// <summary>
        /// Delegate for logging core actions.
        /// </summary>
        /// <param name="core">The core the request was made for.</param>
        /// <param name="request">The original request.</param>
        /// <param name="action">The action taken.</param>
        private void RequestProcessed(Core core, CoreRequest request, CoreAction action)
        {
            if (core == _core)
            {
                if (action != null)
                {
                    Auditors?.Invoke(action);

                    if (action.Type != CoreAction.Types.RunUntil &&
                        action.Type != CoreAction.Types.CreateSnapshot &&
                        action.Type != CoreAction.Types.DeleteSnapshot &&
                        action.Type != CoreAction.Types.RevertToSnapshot)
                    {
                        AddEvent(HistoryEvent.CreateCoreAction(_history.NextEventId(), action), true);

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
                    else if (action.Type == CoreAction.Types.RunUntil)
                    {
                        SnapshotInfo snapshot = _snapshots.LastOrDefault();
                        if (snapshot != null && action.AudioSamples != null)
                        {
                            foreach (UInt16 sample in action.AudioSamples)
                            {
                                snapshot.AudioBuffer.Write(sample);
                            }
                        }

                        SnapshotInfo newSnapshot = _newSnapshots.LastOrDefault();
                        if (newSnapshot != null && action.AudioSamples != null)
                        {
                            foreach (UInt16 sample in action.AudioSamples)
                            {
                                newSnapshot.AudioBuffer.Write(sample);
                            }
                        }
                    }
                    else if (action.Type == CoreAction.Types.RevertToSnapshot)
                    {
                        Display.CopyFromBufferAsync();
                    }
                    else if (action.Type == CoreAction.Types.CreateSnapshot)
                    {
                        _lastTakenSnapshotId = action.SnapshotId;
                        SnapshotInfo newSnapshot = new SnapshotInfo(action.SnapshotId);
                        _newSnapshots.Add(newSnapshot);

                        if (_newSnapshots.Count > 500)
                        {
                            SnapshotInfo snapshot = _newSnapshots[0];
                            _newSnapshots.RemoveAt(0);
                            _core.PushRequest(CoreRequest.DeleteSnapshot(snapshot.Id));
                        }
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
            HistoryEvent lastBookmarkEvent = _history.CurrentEvent;
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
            JumpToBookmark(_history.RootEvent);
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

            Display.GetFromBookmark(bookmarkEvent.Bookmark);
            SetCore(GetCore(bookmarkEvent.Bookmark));

            if (bookmarkEvent.Bookmark != null)
            {
                Auditors?.Invoke(CoreAction.LoadCore(bookmarkEvent.Ticks, bookmarkEvent.Bookmark.State));
            }
            else
            {
                Auditors?.Invoke(CoreAction.Reset(bookmarkEvent.Ticks));
            }

            _file.WriteCurrent(bookmarkEvent);
            _history.CurrentEvent = bookmarkEvent;

            SetCoreRunning();
        }

        /// <summary>
        /// Deletes an event (and all its children) without changing the current event.
        /// </summary>
        /// <param name="historyEvent">Event to delete.</param>
        /// <param name="loading">Indicates whether the MachineFile is being loaded from a file.</param>
        private void DeleteEvent(HistoryEvent historyEvent, bool writeToFile)
        {
            using (AutoPause())
            {
                if (!_history.DeleteEvent(historyEvent))
                {
                    return;
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
            if (historyEvent == null || historyEvent.Children.Count != 0 || historyEvent == _history.CurrentEvent || historyEvent.Parent == null)
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
                AddEvent(HistoryEvent.CreateCheckpoint(_history.NextEventId(), _core.Ticks, DateTime.UtcNow, null), true);
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
                    WriteEvent(tempfile, _history.RootEvent);
                    tempfile.WriteCurrent(_history.CurrentEvent);
                }
                finally
                {
                    tempfile?.Close();
                }

                _file.Close();

                Int64 newLength = _fileSystem.FileLength(tempname);
                Int64 oldLength = _fileSystem.FileLength(Filepath);

                _fileSystem.ReplaceFile(Filepath, tempname);

                _history.RootEvent = null;
                _history.CurrentEvent = null;

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
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(_history.NextEventId(), _core.Ticks, DateTime.UtcNow, bookmark);
            AddEvent(historyEvent, true);

            return historyEvent;
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
                if (currentEvent == _history.RootEvent || currentEvent.Children.Count != 1 || currentEvent.Type != HistoryEvent.Types.Checkpoint || currentEvent.Bookmark != null)
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
        private Core GetCore(Bookmark bookmark)
        {
            Core core = null;
            if (bookmark != null)
            {
                core = Core.Create(Core.LatestVersion, bookmark.State.GetBytes());
                core.IdleRequest = IdleRequest;

                core.AllKeysUp();

                return core;
            }

            core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            core.IdleRequest = IdleRequest;

            return core;
        }

        private CoreRequest IdleRequest()
        {
            return CoreRequest.RunUntil(Ticks + 1000);
        }

        // IMachineFileReader implementation.
        public void SetName(string name)
        {
            _name = name;
        }

        public void DeleteEvent(int id)
        {
            _history.DeleteEvent(id);
        }

        public void SetBookmark(int id, Bookmark bookmark)
        {
            _history.SetBookmark(id, bookmark);
        }

        public void SetCurrentEvent(int id)
        {
            _history.SetCurrentEvent(id);
        }

        public void AddHistoryEvent(HistoryEvent historyEvent)
        {
            AddEvent(historyEvent, false);
        }

        public void Reverse()
        {
            if (_runningState == RunningState.Reverse || _newSnapshots.LastOrDefault() == null)
            {
                return;
            }

            SetCheckpoint();

            _previousRunningState = SetRunningState(RunningState.Reverse);

            Status = "Reversing";
        }

        public void ReverseStop()
        {
            SetCheckpoint();

            SetRunningState(_previousRunningState);
        }
    }
}
