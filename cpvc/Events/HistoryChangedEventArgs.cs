using System;
using System.Collections.Generic;

namespace CPvC
{
    public class HistoryChangedEventArgs : EventArgs
    {
        public HistoryChangedEventArgs(History history, HistoryEvent historyEvent, HistoryChangedAction action, HistoryEvent originalParentEvent, List<HistoryEvent> originalChildrenEvents, HistoryEvent originalCurrent)
        {
            History = history;
            HistoryEvent = historyEvent;
            Action = action;
            OriginalParentEvent = originalParentEvent;
            OriginalChildrenEvents = originalChildrenEvents;
            OriginalCurrent = originalCurrent;
        }

        public History History { get; }
        public HistoryChangedAction Action { get; }
        public HistoryEvent HistoryEvent { get; }
        public HistoryEvent OriginalParentEvent { get; }
        public List<HistoryEvent> OriginalChildrenEvents { get; }
        public HistoryEvent OriginalCurrent { get; }
    }

    public delegate void HistoryChangedEventHandler(object sender, HistoryChangedEventArgs e);
}
