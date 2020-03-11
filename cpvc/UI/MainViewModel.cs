using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace CPvC.UI
{
    /// <summary>
    /// View Model for the main window.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Delegates for calling back into MainWindow.
        public delegate string SelectItemDelegate(List<string> items);
        public delegate string PromptForFileDelegate(FileTypes type, bool existing);
        public delegate HistoryEvent PromptForBookmarkDelegate();
        public delegate string PromptForNameDelegate(string existingName);

        /// <summary>
        /// The data model associated with this view model.
        /// </summary>
        private readonly MainModel _model;

        private ObservableCollection<MachineViewModel> _machineViewModels;

        /// <summary>
        /// The currently active item. Will be either this instance (the Home tab) or a Machine.
        /// </summary>
        private object _active;

        private IFileSystem _fileSystem;
        private SelectItemDelegate _selectItem;
        private PromptForFileDelegate _promptForFile;
        private PromptForBookmarkDelegate _promptForBookmark;
        private PromptForNameDelegate _promptForName;

        private MachineViewModel _nullMachineViewModel;
        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel(ISettings settings, IFileSystem fileSystem, SelectItemDelegate selectItem, PromptForFileDelegate promptForFile, PromptForBookmarkDelegate promptForBookmark, PromptForNameDelegate promptForName)
        {
            _fileSystem = fileSystem;
            _selectItem = selectItem;
            _promptForFile = promptForFile;
            _promptForBookmark = promptForBookmark;
            _promptForName = promptForName;

            _nullMachineViewModel = new MachineViewModel(null, null, null, null, null, null);

            _model = new MainModel(settings, fileSystem);

            _machineViewModels = new ObservableCollection<MachineViewModel>();
            for (int i = 0; i < _model.Machines.Count; i++)
            {
                _machineViewModels.Add(new MachineViewModel(_model.Machines[i], fileSystem, promptForFile, promptForBookmark, promptForName, selectItem));
            }

            ActiveItem = this;
        }

        public ObservableCollection<Machine> Machines
        {
            get
            {
                return _model.Machines;
            }
        }

        public ObservableCollection<MachineViewModel> MachineViewModels
        {
            get
            {
                return _machineViewModels;
            }
        }

        public ObservableCollection<ReplayMachine> ReplayMachines
        {
            get
            {
                return _model.ReplayMachines;
            }
        }

        public MachineViewModel ActiveMachineViewModel
        {
            get
            {
                for (int i = 0; i < _machineViewModels.Count; i++)
                {
                    if (_machineViewModels[i].Machine == (_active as ICoreMachine))
                    {
                        return _machineViewModels[i];
                    }
                }

                return _nullMachineViewModel;
            }
        }

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
                OnPropertyChanged("ActiveMachineViewModel");
            }
        }

        /// <summary>
        /// If the currently selected tab corresponds to a Machine, this property will be a reference to that machine. Otherwise, this property is null.
        /// </summary>
        public ICoreMachine ActiveMachine
        {
            get
            {
                return _active as ICoreMachine;
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
                OnPropertyChanged("ActiveMachineViewModel");
            }
        }

        public void OpenReplayMachine(Machine machine, HistoryEvent finalEvent)
        {
            ReplayMachine replayMachine = new ReplayMachine(finalEvent);
            replayMachine.Name = String.Format("{0} (Replay)", machine.Name);
            ReplayMachines.Add(replayMachine);

            MachineViewModel machineViewModel = new MachineViewModel(replayMachine, _fileSystem, null, null, null, null);
            _machineViewModels.Add(machineViewModel);

            ActiveMachine = replayMachine;
        }

        public Machine NewMachine(PromptForFileDelegate promptForFile, IFileSystem fileSystem)
        {
            string filepath = promptForFile(FileTypes.Machine, false);

            Machine machine = _model.Add(filepath, fileSystem);
            if (machine != null)
            {
                machine.Start();
                _machineViewModels.Add(new MachineViewModel(machine, _fileSystem, _promptForFile, _promptForBookmark, _promptForName, _selectItem));
                ActiveMachine = machine;
            }

            return machine;
        }

        public Machine OpenMachine(PromptForFileDelegate promptForFile, string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                filepath = promptForFile(FileTypes.Machine, true);
            }

            Machine machine = _model.Add(filepath, fileSystem);
            if (machine != null)
            {
                _machineViewModels.Add(new MachineViewModel(machine, _fileSystem, _promptForFile, _promptForBookmark, _promptForName, _selectItem));
                ActiveMachine = machine;
            }

            return machine;
        }

        public void EnableTurbo(bool enabled)
        {
            (ActiveMachine as ITurboableMachine)?.EnableTurbo(enabled);
        }

        public void Close(ICoreMachine machine)
        {
            ((machine ?? ActiveMachine) as IClosableMachine)?.Close();
        }

        public void Key(byte key, bool down)
        {
            (ActiveMachine as IInteractiveMachine)?.Key(key, down);
        }

        public void Remove(MachineViewModel viewModel)
        {
            _model.Remove(viewModel.Machine as Machine);
            _machineViewModels.Remove(viewModel);
            viewModel.CloseCommand.Execute(null);
        }

        public void CloseAll()
        {
            foreach (Machine machine in _model.Machines)
            {
                machine.Close();
            }

            foreach (ReplayMachine machine in _model.ReplayMachines)
            {
                machine.Close();
            }
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (Machines)
            {
                int samplesWritten = 0;
                foreach (Machine machine in Machines)
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
                foreach (ReplayMachine replayMachine in ReplayMachines)
                {
                    // Play audio only from the currently active machine; for the rest, just
                    // advance the audio playback position.
                    if (replayMachine == ActiveItem)
                    {
                        samplesWritten = replayMachine.ReadAudio(buffer, offset, samplesRequested);
                    }
                    else
                    {
                        replayMachine.Core.AdvancePlayback(samplesRequested);
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
