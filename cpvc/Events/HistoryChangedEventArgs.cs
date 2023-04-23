using System;
using System.Collections.Generic;

namespace CPvC
{
    public class HistoryChangedEventArgs : EventArgs
    {
        public HistoryChangedEventArgs(History history, HistoryEvent historyEvent, HistoryChangedAction action, HistoryEvent originalParentEvent, List<HistoryEvent> originalChildrenEvents)
        {
            History = history;
            HistoryEvent = historyEvent;
            Action = action;
            OriginalParentEvent = originalParentEvent;
            OriginalChildrenEvents = originalChildrenEvents;
        }

        public History History { get; }
        public HistoryChangedAction Action { get; }
        public HistoryEvent HistoryEvent { get; }
        public HistoryEvent OriginalParentEvent { get; }
        public List<HistoryEvent> OriginalChildrenEvents { get; }
    }

    public delegate void HistoryChangedEventHandler(object sender, HistoryChangedEventArgs e);
}
