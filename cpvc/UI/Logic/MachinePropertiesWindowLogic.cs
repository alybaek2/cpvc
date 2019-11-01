namespace CPvC
{
    /// <summary>
    /// Encapsulates the logic needed by the Machine Properties dialog.
    /// </summary>
    public class MachinePropertiesWindowLogic
    {
        private Machine _machine;

        public MachinePropertiesWindowLogic(Machine machine)
        {
            _machine = machine;
        }

        public void DeleteTimelines(System.Collections.IList items)
        {
            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.TrimTimeline(item.HistoryEvent);
                }
            }
        }

        public void DeleteBookmarks(System.Collections.IList items)
        {
            foreach (HistoryViewItem item in items)
            {
                if (item.HistoryEvent != null)
                {
                    _machine.SetBookmark(item.HistoryEvent, null);
                }
            }
        }

        public void RewriteMachineFile()
        {
            _machine.RewriteMachineFile();
        }
    }
}
