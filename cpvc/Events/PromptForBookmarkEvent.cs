using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class PromptForBookmarkEventArgs : EventArgs
    {
        public PromptForBookmarkEventArgs()
        {
        }

        public HistoryEvent SelectedBookmark { get; set; }
    }

    public delegate void PromptForBookmarkEventHandler(object sender, PromptForBookmarkEventArgs e);
}
