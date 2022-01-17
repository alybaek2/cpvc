using System;
using System.Collections.Generic;
using System.ComponentModel;

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

        public ReplayMachine(HistoryEvent historyEvent)
        {
            Display = new Display();

            _endTicks = historyEvent.Ticks;
            OnPropertyChanged("EndTicks");

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

        public void Close()
        {
            Core = null;
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
            Core core;

            int startIndex = 0;

            // First find the bookmark.
            if (bookmarkEventIndex == -1)
            {
                core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
                Display.GetFromBookmark(null);
            }
            else
            {
                Bookmark bookmark = (_historyEvents[bookmarkEventIndex] as BookmarkHistoryEvent).Bookmark;
                core = Core.Create(bookmark.Version, bookmark.State.GetBytes());
                Display.GetFromBookmark(bookmark);
                startIndex = bookmarkEventIndex;
            }

            for (int i = startIndex; i < _historyEvents.Count; i++)
            {
                HistoryEvent historyEvent = _historyEvents[i];
                if (historyEvent is CoreActionHistoryEvent coreActionHistoryEvent)
                {
                    core.PushRequest(CoreRequest.RunUntil(coreActionHistoryEvent.Ticks));
                    core.PushRequest(coreActionHistoryEvent.CoreAction);
                }
            }

            core.PushRequest(CoreRequest.RunUntil(_endTicks));

            Core = core;
            Core.Auditors = RequestProcessed;
            Core.IdleRequest += IdleRequest;

            SetCoreRunning();
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

        /// <summary>
        /// Delegate for logging core actions.
        /// </summary>
        /// <param name="core">The core the request was made for.</param>
        /// <param name="request">The original request.</param>
        /// <param name="action">The action taken.</param>
        private void RequestProcessed(Core core, CoreRequest request, CoreAction action)
        {
            Auditors?.Invoke(action);
        }

        private CoreRequest IdleRequest()
        {
            Stop();

            return null;
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
