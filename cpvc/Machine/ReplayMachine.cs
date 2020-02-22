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
        private DoubleCollection _bookmarkTicks;

        public double EndTicks
        {
            get
            {
                return _endTicks;
            }
        }

        public DoubleCollection BookmarkTicks
        {
            get
            {
                return _bookmarkTicks;
            }
        }

        public ReplayMachine(HistoryEvent historyEvent)
        {
            Display = new Display();

            _endTicks = historyEvent.Ticks;
            OnPropertyChanged("EndTicks");

            _historyEvents = new List<HistoryEvent>();
            _bookmarkTicks = new DoubleCollection();

            while (historyEvent != null)
            {
                _historyEvents.Insert(0, historyEvent.CloneWithoutChildren());

                if (historyEvent.Bookmark != null)
                {
                    if (_bookmarkTicks.Count == 0 || _bookmarkTicks[0] != historyEvent.Ticks)
                    {
                        _bookmarkTicks.Insert(0, historyEvent.Ticks);
                    }
                }

                historyEvent = historyEvent.Parent;
            }

            SeekToBookmark(0);
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
            _core?.Dispose();
        }

        private void SeekToBookmark(int historyEventIndex)
        {
            Core core = null;

            int version = -1;

            for (int i = 0; i < _historyEvents.Count; i++)
            {
                HistoryEvent historyEvent = _historyEvents[i];

                if (historyEvent.CoreAction != null && historyEvent.CoreAction.Type == CoreRequest.Types.CoreVersion)
                {
                    version = historyEvent.CoreAction.Version;
                }
                
                if (historyEventIndex == i)
                {
                    if (i == 0)
                    {
                        core = Core.Create(version, Core.Type.CPC6128);
                        Display.GetFromBookmark(null);
                    }
                    else
                    {
                        if (historyEvent.Bookmark != null)
                        {
                            core = Core.Create(version, historyEvent.Bookmark.State.GetBytes());
                            Display.GetFromBookmark(historyEvent.Bookmark);
                        }
                        else
                        {
                            return;
                        }
                    }
                }
                else if (i == (_historyEvents.Count - 1))
                {
                    core.PushRequest(CoreRequest.RunUntilForce(historyEvent.Ticks));
                    core.PushRequest(CoreRequest.Quit());
                }
                else if (i > historyEventIndex)
                {
                    if (historyEvent.Type == HistoryEvent.Types.CoreAction)
                    {
                        core.PushRequest(CoreRequest.RunUntilForce(historyEvent.Ticks));
                        core.PushRequest(historyEvent.CoreAction);
                    }
                }
            }

            bool running = Core?.Running ?? false;
            Core = core;
            if (running)
            {
                Core.Start();
            }
        }

        public void EnableTurbo(bool enabled)
        {
            _core.EnableTurbo(enabled);
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
            SeekToBookmark(0);
        }

        public void SeekToPreviousBookmark()
        {
            int historyIndex = _historyEvents.FindLastIndex(he => he != null && he.Ticks < _core.Ticks && he.Bookmark != null);
            if (historyIndex != -1)
            {
                SeekToBookmark(historyIndex);
            }
        }

        public void SeekToNextBookmark()
        {
            int historyIndex = _historyEvents.FindIndex(he => he != null && he.Ticks > _core.Ticks && he.Bookmark != null);
            if (historyIndex != -1)
            {
                SeekToBookmark(historyIndex);
            }
        }
    }
}
