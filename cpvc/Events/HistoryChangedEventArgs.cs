using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class HistoryChangedEventArgs : EventArgs
    {
        public HistoryChangedEventArgs(History history, HistoryEvent historyEvent, HistoryChangedAction action)
        {
            History = history;
            Event = historyEvent;
            Action = action;
        }

        public History History { get; }
        public HistoryChangedAction Action { get; }
        public HistoryEvent Event { get; }
    }

    public delegate void HistoryChangedEventHandler(object sender, HistoryChangedEventArgs e);
}
