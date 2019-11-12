using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.UI
{
    /// <summary>
    /// Encapsulates logic needed by the main view, but not necessarily part of the main view model.
    /// </summary>
    /// <remarks>
    /// Due to the fact that MainWindow.xaml.cs represents an actual UI window, no unit tests currently exist for that class. Therefore, most of the code
    /// was moved to this class to allow it to be easily unit tested. Interaction with the user is handled by delegates here, which was be easily mocked
    /// for sake of testing. Notice that most of the code left in MainWindow is just passing straight through to MainViewModel or MainViewLogic.
    /// </remarks>
    public class MainViewLogic
    {
        public delegate string PromptForFileDelegate(FileTypes type, bool existing);
        public delegate string SelectItemDelegate(List<string> items);
        public delegate HistoryEvent PromptForBookmarkDelegate();
        public delegate string PromptForNameDelegate(string existingName);
        public delegate void ReportErrorDelegate(string message);

        private MainViewModel _mainViewModel;

        public MainViewLogic(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
        }

        public void NewMachine(IFileSystem fileSystem, PromptForFileDelegate promptForFile, ReportErrorDelegate reportError)
        {
            string filepath = promptForFile(FileTypes.Machine, false);
            if (filepath == null)
            {
                return;
            }

            try
            {
                _mainViewModel.NewMachine(filepath, fileSystem);
            }
            catch (Exception ex)
            {
                reportError(ex.Message);
            }
        }

        public void OpenMachine(string filepath, IFileSystem fileSystem, PromptForFileDelegate promptForFile, ReportErrorDelegate reportError)
        {
            if (filepath == null)
            {
                filepath = promptForFile(FileTypes.Machine, true);
                if (filepath == null)
                {
                    return;
                }
            }

            try
            {
                _mainViewModel.OpenMachine(filepath, fileSystem);
            }
            catch (Exception ex)
            {
                reportError(ex.Message);
            }
        }

        public void LoadDisc(byte drive, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            byte[] image = PromptForMedia(FileTypes.Disc, fileSystem, promptForFile, selectItem);
            if (image != null)
            {
                _mainViewModel.LoadDisc(drive, image);
            }
        }

        public void LoadTape(IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            byte[] image = PromptForMedia(FileTypes.Tape, fileSystem, promptForFile, selectItem);
            if (image != null)
            {
                _mainViewModel.LoadTape(image);
            }
        }

        public void SelectBookmark(PromptForBookmarkDelegate promptForBookmark)
        {
            Machine machine = _mainViewModel.ActiveMachine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                HistoryEvent historyEvent = promptForBookmark();
                if (historyEvent != null)
                {
                    machine.SetCurrentEvent(historyEvent);
                }
            }
        }

        public void RenameMachine(PromptForNameDelegate promptForName)
        {
            Machine machine = _mainViewModel.ActiveMachine;
            using (machine.AutoPause())
            {
                string newName = promptForName(machine.Name);
                if (newName != null)
                {
                    machine.Name = newName;
                }
            }
        }

        private byte[] PromptForMedia(FileTypes type, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            string expectedExt;
            switch (type)
            {
                case FileTypes.Disc:
                    expectedExt = ".dsk";
                    break;
                case FileTypes.Tape:
                    expectedExt = ".cdt";
                    break;
                case FileTypes.Machine:
                    expectedExt = ".cpvc";
                    break;
                default:
                    throw new Exception(String.Format("Unknown FileTypes value {0}.", type));
            }

            string filename = promptForFile(type, true);
            if (filename == null)
            {
                // Action was cancelled by the user.
                return null;
            }

            byte[] buffer = null;
            string ext = System.IO.Path.GetExtension(filename);
            if (ext.ToLower() == ".zip")
            {
                string entry = null;

                List<string> entries = fileSystem.GetZipFileEntryNames(filename);

                List<string> extEntries = entries.Where(x => System.IO.Path.GetExtension(x).ToLower() == expectedExt).ToList();
                if (extEntries.Count == 0)
                {
                    // No images available.
                    Diagnostics.Trace("No files with the extension \"{0}\" found in zip archive \"{1}\".", expectedExt, filename);

                    return null;
                }
                else if (extEntries.Count == 1)
                {
                    // Don't bother prompting the user, since there's only one!
                    entry = extEntries[0];
                }
                else
                {
                    entry = selectItem(entries);
                    if (entry == null)
                    {
                        // Action was cancelled by the user.
                        return null;
                    }
                }

                Diagnostics.Trace("Loading \"{0}\" from zip archive \"{1}\"", entry, filename);
                buffer = fileSystem.GetZipFileEntry(filename, entry);
            }
            else
            {
                Diagnostics.Trace("Loading \"{0}\"", filename);
                buffer = fileSystem.ReadBytes(filename);
            }

            return buffer;
        }
    }
}
