using System.Collections.Generic;

namespace CPvC
{
    /// <summary>
    /// User interface operations required by the MainWindowLogic class.
    /// </summary>
    public interface IUserInterface
    {
        string SelectItem(List<string> items);
        string PromptForFile(FileTypes type, bool existing);

        HistoryEvent PromptForBookmark();

        void ReportError(string message);
    }
}
