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
    /// The LocalMachine class, in addition to encapsulating a running Core object, also maintains a file which contains the state of the machine.
    /// This allows a machine to be closed, and then resumed where it left off the next time it's opened.
    /// </remarks>
    public sealed class LocalMachine : Machine,
        IMachine,
        IInteractiveMachine,
        IBookmarkableMachine,
        IJumpableMachine,
        IPausableMachine,
        IReversibleMachine,
        ITurboableMachine,
        ICompactableMachine,
        IPersistableMachine,
        INotifyPropertyChanged,
        IDisposable
    {
        private string _name;

        public History History
        {
            get
            {
                return _history;
            }
        }

        private RunningState _previousRunningState;

        private History _history;

        private int _snapshotLimit = 3000;
        private int _lastTakenSnapshotId = -1;
        private List<SnapshotInfo> _snapshots;

        private MachineFileWriter _file;

        private MachineFileWriter File
        {
            get
            {
                return _file;
            }

            set
            {
                if (_file != value)
                {
                    if (_file != null)
                    {
                        _file.Machine = null;
                    }

                    _file = value;

                    if (_file != null)
                    {
                        _file.Machine = this;
                    }

                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsOpen));
                }
            }
        }

        private string _filepath;

        public int SnapshotLimit
        {
            get
            {
                return _snapshotLimit;
            }

            set
            {
                if (_snapshotLimit != value)
                {
                    _snapshotLimit = value;
                    OnPropertyChanged();
                }
            }
        }

        public LocalMachine(string name, History history)
        {
            _name = name;

            Display = new Display();

            _previousRunningState = RunningState.Paused;

            _snapshots = new List<SnapshotInfo>();

            _history = history;
        }

        public void Dispose()
        {
            Close();
        }

        public bool CanStart
        {
            get
            {
                return IsOpen && RunningState == RunningState.Paused;
            }
        }

        public bool CanStop
        {
            get
            {
                return IsOpen && RunningState == RunningState.Running;
            }
        }

        private class SnapshotInfo
        {
            public SnapshotInfo(int id, HistoryEvent historyEvent)
            {
                Id = id;
                AudioBuffer = new AudioBuffer(-1);
                HistoryEvent = historyEvent;
            }

            public int Id { get; }

            public AudioBuffer AudioBuffer { get; }

            public HistoryEvent HistoryEvent { get; }
        }

        static public LocalMachine New(string name, string persistentFilepath)
        {
            return New(name, new History(), persistentFilepath);
        }

        static private LocalMachine New(string name, History history, string persistentFilepath)
        {
            LocalMachine machine = new LocalMachine(name, history);
            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            machine.SetCore(core);

            machine.PersistantFilepath = persistentFilepath;

            return machine;
        }

        public void Close()
        {
            lock (_runningStateLock)
            {
                if (IsOpen)
                {
                    Stop();

                    try
                    {
                        // Create a system bookmark so the machine can resume from where it left off the next time it's loaded, but don't
                        // create one if we already have a system bookmark at the current event, or we're at the root event.
                        BookmarkHistoryEvent currentBookmarkEvent = _history.CurrentEvent as BookmarkHistoryEvent;
                        if (currentBookmarkEvent == null ||
                            (currentBookmarkEvent.Ticks != Ticks) ||
                            (currentBookmarkEvent.Bookmark == null && currentBookmarkEvent != _history.RootEvent) ||
                            (currentBookmarkEvent.Bookmark != null && !currentBookmarkEvent.Bookmark.System))
                        {
                            AddBookmark(true);
                        }
                    }
                    finally
                    {
                    }
                }

                SetCore(null);

                if (File != null)
                {
                    File.Dispose();
                    File = null;
                }

                _history = new History();

                Status = null;

                Display?.EnableGreyscale(true);
            }
        }

        public bool CanClose
        {
            get
            {
                return IsOpen;
            }
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

            lock (_snapshots)
            {
                SnapshotInfo currentSnapshot = _snapshots.LastOrDefault();
                while (totalSamplesWritten < samplesRequested && currentSnapshot != null)
                {
                    int samplesWritten = currentSnapshot.AudioBuffer.Render16BitStereo(Volume, buffer, offset, currentSamplesRequested, true);
                    if (samplesWritten == 0)
                    {
                        _core.PushRequest(CoreRequest.RevertToSnapshot(currentSnapshot.Id, currentSnapshot));
                        _core.PushRequest(CoreRequest.DeleteSnapshot(currentSnapshot.Id));
                        _snapshots.RemoveAt(_snapshots.Count - 1);
                        currentSnapshot = _snapshots.LastOrDefault();
                    }

                    totalSamplesWritten += samplesWritten;
                    currentSamplesRequested -= samplesWritten;
                    offset += 4 * samplesWritten;
                }
            }

            return totalSamplesWritten;
        }

        private void TakeSnapshot()
        {
            if (SnapshotLimit > 0)
            {
                int snapshotId = ++_lastTakenSnapshotId;
                _core.PushRequest(CoreRequest.CreateSnapshot(snapshotId));
            }
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

                    if (action.Type != CoreAction.Types.CreateSnapshot &&
                        action.Type != CoreAction.Types.DeleteSnapshot &&
                        action.Type != CoreAction.Types.RevertToSnapshot)
                    {
                        HistoryEvent e = _history.AddCoreAction(action);

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

                    if (action.Type == CoreAction.Types.RunUntil)
                    {
                        lock (_snapshots)
                        {
                            SnapshotInfo newSnapshot = _snapshots.LastOrDefault();
                            if (newSnapshot != null && action.AudioSamples != null)
                            {
                                newSnapshot.AudioBuffer.Write(action.AudioSamples);
                            }
                        }
                    }
                    else if (action.Type == CoreAction.Types.RevertToSnapshot)
                    {
                        HistoryEvent historyEvent = ((SnapshotInfo)request.UserData).HistoryEvent;
                        if (_history.CurrentEvent != historyEvent)
                        {
                            _history.CurrentEvent = historyEvent;
                        }

                        Display.CopyScreenAsync();
                    }
                    else if (action.Type == CoreAction.Types.CreateSnapshot)
                    {
                        lock (_snapshots)
                        {
                            // Figure out what history event should be set as current if we revert to this snapshot.
                            // If the current event is a RunUntil, it may not be "finalized" yet (i.e. it may still
                            // be updated), so go with its parent.
                            HistoryEvent historyEvent = _history.CurrentEvent;
                            CoreActionHistoryEvent coreActionEvent = historyEvent as CoreActionHistoryEvent;
                            if (coreActionEvent != null && coreActionEvent.CoreAction.Type == CoreRequest.Types.RunUntil)
                            {
                                historyEvent = historyEvent.Parent;
                            }

                            SnapshotInfo newSnapshot = new SnapshotInfo(action.SnapshotId, historyEvent);
                            _snapshots.Add(newSnapshot);

                            while (_snapshots.Count > _snapshotLimit)
                            {
                                SnapshotInfo snapshot = _snapshots[0];
                                _snapshots.RemoveAt(0);
                                _core.PushRequest(CoreRequest.DeleteSnapshot(snapshot.Id));
                            }
                        }
                    }
                }
            }
        }

        public void SetCore(Core core)
        {
            if (Core == core)
            {
                return;
            }

            if (Core != null)
            {
                Core.Auditors -= RequestProcessed;
                Core.IdleRequest = null;
            }

            Core = core;

            if (Core != null)
            {
                Core.Auditors += RequestProcessed;
                Core.IdleRequest = IdleRequest;
            }
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
                if (_name != value)
                {
                    _name = value;

                    OnPropertyChanged();
                }
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
                Bookmark bookmark = GetBookmark(system);
                HistoryEvent historyEvent = _history.AddBookmark(_core.Ticks, bookmark);

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
                if (lastBookmarkEvent is BookmarkHistoryEvent b && !b.Bookmark.System && b.Ticks != Core.Ticks)
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
            using (AutoPause())
            {
                SetCurrentEvent(bookmarkEvent);
            }
        }

        /// <summary>
        /// Deletes a branch of the history.
        /// </summary>
        /// <param name="historyEvent">History event to be deleted, along with all its descendents.</param>
        public bool DeleteBranch(HistoryEvent historyEvent)
        {
            using (AutoPause())
            {
                return _history.DeleteBranch(historyEvent);
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
        public void Compact(IFileSystem fileSystem)
        {
            // Only allow closed machines to compact!
            if (!CanCompact())
            {
                throw new Exception("Can't compact an open machine!");
            }

            string oldFilepath = PersistantFilepath;
            string newFilepath = oldFilepath + ".tmp";

            MachineFileReader reader = new MachineFileReader();
            using (ITextFile textFile = fileSystem.OpenTextFile(oldFilepath))
            {
                reader.ReadFile(textFile);
            }

            using (ITextFile textFile = fileSystem.OpenTextFile(newFilepath))
            using (MachineFileWriter writer = new MachineFileWriter(textFile, reader.History))
            {
                writer.WriteHistory(reader.Name);
            }

            fileSystem.ReplaceFile(oldFilepath, newFilepath);
        }

        public bool CanCompact()
        {
            return PersistantFilepath != null && File == null;
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

        public CoreRequest IdleRequest()
        {
            return (RunningState == RunningState.Running) ? CoreRequest.RunUntil(Ticks + 1000) : null;
        }

        public bool DeleteBookmark(HistoryEvent e)
        {
            return _history.DeleteBookmark(e);
        }

        private void SetCurrentEvent(HistoryEvent historyEvent)
        {
            if (historyEvent is BookmarkHistoryEvent bookmarkHistoryEvent)
            {
                Core core = Core.Create(Core.LatestVersion, bookmarkHistoryEvent.Bookmark.State.GetBytes());
                SetCore(core);

                Display.GetFromBookmark(bookmarkHistoryEvent.Bookmark);

                Auditors?.Invoke(CoreAction.LoadCore(historyEvent.Ticks, bookmarkHistoryEvent.Bookmark.State));
            }
            else if (historyEvent is RootHistoryEvent rootHistoryEvent)
            {
                Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
                SetCore(core);

                Display.GetFromBookmark(null);

                Auditors?.Invoke(CoreAction.Reset(rootHistoryEvent.Ticks));
            }
            else
            {
                throw new Exception("Can't set current event to an event that isn't the root or doesn't have a bookmark.");
            }

            _history.CurrentEvent = historyEvent;
        }

        public void Reverse()
        {
            lock (_snapshots)
            {
                if (_runningState == RunningState.Reverse || _snapshots.LastOrDefault() == null)
                {
                    return;
                }
            }

            lock (_runningStateLock)
            {
                _previousRunningState = SetRunningState(RunningState.Reverse);
            }

            Status = "Reversing";
        }

        public void ReverseStop()
        {
            lock (_runningStateLock)
            {
                SetRunningState(_previousRunningState);

                _core.AllKeysUp();
            }
        }

        public void ToggleReversibilityEnabled()
        {
            if (SnapshotLimit == 0)
            {
                SnapshotLimit = 3000;
            }
            else
            {
                SnapshotLimit = 0;
            }
        }

        public bool Persist(IFileSystem fileSystem, string filepath)
        {
            using (AutoPause())
            {
                if (File != null)
                {
                    // We already are persisted!
                    throw new InvalidOperationException("This machine is already persisted!");
                }

                if (String.IsNullOrEmpty(filepath))
                {
                    throw new ArgumentException("Invalid filepath.");
                }

                ITextFile textFile = fileSystem.OpenTextFile(filepath);
                MachineFileWriter machineFileWriter = new MachineFileWriter(textFile, _history);

                machineFileWriter.WriteHistory(_name);

                File = machineFileWriter;

                PersistantFilepath = filepath;

                return true;
            }
        }

        static public LocalMachine OpenFromFile(IFileSystem fileSystem, string filepath)
        {
            LocalMachine machine = new LocalMachine(null, new History());
            machine.PersistantFilepath = filepath;

            machine.OpenFromFile(fileSystem);

            return machine;
        }

        public void OpenFromFile(IFileSystem fileSystem)
        {
            if (IsOpen)
            {
                return;
            }

            ITextFile textFile = null;

            try
            {
                textFile = fileSystem.OpenTextFile(PersistantFilepath);
                MachineFileReader reader = new MachineFileReader();
                reader.ReadFile(textFile);

                MachineFileWriter file = new MachineFileWriter(textFile, reader.History, reader.NextLineId);

                _history = reader.History;
                _name = reader.Name;

                File = file;

                BookmarkHistoryEvent historyEvent = _history.CurrentEvent.MostRecent<BookmarkHistoryEvent>();
                SetCurrentEvent(historyEvent ?? _history.RootEvent);

                // Should probably be monitoring the IsOpen property, I think...
                Display.EnableGreyscale(false);
            }
            catch (Exception ex)
            {
                // Make sure we remove our lock on the machine file...
                textFile?.Dispose();
                textFile = null;

                File = null;

                throw ex;
            }
        }

        static public LocalMachine GetClosedMachine(IFileSystem fileSystem, string filepath)
        {
            MachineFileReader reader = new MachineFileReader();
            using (ITextFile textFile = fileSystem.OpenTextFile(filepath))
            {
                reader.ReadFile(textFile);
            }

            LocalMachine machine = New(reader.Name, reader.History, filepath);
            if (machine.History != null)
            {
                HistoryEvent historyEvent = machine.History.CurrentEvent.MostRecent<BookmarkHistoryEvent>(); ;

                machine.Display.GetFromBookmark((historyEvent as BookmarkHistoryEvent)?.Bookmark);
                machine.Display.EnableGreyscale(true);
            }

            return machine;
        }

        public bool IsOpen
        {
            get
            {
                return (PersistantFilepath == null || File != null) && (Core != null);
            }
        }

        public string PersistantFilepath
        {
            get
            {
                return _filepath;
            }

            private set
            {
                if (_filepath != value)
                {
                    _filepath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsOpen));
                }
            }
        }
    }
}
