using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CPvC
{
    public sealed class ReplayMachine : CoreMachine,
        ICoreMachine,
        IPausableMachine,
        ITurboableMachine,
        IPrerecordedMachine,
        INotifyPropertyChanged
    {
        private UInt64 _endTicks;

        private List<HistoryEvent> _historyEvents;

        private MachineHistory _history;

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

            _history = new MachineHistory();
            List<HistoryEvent> historyEvents = new List<HistoryEvent>();

            while (historyEvent != null)
            {
                historyEvents.Insert(0, historyEvent);

                historyEvent = historyEvent.Parent;
            }

            _historyEvents = new List<HistoryEvent>();

            foreach (HistoryEvent e in historyEvents)
            {
                switch (e.Type)
                {
                    case HistoryEventType.AddCoreAction:
                        _historyEvents.Add(_history.AddCoreAction(e.CoreAction.Clone()));
                        break;
                    case HistoryEventType.AddBookmark:
                        _historyEvents.Add(_history.AddBookmark(e.Ticks, e.Bookmark.Clone()));
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
                return RunningState == RunningState.Paused;
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
                Bookmark bookmark = _historyEvents[bookmarkEventIndex].Bookmark;
                core = Core.Create(bookmark.Version, bookmark.State.GetBytes());
                Display.GetFromBookmark(bookmark);
                startIndex = bookmarkEventIndex;
            }

            for (int i = startIndex; i < _historyEvents.Count; i++)
            {
                HistoryEvent historyEvent = _historyEvents[i];
                if (historyEvent.Type == HistoryEventType.AddCoreAction)
                {
                    core.PushRequest(CoreRequest.RunUntil(historyEvent.Ticks));
                    core.PushRequest(historyEvent.CoreAction);
                }
            }

            core.PushRequest(CoreRequest.RunUntil(_endTicks));

            // This shouldn't be necessary since IdleRequest is set to null. Can probably get
            // rid of CoreAction.Quit altogether, actually.
            core.Quit();

            Core = core;
            Core.Auditors = RequestProcessed;

            SetCoreRunning();
        }

        public void SeekToStart()
        {
            SeekToBookmark(-1);
        }

        public void SeekToPreviousBookmark()
        {
            int bookmarkIndex = _historyEvents.FindLastIndex(he => he.Ticks < _core.Ticks && he.Bookmark != null);
            SeekToBookmark(bookmarkIndex);
        }

        public void SeekToNextBookmark()
        {
            int bookmarkIndex = _historyEvents.FindIndex(he => he.Ticks > _core.Ticks && he.Bookmark != null);
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
    }
}
