using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace CPvC
{
    public sealed class ReplayMachine : Machine,
        IMachine,
        IPausableMachine,
        ITurboableMachine,
        IPrerecordedMachine,
        INotifyPropertyChanged
    {
        private readonly UInt64 _endTicks;

        private readonly List<HistoryEvent> _historyEvents;

        private History _history;

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
            _endTicks = (historyEvent as CoreActionHistoryEvent)?.CoreAction.StopTicks ?? historyEvent.Ticks;

            OnPropertyChanged(nameof(EndTicks));

            _history = new History();
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();

            while (historyEvent != null)
            {
                historyEvents.Insert(0, historyEvent);

                historyEvent = historyEvent.Parent;
            }

            _historyEvents = new List<HistoryEvent>();

            foreach (HistoryEvent e in historyEvents)
            {
                switch (e)
                {
                    case CoreActionHistoryEvent coreActionHistoryEvent:
                        _historyEvents.Add(_history.AddCoreAction(coreActionHistoryEvent.CoreAction.Clone()));
                        break;
                    case BookmarkHistoryEvent bookmarkHistoryEvent:
                        _historyEvents.Add(_history.AddBookmark(e.Ticks, bookmarkHistoryEvent.Bookmark.Clone()));
                        break;
                }
            }

            SeekToBookmark(-1);
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

        private void SeekToBookmark(int bookmarkEventIndex)
        {
            int startIndex = 0;

            // First find the bookmark.
            if (bookmarkEventIndex == -1)
            {
                _core.Create(Core.LatestVersion, Core.Type.CPC6128);

                BlankScreen();
            }
            else
            {
                Bookmark bookmark = (_historyEvents[bookmarkEventIndex] as BookmarkHistoryEvent).Bookmark;
                _core.CreateFromBookmark(bookmark.Version, bookmark.State.GetBytes());

                SetScreen(bookmark.Screen.GetBytes());

                startIndex = bookmarkEventIndex;
            }

            for (int i = startIndex; i < _historyEvents.Count; i++)
            {
                HistoryEvent historyEvent = _historyEvents[i];
                if (historyEvent is CoreActionHistoryEvent coreActionHistoryEvent)
                {
                    PushRequest(MachineRequest.RunUntil(coreActionHistoryEvent.Ticks));
                    PushRequest(coreActionHistoryEvent.CoreAction);
                }
            }

            PushRequest(MachineRequest.RunUntil(_endTicks));
        }

        public void SeekToStart()
        {
            SeekToBookmark(-1);
        }

        public void SeekToPreviousBookmark()
        {
            int bookmarkIndex = _historyEvents.FindLastIndex(he => he.Ticks < _core.Ticks && he is BookmarkHistoryEvent);
            SeekToBookmark(bookmarkIndex);
        }

        public void SeekToNextBookmark()
        {
            int bookmarkIndex = _historyEvents.FindIndex(he => he.Ticks > _core.Ticks && he is BookmarkHistoryEvent);
            if (bookmarkIndex != -1)
            {
                SeekToBookmark(bookmarkIndex);
            }
        }
        protected override MachineRequest GetNextCoreRequest()
        {
            MachineRequest request = base.GetNextCoreRequest();
            if (request == null)
            {
                Stop();
            }

            return request;
        }

        protected override void CoreActionDone(MachineRequest request, MachineAction action)
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

                MachineAction action = MachineAction.CreateSnapshot(Ticks, coreSnapshot.Id);
                RaiseEvent(action);
            }

            base.BeginVSync();
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
    }
}
