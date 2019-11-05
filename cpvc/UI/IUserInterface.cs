using System.Collections.Generic;

namespace CPvC
{
    /// <summary>
    /// User interface operations required by the MainWindowLogic class.
    /// </summary>
    public interface IUserInterface
    {
        void AddMachine(Machine machine);
        void RemoveMachine(Machine machine);
        string SelectItem(List<string> items);
        string PromptForFile(FileTypes type, bool existing);

        Machine GetActiveMachine();

        void OpenProperties();
        HistoryEvent PromptForBookmark();

        void ReportError(string message);
    }
}
