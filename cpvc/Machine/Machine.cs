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

        private int _snapshotLimit = 500;
        private int _lastTakenSnapshotId = -1;
        private List<SnapshotInfo> _snapshots;

        private MachineFile _file;
        private string _filepath;

        public int SnapshotLimit
        {
            get
            {
                return _snapshotLimit;
            }

            set
            {
                _snapshotLimit = value;

                OnPropertyChanged();
            }
        }

        public Machine(string name, string machineFilepath, IFileSystem fileSystem)
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

        static public Machine Create(string name, MachineHistory history)
        {
            Machine machine = new Machine(name, null, null);
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

            if (_file != null)
            {
                _file.Close();
                _file = null;

                OnPropertyChanged("IsOpen");
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
                            if (historyEvent.Type == HistoryEventType.AddCoreAction && historyEvent.CoreAction.Type == CoreRequest.Types.RunUntil)
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
                if (lastBookmarkEvent.Type == HistoryEventType.AddBookmark && !lastBookmarkEvent.Bookmark.System && lastBookmarkEvent.Ticks != Core.Ticks)
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
        public void Compact(bool diffsEnabled)
        {
            throw new Exception("Need to re-implement!");
            //using (AutoPause())
            //{
            //    Machine machine = new Machine(String.Empty, String.Empty, null);
            //    MachineHistory newHistory = new MachineHistory();

            //    string tempname = Filepath + ".new";

            //    MachineFile tempfile = null;
            //    try
            //    {
            //        tempfile = new MachineFile(_fileSystem, tempname);
            //        tempfile.DiffsEnabled = diffsEnabled;
            //        tempfile.SetMachine(machine);
            //        tempfile.SetMachineHistory(newHistory);

            //        machine.Name = _name;
            //        _history.Copy(newHistory);
            //    }
            //    finally
            //    {
            //        tempfile?.Close();
            //    }

            //    _file.Close();

            //    Int64 newLength = _fileSystem.FileLength(tempname);
            //    Int64 oldLength = _fileSystem.FileLength(Filepath);

            //    _fileSystem.ReplaceFile(Filepath, tempname);

            //    _history = new MachineHistory();

            //    _file = new MachineFile(_fileSystem, Filepath);
            //    _file.SetMachine(this);
            //    _file.SetMachineHistory(_history);
            //    _file.ReadFile(out _name, out _history);

            //    Status = String.Format("Compacted machine file by {0}%", (Int64)(100 * ((double)(oldLength - newLength)) / ((double)oldLength)));
            //}
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

        /// <summary>
        /// Returns a new core based on a HistoryEvent.
        /// </summary>
        /// <param name="bookmark">Bookmark to create the core from. If null, then a new core is created.</param>
        /// <returns>If <c>bookmark</c> is not null, returns a core based on that bookmark. If the HistoryEvent is null, a newly-instantiated core is returned.</returns>
        private Core GetCore(Bookmark bookmark)
        {
            Core core;
            if (bookmark != null)
            {
                core = Core.Create(Core.LatestVersion, bookmark.State.GetBytes());
                core.AllKeysUp();

                return core;
            }

            core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);

            return core;
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
            if (historyEvent.Type == HistoryEventType.AddBookmark)
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

                CoreAction action = CoreAction.CoreVersion(historyEvent.Ticks, Core.LatestVersion);
                History.AddCoreAction(action);

                Auditors?.Invoke(action);
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
                SnapshotLimit = 500;
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
                if (_file != null)
                {
                    // We already are persisted!
                    throw new InvalidOperationException("This machine is already persisted!");
                }

                if (String.IsNullOrEmpty(filepath))
                {
                    throw new ArgumentException("Invalid filepath.");
                }

                IFileByteStream fileByteStream = fileSystem.OpenFileByteStream(filepath);
                _file = new MachineFile(fileByteStream);

                _file.History = new MachineHistory();
                History.CopyTo(_file.History);
                _file.History = History;

                _file.Machine = this;
                PersistantFilepath = filepath;

                _file.WriteName(Name);

                return true;
            }
        }

        public void OpenFromFile(IFileSystem fileSystem)
        {
            if (IsOpen)
            {
                return;
            }

            IFileByteStream fileByteStream = fileSystem.OpenFileByteStream(PersistantFilepath);
            MachineFile file = new MachineFile(fileByteStream);

            MachineHistory history;
            string name;
            file.ReadFile(out name, out history);

            _history = history;
            _name = name;

            file.Machine = this;
            file.History = _history;
            _file = file;
            OnPropertyChanged("IsOpen");


            HistoryEvent historyEvent = MostRecentBookmark(_history);
            SetCurrentEvent(historyEvent);

            // Should probably be monitoring the IsOpen property, I think...
            Display.EnableGreyscale(false);
        }

        static public Machine Create(IFileSystem fileSystem, string filepath)
        {
            using (IFileByteStream fileByteStream = fileSystem.OpenFileByteStream(filepath))
            {
                MachineFile file = new MachineFile(fileByteStream);

                file.ReadFile(out string name, out MachineHistory history);

                Machine machine = Machine.Create(name, history);
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
            while (historyEvent.Type != HistoryEventType.AddBookmark && historyEvent != history.RootEvent)
            {
                historyEvent = historyEvent.Parent;
            }

            return historyEvent;
        }

        public bool IsOpen
        {
            get
            {
                return PersistantFilepath == null || _file != null;
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
    }
}
