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

            Machines = new ObservableCollection<Machine>();
            ReplayMachines = new ObservableCollection<ReplayMachine>();
            RemoteMachines = new ObservableCollection<RemoteMachine>();

            LoadFromSettings(fileSystem);
        }

        /// <summary>
        /// Represents the set of machines that are currently open or closed.
        /// </summary>
        public ObservableCollection<Machine> Machines { get; }

        public ObservableCollection<ReplayMachine> ReplayMachines { get; }

        public ObservableCollection<RemoteMachine> RemoteMachines { get; }

        /// <summary>
        /// Adds a machine to the model if it doesn't already exist.
        /// </summary>
        /// <param name="filepath">The filepath of the machine to add.</param>
        /// <param name="fileSystem">The IFileSystem interface to use to access <c>filepath</c>.</param>
        /// <returns></returns>
        public Machine Add(string filepath, IFileSystem fileSystem)
        {
            if (filepath == null)
            {
                return null;
            }

            lock (Machines)
            {
                Machine machine = null;

                // Check to see if we've already got a machine with the same filepath open. Note that the check here using GetFullPath
                // isn't foolproof, as the same file could have different paths (e.g. "C:\test.cpvc" and "\\machine\c$\test.cpvc").
                string fullFilepath = System.IO.Path.GetFullPath(filepath);
                machine = Machines.FirstOrDefault(m => String.Compare(System.IO.Path.GetFullPath(m.Filepath), fullFilepath, true) == 0);
                if (machine == null)
                {
                    try
                    {
                        if (fileSystem.Exists(filepath))
                        {
                            machine = Machine.Open("", filepath, fileSystem, false);
                        }
                        else
                        {
                            string name = System.IO.Path.GetFileNameWithoutExtension(filepath);
                            machine = Machine.New(name, filepath, fileSystem);
                        }
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.Trace("Exception caught while opening {0}: {1}", filepath, ex.Message);

                        throw;
                    }

                    Machines.Add(machine);

                    UpdateSettings();
                }

                return machine;
            }
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
