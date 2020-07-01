using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CPvC
{
    public sealed class ReplayMachine : CoreMachine,
        IPausableMachine,
        ITurboableMachine,
        IPrerecordedMachine,
        IClosableMachine,
        INotifyPropertyChanged,
        IDisposable
    {
        private UInt64 _endTicks;

        private List<HistoryEvent> _historyEvents;

        public UInt64 EndTicks
        {
            get
            {
                return _endTicks;
            }
        }

        public OnCloseDelegate OnClose { get; set; }

        public ReplayMachine(HistoryEvent historyEvent)
        {
            Display = new Display();

            _endTicks = historyEvent.Ticks;
            OnPropertyChanged("EndTicks");

            _historyEvents = new List<HistoryEvent>();

            while (historyEvent != null)
            {
                _historyEvents.Insert(0, historyEvent.CloneWithoutChildren());

                historyEvent = historyEvent.Parent;
            }

            SeekToBookmark(-1);
        }

        public override string Name
        {
            get; set;
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        public bool CanClose()
        {
            return true;
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
                if (historyEvent.Type == HistoryEvent.Types.CoreAction)
                {
                    core.PushRequest(CoreRequest.RunUntilForce(historyEvent.Ticks));
                    core.PushRequest(historyEvent.CoreAction);
                }
            }

            core.PushRequest(CoreRequest.RunUntilForce(_endTicks));
            core.PushRequest(CoreRequest.Quit());

            bool running = Core?.Running ?? false;
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
