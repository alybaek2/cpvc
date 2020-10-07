using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineHistory
    {
        private HistoryEvent _rootEvent;
        private HistoryEvent _currentEvent;
        private Dictionary<int, HistoryEvent> _historyEventById;
        private int _nextEventId;

        public MachineHistory()
        {
            _rootEvent = null;
            _currentEvent = null;
            _historyEventById = new Dictionary<int, HistoryEvent>();
            _nextEventId = 0;
        }

        public HistoryEvent RootEvent
        {
            get
            {
                return _rootEvent;
            }

            set
            {
                _rootEvent = value;
            }
        }

        public HistoryEvent CurrentEvent
        {
            get
            {
                return _currentEvent;
            }

            set
            {
                _currentEvent = value;
            }
        }

        public int NextEventId()
        {
            return _nextEventId++;
        }

        public void AddEvent(HistoryEvent historyEvent)
        {
            if (RootEvent == null)
            {
                _rootEvent = historyEvent;
                _historyEventById[historyEvent.Id] = historyEvent;
            }
            else
            {
                // Special case for creating a parent event...
                if (historyEvent.Ticks < CurrentEvent.Ticks)
                {
                    AddParentCheckpoint(historyEvent);
                }
                else
                {
                    CurrentEvent.AddChild(historyEvent);
                    _historyEventById[historyEvent.Id] = historyEvent;
                }
            }

            _nextEventId = Math.Max(_nextEventId, historyEvent.Id + 1);

            CurrentEvent = historyEvent;
        }

        private void AddParentCheckpoint(HistoryEvent newParent)
        {
            HistoryEvent historyEvent = CurrentEvent;

            while (historyEvent.Parent != null)
            {
                if (historyEvent.Parent.Ticks > newParent.Ticks)
                {
                    historyEvent = historyEvent.Parent;
                }
                else
                {
                    historyEvent.Parent.AddChild(newParent);
                    historyEvent.Parent.RemoveChild(historyEvent);
                    newParent.AddChild(historyEvent);
                    _historyEventById[newParent.Id] = newParent;

                    break;
                }
            }
        }

        public bool DeleteEvent(int id)
        {
            if (_historyEventById.TryGetValue(id, out HistoryEvent historyEvent) && historyEvent != null)
            {
                return DeleteEvent(historyEvent);
            }

            return false;
        }

        /// <summary>
        /// Deletes an event (and all its children) without changing the current event.
        /// </summary>
        /// <param name="historyEvent">Event to delete.</param>
        /// <param name="loading">Indicates whether the MachineFile is being loaded from a file.</param>
        public bool DeleteEvent(HistoryEvent historyEvent)
        {
            if (historyEvent.Parent == null)
            {
                return false;
            }

            historyEvent.Parent.RemoveChild(historyEvent);

            // Remove the event and all its descendents from the lookup.
            foreach (HistoryEvent e in historyEvent.GetSelfAndDescendents())
            {
                _historyEventById.Remove(e.Id);
            }

            return true;
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
    }
}
