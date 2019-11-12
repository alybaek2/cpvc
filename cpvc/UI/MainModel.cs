using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.UI
{
    /// <summary>
    /// Data model for the main window.
    /// </summary>
    public class MainModel
    {
        private ISettings _settings;

        public MainModel(ISettings settings, IFileSystem fileSystem)
        {
            _settings = settings;
            
            OpenMachines = new ObservableCollection<Machine>();
            ClosedMachines = new ObservableCollection<MachineInfo>();

            LoadFromSettings(fileSystem);
        }

        /// <summary>
        /// Represents the set of machines that are currently open.
        /// </summary>
        public ObservableCollection<Machine> OpenMachines { get; }

        /// <summary>
        /// Represents the set of machines not currently open, but were opened at some point prior.
        /// </summary>
        public ObservableCollection<MachineInfo> ClosedMachines { get; }

        /// <summary>
        /// Adds a machine to the model, by adding it to OpenMachines and removing the corresponding MachineInfo from ClosedMachines.
        /// </summary>
        /// <param name="machine">The machine to add.</param>
        public void AddMachine(Machine machine)
        {
            lock (OpenMachines)
            {
                OpenMachines.Add(machine);
            }

            lock (ClosedMachines)
            {
                List<MachineInfo> closedMachines = ClosedMachines.Where(m => String.Compare(m.Filepath, machine.Filepath, true) == 0).ToList();
                foreach (MachineInfo machineInfo in closedMachines)
                {
                    ClosedMachines.Remove(machineInfo);
                }
            }

            UpdateSettings();
        }

        /// <summary>
        /// Removes a machine from the mode, by removing it from OpenMachines and adding a corresponding MachineInfo to ClosedMachines.
        /// </summary>
        /// <param name="machine">The machine to remove.</param>
        public void RemoveMachine(Machine machine)
        {
            lock (OpenMachines)
            {
                OpenMachines.Remove(machine);
            }

            lock (ClosedMachines)
            {
                if (!ClosedMachines.Any(m => String.Compare(m.Filepath, machine.Filepath, true) == 0))
                {
                    ClosedMachines.Add(new MachineInfo(machine));
                }
            }

            UpdateSettings();
        }

        /// <summary>
        /// Updates the config settings with all the machines in OpenMachines and ClosedMachines.
        /// </summary>
        private void UpdateSettings()
        {
            Dictionary<string, MachineInfo> machineFilepathNameMap = new Dictionary<string, MachineInfo>();

            lock (OpenMachines)
            {
                foreach (Machine machine in OpenMachines)
                {
                    machineFilepathNameMap[machine.Filepath.ToLower()] = new MachineInfo(machine.Name, machine.Filepath, null);
                }
            }

            lock (ClosedMachines)
            {
                foreach (MachineInfo machineInfo in ClosedMachines)
                {
                    machineFilepathNameMap[machineInfo.Filepath.ToLower()] = machineInfo;
                }
            }

            _settings.RecentlyOpened = Helpers.JoinWithEscape(',', machineFilepathNameMap.Select(kv => kv.Value.AsString()));
        }

        /// <summary>
        /// Loads the model from the config settings.
        /// </summary>
        /// <param name="fileSystem">File system required by MachineInfo to load a thumbnail for each machine.</param>
        private void LoadFromSettings(IFileSystem fileSystem)
        {
            string recent = _settings.RecentlyOpened;
            if (recent != null)
            {
                lock (ClosedMachines)
                {
                    foreach (MachineInfo info in Helpers.SplitWithEscape(',', recent).Select(x => MachineInfo.FromString(x, fileSystem)).Where(y => y != null))
                    {
                        ClosedMachines.Add(info);
                    }
                }
            }
        }
    }
}
