using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CPvC.UI
{
    /// <summary>
    /// View Model for the main window.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The data model associated with this view model.
        /// </summary>
        private readonly MainModel _model;

        public MainViewModel(ISettings settings, IFileSystem fileSystem)
        {
            _model = new MainModel(settings, fileSystem);
        }

        public ObservableCollection<Machine> Machines
        {
            get
            {
                return _model.Machines;
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Machine NewMachine(string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                return null;
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
                machine?.Dispose();

                throw;
            }

            _model.Add(machine);
            machine.Start();

            return machine;
        }

        public Machine OpenMachine(string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                return null;
            }

            Machine machine = null;

            try
            {
                machine = Machine.Open(null, filepath, fileSystem, false);
            }
            catch (Exception ex)
            {
                Diagnostics.Trace("[MainViewModel.OpenMachine] Exception caught: {0}", ex.Message);
                machine?.Dispose();

                throw;
            }

            _model.Add(machine);

            return machine;
        }

        public void Close(Machine machine)
        {
            machine.Close();
        }

        public void Remove(Machine machine)
        {
            _model.Remove(machine);
            machine.Close();
        }

        public void CloseAll()
        {
            foreach (Machine machine in _model.Machines)
            {
                Close(machine);
            }
        }

        public void LoadDisc(Machine machine, byte drive, byte[] image)
        {
            machine?.LoadDisc(drive, image);
        }

        public void LoadTape(Machine machine, byte[] image)
        {
            machine?.LoadTape(image);
        }

        public void ToggleRunning(Machine machine)
        {
            machine?.ToggleRunning();
        }
    }
}
