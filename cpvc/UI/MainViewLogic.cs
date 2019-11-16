using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

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
    public class MainViewLogic : INotifyPropertyChanged
    {
        public delegate string PromptForFileDelegate(FileTypes type, bool existing);
        public delegate string SelectItemDelegate(List<string> items);
        public delegate HistoryEvent PromptForBookmarkDelegate();
        public delegate string PromptForNameDelegate(string existingName);

        private object _active;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewLogic(MainViewModel mainViewModel)
        {
            ViewModel = mainViewModel;
        }

        public MainViewModel ViewModel { get; }

        /// <summary>
        /// Represents the currently active item in the main window. Corresponds to the DataContext associated with the currently
        /// selected tab in the main window.
        /// </summary>
        public object ActiveItem
        {
            get
            {
                return _active;
            }

            set
            {
                _active = value;
                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachine");
            }
        }

        /// <summary>
        /// If the currently selected tab corresponds to a Machine, this property will be a reference to that machine. Otherwise, this property is null.
        /// </summary>
        public Machine ActiveMachine
        {
            get
            {
                return _active as Machine;
            }

            set
            {
                _active = value;

                if (_active is Machine machine && machine.RequiresOpen)
                {
                    machine.Open();
                }

                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachine");
            }
        }

        public void NewMachine(IFileSystem fileSystem, PromptForFileDelegate promptForFile)
        {
            string filepath = promptForFile(FileTypes.Machine, false);
            if (filepath == null)
            {
                return;
            }

            Machine machine = ViewModel.NewMachine(filepath, fileSystem);
            if (machine != null)
            {
                ActiveMachine = machine;
            }
        }

        public void OpenMachine(string filepath, IFileSystem fileSystem, PromptForFileDelegate promptForFile)
        {
            if (filepath == null)
            {
                filepath = promptForFile(FileTypes.Machine, true);
                if (filepath == null)
                {
                    return;
                }
            }

            Machine machine = ViewModel.OpenMachine(filepath, fileSystem);
            if (machine != null)
            {
                ActiveMachine = machine;
            }
        }

        public void LoadDisc(byte drive, IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            Machine machine = ActiveMachine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                byte[] image = PromptForMedia(FileTypes.Disc, fileSystem, promptForFile, selectItem);
                if (image != null)
                {
                    ViewModel.LoadDisc(ActiveMachine, drive, image);
                }
            }
        }

        public void LoadTape(IFileSystem fileSystem, PromptForFileDelegate promptForFile, SelectItemDelegate selectItem)
        {
            Machine machine = ActiveMachine;
            if (machine == null)
            {
                return;
            }

            using (machine.AutoPause())
            {
                byte[] image = PromptForMedia(FileTypes.Tape, fileSystem, promptForFile, selectItem);
                if (image != null)
                {
                    ViewModel.LoadTape(ActiveMachine, image);
                }
            }
        }

        public void SelectBookmark(PromptForBookmarkDelegate promptForBookmark)
        {
            Machine machine = ActiveMachine;
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
            using (ActiveMachine.AutoPause())
            {
                string newName = promptForName(ActiveMachine.Name);
                if (newName != null)
                {
                    ActiveMachine.Name = newName;
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

        public void Key(byte key, bool down)
        {
            ActiveMachine?.Key(key, down);
        }

        public void Close()
        {
            ViewModel.Close(ActiveMachine);
        }

        public void Reset()
        {
            ActiveMachine?.Reset();
        }

        public void Pause()
        {
            ActiveMachine?.Stop();
        }

        public void Resume()
        {
            ActiveMachine?.Start();
        }

        public void LoadDisc(byte drive, byte[] image)
        {
            ViewModel.LoadDisc(ActiveMachine, drive, image);
        }

        public void LoadTape(byte[] image)
        {
            ViewModel.LoadTape(ActiveMachine, image);
        }

        public void CompactFile()
        {
            ActiveMachine?.RewriteMachineFile();
        }

        public void AddBookmark()
        {
            ActiveMachine?.AddBookmark(false);
        }

        public void SeekToLastBookmark()
        {
            ActiveMachine?.SeekToLastBookmark();
        }

        public void EnableTurbo(bool enabled)
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                ActiveMachine.EnableTurbo(enabled);
            }
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (ViewModel.Machines)
            {
                int samplesWritten = 0;
                foreach (Machine machine in ViewModel.Machines)
                {
                    // Play audio only from the currently active machine; for the rest, just
                    // advance the audio playback position.
                    if (machine == ActiveMachine)
                    {
                        samplesWritten = machine.ReadAudio(buffer, offset, samplesRequested);
                    }
                    else
                    {
                        machine.AdvancePlayback(samplesRequested);
                    }
                }

                return samplesWritten;
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
