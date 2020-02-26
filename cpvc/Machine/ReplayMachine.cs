using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CPvC
{
    public sealed class ReplayMachine : CoreMachine, IPausableMachine, ITurboableMachine, IPrerecordedMachine, IClosableMachine, INotifyPropertyChanged, IDisposable
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

            SeekToBookmark(null);
        }

        public string Name
        {
            get; set;
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        public void Close()
        {
            Core = null;
        }

        private void SeekToBookmark(Bookmark bookmark)
        {
            Core core = null;

            int version = -1;
            int startIndex = 0;

            // First find the bookmark.
            if (bookmark == null)
            {
                core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
                Display.GetFromBookmark(null);
            }
            else
            {
                for (int i = 0; i < _historyEvents.Count; i++)
                {
                    HistoryEvent historyEvent = _historyEvents[i];
                    if (historyEvent.CoreAction != null && historyEvent.CoreAction.Type == CoreRequest.Types.CoreVersion)
                    {
                        version = historyEvent.CoreAction.Version;
                    }
                    else if (historyEvent.Bookmark == bookmark)
                    {
                        core = Core.Create(version, historyEvent.Bookmark.State.GetBytes());
                        Display.GetFromBookmark(historyEvent.Bookmark);
                        startIndex = i + 1;
                        break;
                    }
                }

                // Need a check for core == null?
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
            if (running)
            {
                Core.Start();
            }
        }

        public void Start()
        {
            if (_core.Ticks < _endTicks)
            {
                _core.Start();
            }
        }

        public void Stop()
        {
            _core.Stop();
        }

        public void ToggleRunning()
        {
            if (_core.Running)
            {
                Stop();
            }
            else
            {
                Start();
            }
        }

        public void SeekToStart()
        {
            SeekToBookmark(null);
        }

        public void SeekToPreviousBookmark()
        {
            Bookmark bookmark = _historyEvents.Where(he => he.Ticks < _core.Ticks && he.Bookmark != null).Select(he => he.Bookmark).LastOrDefault();
            SeekToBookmark(bookmark);
        }

        public void SeekToNextBookmark()
        {
            Bookmark bookmark = _historyEvents.Where(he => he.Ticks > _core.Ticks && he.Bookmark != null).Select(he => he.Bookmark).FirstOrDefault();
            if (bookmark != null)
            {
                SeekToBookmark(bookmark);
            }
        }
    }
}
