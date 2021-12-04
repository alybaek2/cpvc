using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CPvC
{
    public class MainModel
    {
        private readonly ISettings _settings;

        private ObservableCollection<IMachine> _machines = new ObservableCollection<IMachine>();

        public ReadOnlyObservableCollection<IMachine> Machines { get; }

        public MainModel(ISettings settings, IFileSystem fileSystem)
        {
            _settings = settings;

            Machines = new ReadOnlyObservableCollection<IMachine>(_machines);

            LoadFromSettings(fileSystem);
        }

        private void Machine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LocalMachine.PersistantFilepath))
            {
                UpdateSettings();
            }
        }

        public LocalMachine AddMachine(string filepath, IFileSystem fileSystem, bool open)
        {
            if (String.IsNullOrEmpty(filepath))
            {
                return null;
            }

            lock (_machines)
            {
                if (GetPersistedMachine(filepath) == null)
                {
                    LocalMachine machine;
                    if (open)
                    {
                        machine = LocalMachine.OpenFromFile(fileSystem, filepath);
                    }
                    else
                    {
                        machine = LocalMachine.GetClosedMachine(fileSystem, filepath);
                    }

                    AddMachine(machine);

                    return machine;
                }
            }

            return null;
        }

        public void AddMachine(IMachine machine)
        {
            if (machine == null)
            {
                throw new ArgumentException("Can't add a null machine.", nameof(machine));
            }

            lock (_machines)
            {
                _machines.Add(machine);
                UpdateSettings();
                machine.PropertyChanged += Machine_PropertyChanged;
            }
        }

        public void RemoveMachine(IMachine machine)
        {
            lock (_machines)
            {
                machine.PropertyChanged -= Machine_PropertyChanged;
                _machines.Remove(machine);
                UpdateSettings();
            }
        }

        /// <summary>
        /// Updates the config settings with all the machines in OpenMachines and ClosedMachines.
        /// </summary>
        public void UpdateSettings()
        {
            Dictionary<string, string> machineFilepathNameMap = new Dictionary<string, string>();

            lock (Machines)
            {
                foreach (IMachine machine in _machines)
                {
                    IPersistableMachine pm = machine as IPersistableMachine;
                    if (pm == null || pm.PersistantFilepath == null)
                    {
                        continue;
                    }

                    machineFilepathNameMap[pm.PersistantFilepath.ToLower()] = machine.Name;
                }
            }

            _settings.RecentlyOpened = Helpers.JoinWithEscape(',', machineFilepathNameMap.Keys); // machineFilepathNameMap.Select(kv => Helpers.JoinWithEscape(';', new List<string> { kv.Value, kv.Key })));
        }

        /// <summary>
        /// Loads the model from the config settings.
        /// </summary>
        /// <param name="fileSystem">File system required by MachineInfo to load a thumbnail for each machine.</param>
        private void LoadFromSettings(IFileSystem fileSystem)
        {
            string recent = _settings?.RecentlyOpened;
            if (recent == null)
            {
                return;
            }

            lock (Machines)
            {
                foreach (string machineStr in Helpers.SplitWithEscape(',', recent))
                {
                    try
                    {
                        AddMachine(machineStr, fileSystem, false);
                    }
                    catch
                    {
                        Diagnostics.Trace("Unable to load \"{0}\".", machineStr);
                    }
                }
            }
        }

        private IMachine GetPersistedMachine(string filepath)
        {
            return _machines.FirstOrDefault(
                m => {
                    if (m is IPersistableMachine pm)
                    {
                        return String.Compare(pm.PersistantFilepath, filepath, true) == 0;
                    }

                    return false;
                });
        }
    }
}
