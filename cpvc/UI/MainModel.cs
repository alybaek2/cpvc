using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace CPvC.UI
{
    /// <summary>
    /// Data model for the main window.
    /// </summary>
    public class MainModel
    {
        private readonly ISettings _settings;

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
        /// Adds a machine to OpenMachines and removes the corresponding MachineInfo from ClosedMachines.
        /// </summary>
        /// <param name="machine">The machine to add.</param>
        public void OpenMachine(Machine machine)
        {
            lock (OpenMachines)
            {
                OpenMachines.Add(machine);
            }

            RemoveFromClosed(machine.Filepath);

            UpdateSettings();
        }

        /// <summary>
        /// Removes a machine from OpenMachines and adds a corresponding MachineInfo to ClosedMachines.
        /// </summary>
        /// <param name="machine">The machine to remove.</param>
        public void CloseMachine(Machine machine)
        {
            RemoveFromOpen(machine);

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
        /// Removes a machine from the model.
        /// </summary>
        /// <param name="machine">The machine to remove.</param>
        public void Remove(Machine machine)
        {
            RemoveFromOpen(machine);
            RemoveFromClosed(machine.Filepath);

            UpdateSettings();
        }

        /// <summary>
        /// Removes a closed machine from the model.
        /// </summary>
        /// <param name="machineInfo">The closed machine to remove.</param>
        public void Remove(MachineInfo machineInfo)
        {
            RemoveFromClosed(machineInfo.Filepath);

            UpdateSettings();
        }

        private void RemoveFromOpen(Machine machine)
        {
            lock (OpenMachines)
            {
                OpenMachines.Remove(machine);
            }
        }

        private void RemoveFromClosed(string filepath)
        {
            lock (ClosedMachines)
            {
                List<MachineInfo> closedMachines = ClosedMachines.Where(m => String.Compare(m.Filepath, filepath, true) == 0).ToList();
                foreach (MachineInfo machineInfo in closedMachines)
                {
                    ClosedMachines.Remove(machineInfo);
                }
            }
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
