using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace CPvC
{
    public sealed class ReplayMachine : Machine,
        IMachine,
        IPausableMachine,
        ITurboableMachine,
        IPrerecordedMachine,
        IReversibleMachine,
        INotifyPropertyChanged
    {
        private readonly UInt64 _endTicks;

        private readonly List<HistoryEvent> _historyEvents;

        private History _history;

        private int _historyEventIndex;
        private object _historyLock;

        public UInt64 EndTicks
        {
            get
            {
                return _endTicks;
            }
        }

        public override WaitHandle CanProcessEvent
        {
            get
            {
                return AudioBuffer.UnderrunEvent;
            }
        }

        public ReplayMachine(HistoryEvent historyEvent)
        {
            IMachineAction action = (historyEvent as CoreActionHistoryEvent)?.CoreAction;
            if (action != null && action is RunUntilAction runUntilAction)
            {
                _endTicks = runUntilAction.StopTicks;
            }
            else
            {
                _endTicks = historyEvent.Ticks;
            }

            OnPropertyChanged(nameof(EndTicks));

            _historyLock = new object();

            _history = new History();
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();

            while (historyEvent != null)
            {
                historyEvents.Insert(0, historyEvent);

                historyEvent = historyEvent.Parent;
            }

            _historyEvents = new List<HistoryEvent>();
            _historyEvents.Add(new RootHistoryEvent(null));

            foreach (HistoryEvent e in historyEvents)
            {
                switch (e)
                {
                    case CoreActionHistoryEvent coreActionHistoryEvent:
                        _historyEvents.Add(_history.AddCoreAction(new RunUntilAction(coreActionHistoryEvent.Ticks, coreActionHistoryEvent.Ticks, null)));
                        _historyEvents.Add(_history.AddCoreAction(MachineAction.Clone(coreActionHistoryEvent.CoreAction)));
                        break;
                    case BookmarkHistoryEvent bookmarkHistoryEvent:
                        _historyEvents.Add(_history.AddCoreAction(new RunUntilAction(bookmarkHistoryEvent.Ticks, bookmarkHistoryEvent.Ticks, null)));
                        _historyEvents.Add(_history.AddBookmark(e.Ticks, bookmarkHistoryEvent.Bookmark.Clone()));
                        break;
                }
            }

            _historyEventIndex = 0;
        }

        public override string Name
        {
            get; set;
        }

        public bool CanStart
        {
            get
            {
                return RunningState == RunningState.Paused && Ticks < EndTicks;
            }
        }

        public bool CanStop
        {
            get
            {
                return RunningState == RunningState.Running;
            }
        }

        public bool CanClose
        {
            get
            {
                return true;
            }
        }

        public new MachineRequest Start()
        {
            // Would a better test be to see if the core has outstanding requests?
            if (Ticks < _endTicks)
            {
                return base.Start();
            }

            return null;
        }

        public MachineRequest SeekToStart()
        {
            ReplayMachineRequest machineRequest = new ReplayMachineRequest(0);
            PushRequest(machineRequest);

            return machineRequest;
        }

        public MachineRequest SeekToPreviousBookmark()
        {
            lock (_historyLock)
            {
                int index = _historyEventIndex;

                while (true)
                {
                    index--;

                    if (index < 0)
                    {
                        ReplayMachineRequest machineRequest = new ReplayMachineRequest(0);
                        PushRequest(machineRequest);

                        return machineRequest;
                    }

                    if (_historyEvents[index] is BookmarkHistoryEvent bookmarkEvent && bookmarkEvent.Ticks < Ticks)
                    {
                        ReplayMachineRequest machineRequest = new ReplayMachineRequest(index);
                        PushRequest(machineRequest);

                        return machineRequest;
                    }
                }
            }

            return null;
        }

        public MachineRequest SeekToNextBookmark()
        {
            lock (_historyLock)
            {
                int index = _historyEventIndex;

                while (true)
                {
                    index++;

                    // Should probably just send this in the request and let the machine thread stop!
                    if (index >= _historyEvents.Count)
                    {
                        MachineRequest machineRequest = new PauseRequest();
                        PushRequest(machineRequest);

                        return machineRequest;
                    }

                    if (_historyEvents[index] is BookmarkHistoryEvent bookmarkEvent)
                    {
                        ReplayMachineRequest machineRequest = new ReplayMachineRequest(index);
                        PushRequest(machineRequest);

                        return machineRequest;
                    }
                }
            }
        }

        protected override MachineRequest GetNextCoreRequest()
        {
            MachineRequest request = null;
            if (_requestedRunningState == RunningState.Reverse)
            {
                lock (_snapshots)
                {
                    if (_snapshots.Count > 0)
                    {
                        int snapshotId = _snapshots[_snapshots.Count - 1];
                        _snapshots.RemoveAt(_snapshots.Count - 1);

                        request = new RevertToSnapshotRequest(snapshotId);
                    }
                }

                return request;
            }

            request = ProcessHistoryEvent();

            if (_historyEventIndex >= _historyEvents.Count)
            {
                Stop();
            }

            return request;
        }

        protected override void CoreActionDone(MachineRequest request, IMachineAction action)
        {
            RaiseEvent(action);
        }

        protected override void BeginVSync()
        {
            CoreSnapshot coreSnapshot = CreateCoreSnapshot(_nextCoreSnapshotId++);
            if (coreSnapshot != null)
            {
                _currentCoreSnapshot = coreSnapshot;
                _core.CreateSnapshotSync(coreSnapshot.Id);

                IMachineAction action = new CreateSnapshotAction(Ticks, coreSnapshot.Id);
                RaiseEvent(action);
            }

            base.BeginVSync();
        }

        public MachineRequest Reverse()
        {
            lock (_snapshots)
            {
                if (_requestedRunningState != RunningState.Running || _snapshots.Count == 0)
                {
                    return null;
                }
            }

            MachineRequest request = new ReverseRequest();
            PushRequest(request);

            return request;
        }

        public void ToggleReversibilityEnabled()
        {

        }

        public override IMachineAction ProcessRevertToSnapshot(RevertToSnapshotRequest request)
        {
            // Play samples from the snapshot info until they run out, then actually revert to snapshot.
            CoreSnapshot coreSnapshot = _allCoreSnapshots[request.SnapshotId];
            AudioBuffer.CopyFrom(coreSnapshot.AudioBuffer);

            if (coreSnapshot.AudioBuffer.SampleCount == 0)
            {
                ReplayCoreSnapshot cs = (ReplayCoreSnapshot)_allCoreSnapshots[request.SnapshotId];
                _historyEventIndex = cs.HistoryEventIndex;

                _core.RevertToSnapshotSync(request.SnapshotId);

                RaiseDisplayUpdated();

                _core.DeleteSnapshotSync(request.SnapshotId);

                _allCoreSnapshots.Remove(request.SnapshotId);

                return MachineAction.RevertToSnapshot(0 /*request.StopTicks*/, request.SnapshotId);
            }

            return null;
        }

        private class ReplayCoreSnapshot : CoreSnapshot
        {
            public ReplayCoreSnapshot(int id, int historyEventIndex) : base(id)
            {
                HistoryEventIndex = historyEventIndex;
            }

            public int HistoryEventIndex { get; }
        }

        protected override CoreSnapshot CreateCoreSnapshot(int id)
        {
            ReplayCoreSnapshot coreSnapshot = new ReplayCoreSnapshot(id, _historyEventIndex);

            lock (_snapshots)
            {
                _allCoreSnapshots.Add(id, coreSnapshot);
                _snapshots.Add(id);
            }

            return coreSnapshot;
        }


        protected override void OnPropertyChanged(string name)
        {
            if (name == nameof(RunningState))
            {
                base.OnPropertyChanged(nameof(CanStart));
                base.OnPropertyChanged(nameof(CanStop));
            }

            base.OnPropertyChanged(name);
        }

        private MachineRequest ProcessHistoryEvent()
        {
            if (_historyEvents[_historyEventIndex] is RootHistoryEvent)
            {
                Core core = new Core(Core.LatestVersion, Core.Type.CPC6128);

                MachineRequest request = new LoadCoreRequest(MemoryBlob.Create(core.GetState()), MemoryBlob.Create(core.GetScreen()));
                _historyEventIndex++;
                return request;
            }
            else if (_historyEvents[_historyEventIndex] is CoreActionHistoryEvent ce)
            {
                MachineRequest request = (MachineRequest)ce.CoreAction; //.AsRequest();

                if (request is RunUntilRequest runUntilRequest && Ticks < runUntilRequest.StopTicks)
                {
                    UInt64 remainingTicks = runUntilRequest.StopTicks - Ticks;
                    if (remainingTicks > 10000)
                    {
                        request = new RunUntilRequest(Ticks + 10000);
                    }
                    else
                    {
                        _historyEventIndex++;
                    }
                }
                else
                {
                    _historyEventIndex++;
                }

                return request;
            }
            else if (_historyEvents[_historyEventIndex] is BookmarkHistoryEvent be)
            {
                // Test - the current state of the machine should match the bookmark!
                byte[] currentState = GetState();
                byte[] bookmarkState = be.Bookmark.State.GetBytes();
                if (!currentState.SequenceEqual(bookmarkState))
                {
                    Core bookmarkCore = new Core(Core.LatestVersion, Core.Type.CPC6128);
                    bookmarkCore.LoadState(bookmarkState);

                    //throw new Exception("Current state doesn't match bookmarked state!");
                }



                MachineRequest request = new LoadCoreRequest(be.Bookmark.State, be.Bookmark.Screen);
                _historyEventIndex++;
                return request;
            }

            // Shouldn't get here!
            throw new Exception(String.Format("Unexpected history event type: {0}", _historyEvents[_historyEventIndex].GetType().Name));
        }

        protected override void ProcessRequest(MachineRequest request)
        {
            if (request is ReplayMachineRequest rmr)
            {
                lock (_historyLock)
                {
                    if (_historyEventIndex != rmr.HistoryIndex)
                    {
                        DeleteAllCoreSnapshots();
                    }

                    _historyEventIndex = rmr.HistoryIndex;

                    MachineRequest request2 = ProcessHistoryEvent();
                    ProcessCoreRequest(request2);

                    rmr.SetProcessed();
                }
            }
            else
            {
                base.ProcessRequest(request);
            }
        }

        private class ReplayMachineRequest : MachineRequest
        {
            public ReplayMachineRequest(int historyIndex)
            {
                HistoryIndex = historyIndex;
            }

            public int HistoryIndex { get; }
        }
    }
}
