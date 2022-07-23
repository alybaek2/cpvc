﻿using System;
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
                return ActualRunningState == RunningState.Paused && Ticks < EndTicks;
            }
        }

        public bool CanStop
        {
            get
            {
                return ActualRunningState == RunningState.Running;
            }
        }

        public bool CanClose
        {
            get
            {
                return true;
            }
        }

        public new void Start()
        {
            // Would a better test be to see if the core has outstanding requests?
            if (Ticks < _endTicks)
            {
                base.Start();
            }
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
                    PushRequest(CoreRequest.RunUntil(coreActionHistoryEvent.Ticks));
                    PushRequest(coreActionHistoryEvent.CoreAction);
                }
            }

            PushRequest(CoreRequest.RunUntil(_endTicks));

            Stop();
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
        protected override CoreRequest GetNextRequest()
        {
            CoreRequest request = base.GetNextRequest();
            if (request == null)
            {
                Stop();
            }

            return request;
        }

        protected override void CoreActionDone(CoreRequest request, CoreAction action)
        {
            RaiseEvent(action);
        }

        protected override void OnPropertyChanged(string name)
        {
            if (name == nameof(ActualRunningState))
            {
                base.OnPropertyChanged(nameof(CanStart));
                base.OnPropertyChanged(nameof(CanStop));
            }

            base.OnPropertyChanged(name);
        }
    }
}
