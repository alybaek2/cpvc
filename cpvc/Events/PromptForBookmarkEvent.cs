using System.ComponentModel;

namespace CPvC
{
    public class PromptForBookmarkEventArgs : HandledEventArgs
    {
        public PromptForBookmarkEventArgs()
        {
        }

        public HistoryEvent SelectedBookmark { get; set; }
    }

    public delegate void PromptForBookmarkEventHandler(object sender, PromptForBookmarkEventArgs e);
}
