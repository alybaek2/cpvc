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

        public MachineHistory History
        {
            get
            {
                return _history;
            }
        }

        private RunningState _previousRunningState;

        private MachineHistory _history;

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
                    _file = value;
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

        public LocalMachine(string name)
        {
            _name = name;

            Display = new Display();

            _previousRunningState = RunningState.Paused;

            _snapshots = new List<SnapshotInfo>();

            _history = new MachineHistory();
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

        static public LocalMachine New(string name, MachineHistory history)
        {
            LocalMachine machine = new LocalMachine(name);
            if (history != null)
            {
                machine._history = history;
            }

            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            machine.SetCore(core);

            return machine;
        }

        public void Close()
        {
            if (IsOpen)
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
                }
            }

            SetCore(null);

            if (File != null)
            {
                File.Dispose();
                File = null;
            }

            _history = new MachineHistory();

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
                            _history.SetCurrent(historyEvent);
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
                            if (historyEvent.Type == HistoryEventType.CoreAction && historyEvent.CoreAction.Type == CoreRequest.Types.RunUntil)
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
                if (lastBookmarkEvent.Type == HistoryEventType.Bookmark && !lastBookmarkEvent.Bookmark.System && lastBookmarkEvent.Ticks != Core.Ticks)
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
        /// Deletes an event (and all its children) without changing the current event.
        /// </summary>
        /// <param name="historyEvent">Event to delete.</param>
        /// <param name="loading">Indicates whether the MachineFile is being loaded from a file.</param>
        private void DeleteEvent(HistoryEvent historyEvent, bool writeToFile)
        {
            using (AutoPause())
            {
                if (!_history.DeleteEventAndChildren(historyEvent))
                {
                    return;
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

        /// <summary>
        /// Rewrites the machine file according to the current in-memory representation of the timeline.
        /// </summary>
        /// <param name="diffsEnabled">Enable calculation of diffs. Setting this to true can cause compacting to take a long time.</param>
        /// <remarks>
        /// This is useful for compacting the size of the machine file, due to the fact that bookmark and timeline deletions don't actually
        /// remove anything from the machine file, but simply log the fact they happened.
        /// </remarks>
        /// 
        public void Compact(IFileSystem fileSystem, bool diffsEnabled)
        {
            // Only allow closed machines to compact!
            if (!CanCompact())
            {
                throw new Exception("Can't compact an open machine!");
            }

            string oldFilepath = PersistantFilepath;
            string newFilepath = oldFilepath + ".tmp";

            string name;
            MachineHistory history;

            using (ITextFile textFile = fileSystem.OpenTextFile(oldFilepath))
            {
                MachineFileReader reader = new MachineFileReader();

                Dictionary<HistoryEvent, int> historyEventToId = new Dictionary<HistoryEvent, int>();
                reader.ReadFile(textFile, out name, out history, out _, out _);
            }

            using (ITextFile textFile = fileSystem.OpenTextFile(newFilepath))
            {
                MachineFileWriter writer = new MachineFileWriter(textFile, history);

                writer.WriteHistory(name);
                writer.Dispose();
            }

            fileSystem.ReplaceFile(oldFilepath, newFilepath);
        }

        public bool CanCompact()
        {
            return !IsOpen && PersistantFilepath != null;
        }

        private HistoryEvent AddCheckpointWithBookmarkEvent(bool system)
        {
            Bookmark bookmark = GetBookmark(system);
            HistoryEvent e = _history.AddBookmark(_core.Ticks, bookmark);

            return e;
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

        public bool DeleteEvent(HistoryEvent e)
        {
            return _history.DeleteEvent(e);
        }

        public void SetCurrentEvent(HistoryEvent historyEvent)
        {
            if (historyEvent.Type == HistoryEventType.Bookmark)
            {
                Core core = Core.Create(Core.LatestVersion, historyEvent.Bookmark.State.GetBytes());
                SetCore(core);

                Display.GetFromBookmark(historyEvent.Bookmark);

                Auditors?.Invoke(CoreAction.LoadCore(historyEvent.Ticks, historyEvent.Bookmark.State));
            }
            else if (historyEvent == History.RootEvent)
            {
                Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
                SetCore(core);

                Display.GetFromBookmark(null);

                Auditors?.Invoke(CoreAction.Reset(historyEvent.Ticks));
            }
            else
            {
                throw new Exception("Can't set current event to an event that isn't the root or doesn't have a bookmark.");
            }

            _history.SetCurrent(historyEvent);
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

                File.Machine = this;
                PersistantFilepath = filepath;

                return true;
            }
        }

        static public LocalMachine OpenFromFile(IFileSystem fileSystem, string filepath)
        {
            LocalMachine machine = new LocalMachine(null);
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

            ITextFile textFile = fileSystem.OpenTextFile(PersistantFilepath);

            Dictionary<HistoryEvent, int> historyEventToId = new Dictionary<HistoryEvent, int>();

            MachineFileReader reader = new MachineFileReader();
            reader.ReadFile(textFile, out string name, out MachineHistory history, out int nextPersistentId, out int nextBlobId);

            MachineFileWriter file = new MachineFileWriter(textFile, history, nextPersistentId, nextBlobId);

            _history = history;
            _name = name;

            file.Machine = this;
            File = file;

            HistoryEvent historyEvent = MostRecentBookmark(_history);
            SetCurrentEvent(historyEvent);

            // Should probably be monitoring the IsOpen property, I think...
            Display.EnableGreyscale(false);
        }

        static public LocalMachine Create(IFileSystem fileSystem, string filepath)
        {
            using (ITextFile fileByteStream = fileSystem.OpenTextFile(filepath))
            {
                Dictionary<HistoryEvent, int> historyEventToId = new Dictionary<HistoryEvent, int>();

                MachineFileReader reader = new MachineFileReader();
                reader.ReadFile(fileByteStream, out string name, out MachineHistory history, out int nextPersistentId, out int nextBlobId);

                MachineFileWriter file = new MachineFileWriter(fileByteStream, history, nextPersistentId, nextBlobId);

                LocalMachine machine = LocalMachine.New(name, history);
                machine.PersistantFilepath = filepath;

                if (history != null)
                {
                    HistoryEvent historyEvent = MostRecentBookmark(history);

                    machine.Display.GetFromBookmark(historyEvent.Bookmark);
                    machine.Display.EnableGreyscale(true);
                }

                return machine;
            }
        }

        static private HistoryEvent MostRecentBookmark(MachineHistory history)
        {
            HistoryEvent historyEvent = history.CurrentEvent;
            while (historyEvent.Type != HistoryEventType.Bookmark && historyEvent != history.RootEvent)
            {
                historyEvent = historyEvent.Parent;
            }

            return historyEvent;
        }

        public bool IsOpen
        {
            get
            {
                return PersistantFilepath == null || File != null;
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
