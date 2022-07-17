using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace CPvC
{
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

        private History _history;

        private int _snapshotLimit = 3000;
        private int _lastTakenSnapshotId = -1;
        private List<int> _snapshots;
        private Dictionary<int, SnapshotInfo> _allSnapshots;

        private bool _isOpen = false;

        private MachineFile _file;

        private MachineFile File
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

            _snapshots = new List<int>();
            _allSnapshots = new Dictionary<int, SnapshotInfo>();
            _history = history;

            _core.Create(Core.LatestVersion, Core.Type.CPC6128);
            IsOpen = true;
        }

        public void Dispose()
        {
            Close();
        }

        public override WaitHandle CanProcessEvent
        {
            get
            {
                return AudioBuffer.UnderrunEvent;
            }
        }

        public bool CanStart
        {
            get
            {
                return IsOpen && ActualRunningState == RunningState.Paused;
            }
        }

        public bool CanStop
        {
            get
            {
                return IsOpen && ActualRunningState == RunningState.Running;
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

            machine.PersistantFilepath = persistentFilepath;

            return machine;
        }

        public override void Close()
        {
            if (IsOpen)
            {
                RequestStopAndWait();

                try
                {
                    // Create a system bookmark so the machine can resume from where it left off the next time it's loaded, but don't
                    // create one if we already have a system bookmark at the current event, or we're at the root event.
                    BookmarkHistoryEvent currentBookmarkEvent = _history.CurrentEvent as BookmarkHistoryEvent;
                    if (currentBookmarkEvent == null ||
                        (currentBookmarkEvent.Ticks != Ticks) ||
                        (currentBookmarkEvent.Bookmark == null) ||
                        (currentBookmarkEvent.Bookmark != null && !currentBookmarkEvent.Bookmark.System))
                    {
                        AddBookmark(true);
                    }
                }
                finally
                {
                }
            }

            if (_core != null)
            {
                _core.Close();
            }

            if (File != null)
            {
                File.Dispose();
                File = null;
            }

            _history = new History();

            Status = null;

            base.Close();

            IsOpen = false;
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
        protected override void BeginVSync()
        {
            TakeSnapshot();

            base.BeginVSync();
        }

        public override int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            return base.ReadAudio(buffer, offset, samplesRequested);
        }

        private void TakeSnapshot()
        {
            if (SnapshotLimit > 0)
            {
                // This doesn't necessarily have to go through the request queue.... can't just do it right away!
                int snapshotId = ++_lastTakenSnapshotId;

                lock (_snapshots)
                {
                    // Figure out what history event should be set as current if we revert to this snapshot.
                    // If the current event is a RunUntil, it may not be "finalized" yet (i.e. it may still
                    // be updated), so go with its parent.
                    HistoryEvent historyEvent = _history.MostRecentClosedEvent(_history.CurrentEvent);

                    SnapshotInfo newSnapshot = new SnapshotInfo(snapshotId, historyEvent);
                    _snapshots.Add(newSnapshot.Id);
                    _allSnapshots.Add(newSnapshot.Id, newSnapshot);

                    _core.CreateSnapshotSync(snapshotId);

                    CoreAction action = CoreAction.CreateSnapshot(Ticks, snapshotId);
                    RaiseEvent(action);

                    while (_snapshots.Count > SnapshotLimit)
                    {
                        SnapshotInfo snapshot = _allSnapshots[_snapshots[0]];
                        _allSnapshots.Remove(_snapshots[0]);
                        _snapshots.RemoveAt(0);

                        _core.DeleteSnapshotSync(snapshot.Id);
                        action = CoreAction.DeleteSnapshot(Ticks, snapshot.Id);
                        RaiseEvent(action);
                    }
                }
            }
        }

        protected override void CoreActionDone(CoreRequest request, CoreAction action)
        {
            if (action == null)
            {
                return;
            }

            RaiseEvent(action);

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
                    if (_snapshots.Count > 0)
                    {
                        SnapshotInfo newSnapshot = _allSnapshots[_snapshots.Last()];
                        if (newSnapshot != null && action.AudioSamples != null)
                        {
                            newSnapshot.AudioBuffer.Write(action.AudioSamples);
                        }
                    }
                }
            }
            else if (action.Type == CoreAction.Types.RevertToSnapshot)
            {
                SnapshotInfo currentSnapshot = _allSnapshots[action.SnapshotId];
                if (currentSnapshot != null)
                {
                    _core.DeleteSnapshotSync(request.SnapshotId);

                    _history.CurrentEvent = currentSnapshot.HistoryEvent;

                    _allSnapshots.Remove(request.SnapshotId);
                }
            }
            else if (action.Type == CoreAction.Types.CreateSnapshot)
            {
                lock (_snapshots)
                {
                    // Figure out what history event should be set as current if we revert to this snapshot.
                    // If the current event is a RunUntil, it may not be "finalized" yet (i.e. it may still
                    // be updated), so go with its parent.
                    HistoryEvent historyEvent = _history.MostRecentClosedEvent(_history.CurrentEvent);

                    SnapshotInfo newSnapshot = new SnapshotInfo(action.SnapshotId, historyEvent);
                    _snapshots.Add(newSnapshot.Id);
                    _allSnapshots.Add(newSnapshot.Id, newSnapshot);

                    while (_snapshots.Count > _snapshotLimit)
                    {
                        int snapshotId = _snapshots[0];
                        _snapshots.RemoveAt(0);
                        _allSnapshots.Remove(snapshotId);
                        PushRequest(CoreRequest.DeleteSnapshot(snapshotId));
                    }
                }
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

        public CoreRequest Reset()
        {
            CoreRequest request = CoreRequest.Reset();
            PushRequest(request);
            Status = "Reset";

            return request;
        }

        public CoreRequest Key(byte keycode, bool down)
        {
            CoreRequest request = CoreRequest.KeyPress(keycode, down);
            PushRequest(request);

            return request;
        }

        public void AddBookmark(bool system)
        {
            using (AutoPause())
            {
                Bookmark bookmark = GetBookmark(system);
                HistoryEvent historyEvent = _history.AddBookmark(_core.Ticks, bookmark);

                Diagnostics.Trace("Created bookmark at tick {0}", _core.Ticks);
                Status = String.Format("Bookmark added at {0}", Helpers.GetTimeSpanFromTicks(Ticks).ToString(@"hh\:mm\:ss"));
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
                if (lastBookmarkEvent is BookmarkHistoryEvent b && !b.Bookmark.System && b.Ticks != Ticks)
                {
                    TimeSpan before = Helpers.GetTimeSpanFromTicks(Ticks);
                    JumpToBookmark(b);
                    TimeSpan after = Helpers.GetTimeSpanFromTicks(Ticks);
                    Status = String.Format("Rewound to {0} (-{1})", after.ToString(@"hh\:mm\:ss"), (after - before).ToString(@"hh\:mm\:ss"));
                    return;
                }

                lastBookmarkEvent = lastBookmarkEvent.Parent;
            }

            // No bookmarks? Go all the way back to the root!
            JumpToRoot();
            Status = "Rewound to start";
        }

        public CoreRequest LoadDisc(byte drive, byte[] diskBuffer)
        {
            CoreRequest request = CoreRequest.LoadDisc(drive, diskBuffer);
            PushRequest(request);

            return request;
        }

        public CoreRequest LoadTape(byte[] tapeBuffer)
        {
            CoreRequest request = CoreRequest.LoadTape(tapeBuffer);
            PushRequest(request);

            return request;
        }

        /// <summary>
        /// Changes the current position in the timeline.
        /// </summary>
        /// <param name="bookmarkEvent">The event to become the current event in the timeline. This event must have a bookmark.</param>
        public void JumpToBookmark(BookmarkHistoryEvent bookmarkEvent)
        {
            using (AutoPause())
            {
                SetCurrentEvent(bookmarkEvent);
            }
        }

        /// <summary>
        /// Changes the current position in the timeline to the root.
        /// </summary>
        public void JumpToRoot()
        {
            using (AutoPause())
            {
                SetCurrentToRoot();
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
        /// <remarks>
        /// This is useful for compacting the size of the machine file, due to the fact that bookmark and timeline deletions don't actually
        /// remove anything from the machine file, but simply log the fact they happened.
        /// </remarks>
        /// 
        public void Compact(IFileSystem fileSystem)
        {
            // Only allow closed machines to compact!
            if (!CanCompact)
            {
                throw new InvalidOperationException();
            }

            string oldFilepath = PersistantFilepath;
            string newFilepath = oldFilepath + ".tmp";

            MachineFileInfo info = null;
            using (ITextFile textFile = fileSystem.OpenTextFile(oldFilepath))
            {
                info = MachineFile.Read(textFile);
            }


            using (ITextFile textFile = fileSystem.OpenTextFile(newFilepath))
            using (MachineFile writer = new MachineFile(textFile, info.History))
            {
                writer.WriteHistory(info.Name);
            }

            fileSystem.ReplaceFile(oldFilepath, newFilepath);
        }

        public bool CanCompact
        {
            get
            {
                return PersistantFilepath != null && !IsOpen;
            }
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
                byte[] screen = _core.GetScreen();
                byte[] core = _core.GetState();

                return new Bookmark(system, _core.Version, core, screen);
            }
        }

        public bool DeleteBookmark(HistoryEvent e)
        {
            return _history.DeleteBookmark(e);
        }

        private void SetCurrentEvent(BookmarkHistoryEvent bookmarkHistoryEvent)
        {
            _core.CreateFromBookmark(Core.LatestVersion, bookmarkHistoryEvent.Bookmark.State.GetBytes());

            SetScreen(bookmarkHistoryEvent.Bookmark.Screen.GetBytes());

            CoreAction action = CoreAction.LoadCore(bookmarkHistoryEvent.Ticks, bookmarkHistoryEvent.Bookmark.State);
            RaiseEvent(action);

            _history.CurrentEvent = bookmarkHistoryEvent;
        }

        private void SetCurrentToRoot()
        {
            _core.Create(Core.LatestVersion, Core.Type.CPC6128);

            BlankScreen();

            CoreAction action = CoreAction.Reset(_history.RootEvent.Ticks);
            RaiseEvent(action);

            _history.CurrentEvent = _history.RootEvent;
        }

        public void Reverse()
        {
            lock (_snapshots)
            {
                if (_requestedState != RunningState.Running || _snapshots.Count == 0)
                {
                    return;
                }
            }

            RequestedState = RunningState.Reverse;

            OnPropertyChanged(nameof(ActualRunningState));

            Status = "Reversing";
        }

        public void ReverseStop()
        {
            RequestedState = RunningState.Running;

            OnPropertyChanged(nameof(ActualRunningState));

            AllKeysUp();
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
                    throw new ArgumentException("Invalid filepath.", nameof(filepath));
                }

                ITextFile textFile = fileSystem.OpenTextFile(filepath);
                MachineFile machineFile = new MachineFile(textFile, _history);

                machineFile.WriteHistory(_name);

                File = machineFile;

                PersistantFilepath = filepath;

                return true;
            }
        }

        static public LocalMachine OpenFromFile(IFileSystem fileSystem, string filepath)
        {
            LocalMachine machine = new LocalMachine(null, new History());
            machine.PersistantFilepath = filepath;

            machine.IsOpen = false;
            machine.OpenFromFile(fileSystem);

            return machine;
        }

        public void OpenFromFile(IFileSystem fileSystem)
        {
            if (IsOpen)
            {
                return;
            }

            _core = new Core(Core.LatestVersion, Core.Type.CPC6128);

            if (_machineThread == null)
            {
                _machineThread = new System.Threading.Thread(MachineThread);
                _machineThread.Start();
            }

            ITextFile textFile = null;

            try
            {
                textFile = fileSystem.OpenTextFile(PersistantFilepath);
                MachineFileInfo info = MachineFile.Read(textFile);
                MachineFile file = new MachineFile(textFile, info.History, info.NextLineId);

                _history = info.History;
                _name = info.Name;

                File = file;

                BookmarkHistoryEvent historyEvent = _history.CurrentEvent.MostRecent<BookmarkHistoryEvent>();
                if (historyEvent != null)
                {
                    SetCurrentEvent(historyEvent);
                }
                else
                {
                    SetCurrentToRoot();
                }
            }
            catch (Exception ex)
            {
                // Make sure we remove our lock on the machine file...
                textFile?.Dispose();
                textFile = null;

                File = null;

                throw ex;
            }

            IsOpen = true;
        }

        static public LocalMachine GetClosedMachine(IFileSystem fileSystem, string filepath)
        {
            MachineFileInfo info = null;
            using (ITextFile textFile = fileSystem.OpenTextFile(filepath))
            {
                info = MachineFile.Read(textFile);
            }

            LocalMachine machine = New(info.Name, info.History, filepath);
            HistoryEvent historyEvent = machine.History.CurrentEvent.MostRecent<BookmarkHistoryEvent>();
            machine.IsOpen = false;

            machine.SetScreen((historyEvent as BookmarkHistoryEvent)?.Bookmark.Screen.GetBytes());

            return machine;
        }

        public bool IsOpen
        {
            get
            {
                return _isOpen;
            }

            private set
            {
                if (value == _isOpen)
                {
                    return;
                }

                _isOpen = value;
                OnPropertyChanged();
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
                }
            }
        }

        protected override void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (name == nameof(ActualRunningState))
            {
                base.OnPropertyChanged(nameof(CanStart));
                base.OnPropertyChanged(nameof(CanStop));
            }

            base.OnPropertyChanged(name);
        }

        protected override CoreRequest GetNextRequest()
        {
            CoreRequest request = base.GetNextRequest();
            if (request == null)
            {
                if (_requestedState == RunningState.Running)
                {
                    request = CoreRequest.RunUntil(Ticks + 1000);
                }
                else if (_requestedState == RunningState.Reverse)
                {
                    lock (_snapshots)
                    {
                        if (_snapshots.Count > 0)
                        {
                            int snapshotId = _snapshots[_snapshots.Count - 1];
                            _snapshots.RemoveAt(_snapshots.Count - 1);

                            request = CoreRequest.RevertToSnapshot(snapshotId);
                        }
                    }
                }
            }

            return request;
        }

        public override (bool, CoreAction) ProcessRevertToSnapshot(CoreRequest request)
        {
            // Play samples from the snapshot info until they run out, then actually revert to snapshot.
            SnapshotInfo snapshotInfo = _allSnapshots[request.SnapshotId];
            AudioBuffer.CopyFrom(snapshotInfo.AudioBuffer);

            if (snapshotInfo.AudioBuffer.SampleCount == 0)
            {
                _core.RevertToSnapshotSync(request.SnapshotId);

                RaiseDisplayUpdated();

                OnPropertyChanged(nameof(Ticks));

                return (true, CoreAction.RevertToSnapshot(request.StopTicks, request.SnapshotId));
            }

            return (false, null);
        }
    }
}
