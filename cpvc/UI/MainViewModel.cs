using System;
using System.ComponentModel;
using System.Linq;

namespace CPvC.UI
{
    /// <summary>
    /// View Model for the main window.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private object _active;

        public MainViewModel(ISettings settings, IFileSystem fileSystem)
        {
            Model = new MainModel(settings, fileSystem);
        }

        /// <summary>
        /// The data model associated with this view model.
        /// </summary>
        /// <remarks>
        /// It might be questionable to expose the model directly to the view, but doing so reduces the numbder of passthrough
        /// methods and properties in the view model.
        /// </remarks>
        public MainModel Model { get; }

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
                OnPropertyChanged("ActiveItem");
                OnPropertyChanged("ActiveMachine");
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void NewMachine(string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                return;
            }

            string machineName = System.IO.Path.GetFileNameWithoutExtension(filepath);

            Machine machine = null;

            try
            {
                machine = Machine.New(machineName, filepath, fileSystem);
            }
            catch (Exception ex)
            {
                Diagnostics.Trace("[MainViewModel.NewMachine] Exception caught: {0}", ex.Message);
                if (machine != null)
                {
                    machine.Dispose();
                }

                throw;
            }

            AddMachine(machine);
            ActiveMachine = machine;
            machine.Start();
        }

        public void OpenMachine(string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                return;
            }

            Machine machine = null;

            try
            {
                machine = Machine.Open(filepath, fileSystem);
            }
            catch (Exception ex)
            {
                Diagnostics.Trace("[MainViewModel.OpenMachine] Exception caught: {0}", ex.Message);
                if (machine != null)
                {
                    machine.Dispose();
                }

                throw;
            }

            AddMachine(machine);
            ActiveMachine = machine;
        }

        public void Close()
        {
            Close(ActiveMachine);
        }

        public void Close(Machine machine)
        {
            CloseMachine(machine);
            machine.Close();
        }

        public void Remove(Machine machine)
        {
            Model.Remove(machine);
            machine.Close();
        }

        public void Remove(MachineInfo machineInfo)
        {
            Model.Remove(machineInfo);
        }

        public void CloseAll()
        {
            // Make a copy of Machines since Close will be removing elements from it.
            foreach (Machine machine in Model.OpenMachines.ToList())
            {
                Close(machine);
            }
        }

        public void LoadDisc(byte drive, byte[] image)
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                ActiveMachine.LoadDisc(drive, image);
            }
        }

        public void LoadTape(byte[] image)
        {
            if (ActiveMachine == null)
            {
                return;
            }

            using (ActiveMachine.AutoPause())
            {
                ActiveMachine.LoadTape(image);
            }
        }

        public void Key(byte key, bool down)
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Key(key, down);
            }
        }

        public void Reset()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Reset();
            }
        }

        public void Pause()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Stop();
            }
        }

        public void Resume()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.Start();
            }
        }

        public void ToggleRunning(Machine machine)
        {
            if (machine != null)
            {
                machine.ToggleRunning();
            }
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

        public void AddBookmark()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.AddBookmark(false);
            }
        }

        public void SeekToLastBookmark()
        {
            if (ActiveMachine != null)
            {
                ActiveMachine.SeekToLastBookmark();
            }
        }

        public void CompactFile()
        {
            if (ActiveMachine == null)
            {
                return;
            }

            ActiveMachine.RewriteMachineFile();
        }

        public int ReadAudio(byte[] buffer, int offset, int samplesRequested)
        {
            lock (Model.OpenMachines)
            {
                int samplesWritten = 0;
                foreach (Machine machine in Model.OpenMachines)
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

        private void AddMachine(Machine machine)
        {
            Model.OpenMachine(machine);
        }

        private void CloseMachine(Machine machine)
        {
            Model.CloseMachine(machine);
        }
    }
}
