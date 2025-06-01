using ListTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class FilteredHistory : BasicListTree
    {
        public FilteredHistory(History history) : base(history.RootEvent)
        {
            _history = history;

            Sync();

            _history.Auditors += History_Event;
        }

        private void Sync()
        {

        }

        private void History_Event(object sender, HistoryChangedEventArgs e)
        {

        }

        private History _history;
    }
}
