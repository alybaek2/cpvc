using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface IMachineFileReader
    {
        void SetName(string name);
        void DeleteEvent(int id);
        void SetBookmark(int id, Bookmark bookmark);
        void SetCurrentEvent(int id);
        void AddHistoryEvent(HistoryEvent historyEvent);
    }
}
