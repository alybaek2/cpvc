using System;

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
