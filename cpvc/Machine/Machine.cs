using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CPvC
{
    /// <summary>
    /// Represents a persistent instance of a CPvC machine.
    /// </summary>
    /// <remarks>
    /// The Machine class, in addition to encapsulating a running Core object, also maintains a file which contains the state of the machine.
    /// This allows a machine to be closed, and then resumed where it left off the next time it's opened.
    /// </remarks>
    public sealed class Machine : IMachineFileReader, INotifyPropertyChanged, IDisposable
    {
        private string _name;
        private Core _core;
        private bool _running;
        private int _autoPauseCount;

        public Display Display { get; private set; }
        public HistoryEvent CurrentEvent { get; private set; }
        public HistoryEvent RootEvent { get; private set; }
        private Dictionary<int, HistoryEvent> _historyEventById;

        public string Filepath { get; }

        private int _nextEventId;

        private readonly IFileSystem _fileSystem;
        private MachineFile _file;

        public event PropertyChangedEventHandler PropertyChanged;

        private string _status;

        public string Status
        {
            get
            {
                return _status;
            }

            set
            {
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        public Machine(string name, string machineFilepath, IFileSystem fileSystem)
        {
            _name = name;
            Filepath = machineFilepath;
            _running = false;
            _autoPauseCount = 0;

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
            }
            catch (Exception)
            {
                machine?.Dispose();

                throw;
            }

            return machine;
        }

        /// <summary>
        /// Fully opens a machine that was previously lazy-loaded.
        /// </summary>
        public void Open()
        {
            try
            {
                IByteStream file = _fileSystem.OpenBinaryFile(Filepath);
                _file = new MachineFile(file);

                _file.ReadFile(this);

                if (CurrentEvent == null)
                {
                    // The machine file is either empty or has some other problem...
                    throw new Exception(String.Format("Unable to load file \"{0}\"; CurrentEvent is null!", Filepath));
                }

                // Rewind from the current event to the most recent one with a bookmark...
                HistoryEvent bookmarkEvent = CurrentEvent;
                while (bookmarkEvent.Parent != null && bookmarkEvent.Bookmark == null)
                {
                    bookmarkEvent = bookmarkEvent.Parent;
                }

                Core core = GetCore(bookmarkEvent);

                CurrentEvent = bookmarkEvent;

                Display.EnableGreyscale(false);
                Display.GetFromBookmark(bookmarkEvent?.Bookmark);
                Core = core;
            }
            catch (Exception)
            {
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
                fileSystem.DeleteFile(machineFilepath);
                machine = new Machine(null, machineFilepath, fileSystem);

                IByteStream file = fileSystem.OpenBinaryFile(machine.Filepath);
                machine._file = new MachineFile(file);
                machine.Name = name;

                machine.RootEvent = HistoryEvent.CreateCheckpoint(machine.NextEventId(), 0, DateTime.UtcNow, null);
                machine._file.WriteHistoryEvent(machine.RootEvent);

                machine.CurrentEvent = machine.RootEvent;

                machine.Core = Core.Create(Core.Type.CPC6128);

                return machine;
            }
            catch (Exception)
            {
                machine?.Dispose();

                throw;
            }
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
                    bool ticksDifferent = (Core != null && CurrentEvent.Ticks != Core.Ticks);
                    if (ticksDifferent || (CurrentEvent.Bookmark == null && CurrentEvent != RootEvent) || (CurrentEvent.Bookmark != null && !CurrentEvent.Bookmark.System))
                    {
                        AddCheckpointWithBookmarkEvent(true);
                    }
                }
                finally
                {
                    _file.Close();
                }
            }

            Core = null;

            _running = false;
            _autoPauseCount = 0;

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
            if (core == _core && action != null && action.Type != CoreAction.Types.RunUntil)
            {
                AddEvent(HistoryEvent.CreateCoreAction(NextEventId(), action), true);

                switch (action.Type)
                {
                    case CoreActionBase.Types.LoadDisc:
                        Status = (action.MediaBuffer.GetBytes() != null) ? "Loaded disc" : "Ejected disc";
                        break;
                    case CoreActionBase.Types.LoadTape:
                        Status = (action.MediaBuffer.GetBytes() != null) ? "Loaded tape" : "Ejected tape";
                        break;
                    case CoreActionBase.Types.Reset:
                        Status = "Reset";
                        break;
                }
            }
        }

        public Core Core
        {
            get
            {
                return _core;
            }

            private set
            {
                if (_core == value)
                {
                    return;
                }

                if (_core != null)
                {
                    _core.Dispose();
                }

                if (value != null)
                {
                    value.SetScreenBuffer(Display.Buffer);
                    value.Auditors += RequestProcessed;
                    value.BeginVSync = BeginVSync;
                }

                _core = value;

                OnPropertyChanged("Core");
                OnPropertyChanged("RequiresOpen");
            }
        }

        public bool RequiresOpen
        {
            get
            {
                return _core == null;
            }
        }

        /// <summary>
        /// Helper class that pauses the machine on creation and resumes the machine when the object is disposed.
        /// </summary>
        private class AutoPauser : IDisposable
        {
            private readonly Machine _machine;

            public AutoPauser(Machine machine)
            {
                _machine = machine;
                _machine._autoPauseCount++;
                _machine.SetCoreRunning();
            }

            public void Dispose()
            {
                _machine._autoPauseCount--;
                _machine.SetCoreRunning();
            }
        }

        /// <summary>
        /// Pauses the machine and returns an IDisposable which, when disposed of, causes the machine to resume (if it was running before).
        /// </summary>
        /// <returns>A IDisposable interface.</returns>
        public IDisposable AutoPause()
        {
            return new AutoPauser(this);
        }

        /// <summary>
        /// The name of the machine.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }

            set
            {
                _name = value;
                _file?.WriteName(_name);

                OnPropertyChanged("Name");
            }
        }

        /// <summary>
        /// Delegate for VSync events.
        /// </summary>
        /// <param name="core">Core whose VSync signal went form low to high.</param>
        private void BeginVSync(Core core)
        {
            // Only copy to the display if the VSync is from a core we're interesting in.
            if (core != null && _core == core)
            {
                Display.CopyFromBufferAsync();
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

        public void Start()
        {
            _running = true;
            SetCoreRunning();
            Status = "Resumed";
        }

        public void Stop()
        {
            _running = false;
            SetCoreRunning();
            Status = "Paused";
        }

        public void ToggleRunning()
        {
            if (_running)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        public void AdvancePlayback(int samples)
        {
            _core?.AdvancePlayback(samples);
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            return _core?.ReadAudio16BitStereo(buffer, offset, samplesRequested) ?? 0;
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
        public void SeekToLastBookmark()
        {
            HistoryEvent lastBookmarkEvent = CurrentEvent;
            while (lastBookmarkEvent != null)
            {
                if (lastBookmarkEvent.Type == HistoryEvent.Types.Checkpoint && lastBookmarkEvent.Bookmark != null && !lastBookmarkEvent.Bookmark.System && lastBookmarkEvent.Ticks != Core.Ticks)
                {
                    TimeSpan before = Helpers.GetTimeSpanFromTicks(Core.Ticks);
                    SetCurrentEvent(lastBookmarkEvent);
                    TimeSpan after = Helpers.GetTimeSpanFromTicks(Core.Ticks);
                    Status = String.Format("Rewound to {0} (-{1})", after.ToString(@"hh\:mm\:ss"), (after - before).ToString(@"hh\:mm\:ss"));
                    return;
                }

                lastBookmarkEvent = lastBookmarkEvent.Parent;
            }

            // No bookmarks? Go all the way back to the root!
            SetCurrentEvent(RootEvent);
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

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void EnableTurbo(bool enabled)
        {
            _core.EnableTurbo(enabled);

            Status = enabled ? "Turbo enabled" : "Turbo disabled";
        }

        /// <summary>
        /// Changes the current position in the timeline.
        /// </summary>
        /// <param name="bookmarkEvent">The event to become the current event in the timeline. This event must have a bookmark.</param>
        public void SetCurrentEvent(HistoryEvent bookmarkEvent)
        {
            // Add a checkpoint at the current position to properly mark the end of this branch...
            Core.Stop();
            SetCheckpoint();
            byte volume = Core.Volume;

            Display.GetFromBookmark(bookmarkEvent?.Bookmark);
            Core = Machine.GetCore(bookmarkEvent);
            Core.Volume = volume;

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
        /// <remarks>
        /// This is useful for compacting the size of the machine file, due to the fact that bookmark and timeline deletions don't actually
        /// remove anything from the machine file, but simply log the fact they happened.
        /// </remarks>
        public void RewriteMachineFile()
        {
            using (AutoPause())
            {
                string tempname = Filepath + ".new";
                IByteStream newFile = _fileSystem.OpenBinaryFile(tempname);

                MachineFile tempfile = null;
                try
                {
                    tempfile = new MachineFile(newFile);

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

                _file = new MachineFile(_fileSystem.OpenBinaryFile(Filepath));
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
        /// Sets the core to the appropriate running state, given the <c>_running</c> and <c>_autoPauseCount</c> members.
        /// </summary>
        private void SetCoreRunning()
        {
            if (_core == null)
            {
                return;
            }

            bool shouldRun = (_running && _autoPauseCount == 0);
            if (shouldRun && !_core.Running)
            {
                _core.Start();
            }
            else if (!shouldRun && _core.Running)
            {
                _core.Stop();
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
                byte[] screen = Display.GetBitmapBytes();
                byte[] core = _core.GetState();

                return new Bookmark(system, core, screen);
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
        /// <param name="historyEvent">HistoryEvent to create the core from.</param>
        /// <returns>If the HistoryEvent contains a bookmark, returns a core based on that bookmark. If the HistoryEvent is the root event, a newly-instantiated core is returned. Otherwise, null is returned.</returns>
        static private Core GetCore(HistoryEvent historyEvent)
        {
            Core core = null;
            try
            {
                if (historyEvent.Parent == null)
                {
                    // A history event with no parent is assumed to be the root.
                    core = Core.Create(Core.Type.CPC6128);
                }
                else if (historyEvent.Bookmark != null)
                {
                    core = Core.Create(historyEvent.Bookmark.State.GetBytes());

                    // Ensure all keys are in an "up" state.
                    for (byte keycode = 0; keycode < 80; keycode++)
                    {
                        core.KeyPress(keycode, false);
                    }
                }

                return core;
            }
            catch (Exception)
            {
                core?.Dispose();

                throw;
            }
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
    }
}
