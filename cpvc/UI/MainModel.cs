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

            Machines = new ObservableCollection<Machine>();

            LoadFromSettings(fileSystem);
        }

        /// <summary>
        /// Represents the set of machines that are currently open or closed.
        /// </summary>
        public ObservableCollection<Machine> Machines { get; }

        /// <summary>
        /// Adds a machine to the model.
        /// </summary>
        /// <param name="machine">The machine to add.</param>
        public void Add(Machine machine)
        {
            lock (Machines)
            {
                if (!Machines.Contains(machine))
                {
                    Machines.Add(machine);
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
            lock (Machines)
            {
                Machines.Remove(machine);
            }

            UpdateSettings();
        }

        /// <summary>
        /// Updates the config settings with all the machines in OpenMachines and ClosedMachines.
        /// </summary>
        private void UpdateSettings()
        {
            Dictionary<string, string> machineFilepathNameMap = new Dictionary<string, string>();

            lock (Machines)
            {
                foreach (Machine machine in Machines)
                {
                    machineFilepathNameMap[machine.Filepath.ToLower()] = machine.Name;
                }
            }

            _settings.RecentlyOpened = Helpers.JoinWithEscape(',', machineFilepathNameMap.Select(kv => Helpers.JoinWithEscape(';', new List<string> { kv.Value, kv.Key })));
        }

        /// <summary>
        /// Loads the model from the config settings.
        /// </summary>
        /// <param name="fileSystem">File system required by MachineInfo to load a thumbnail for each machine.</param>
        private void LoadFromSettings(IFileSystem fileSystem)
        {
            string recent = _settings.RecentlyOpened;
            if (recent == null)
            {
                return;
            }

            lock (Machines)
            {
                foreach (string machineStr in Helpers.SplitWithEscape(',', recent))
                {
                    List<string> tokens = Helpers.SplitWithEscape(';', machineStr);
                    if (tokens.Count < 2)
                    {
                        continue;
                    }

                    try
                    {
                        Machine machine = Machine.Open(tokens[0], tokens[1], fileSystem, true);
                        Machines.Add(machine);
                    }
                    catch
                    {
                        Diagnostics.Trace("Unable to load \"{0}\".", tokens[1]);
                    }
                }
            }
        }
    }
}
