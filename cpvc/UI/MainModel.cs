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

        private ObservableCollection<ICoreMachine> _machines = new ObservableCollection<ICoreMachine>();

        public ReadOnlyObservableCollection<ICoreMachine> Machines { get; }

        public MainModel(ISettings settings, IFileSystem fileSystem)
        {
            _settings = settings;

            Machines = new ReadOnlyObservableCollection<ICoreMachine>(_machines);

            LoadFromSettings(fileSystem);

            // Start observing the machines collection only after we've finished loading it.
            _machines.CollectionChanged += Machines_CollectionChanged;
        }

        private void Machines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (object item in e.NewItems)
                        {
                            ICoreMachine machine = (ICoreMachine)item;
                            machine.PropertyChanged += Machine_PropertyChanged;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (object item in e.OldItems)
                        {
                            ICoreMachine machine = (ICoreMachine)item;
                            machine.PropertyChanged -= Machine_PropertyChanged;
                        }
                    }
                    break;
                default:
                    throw new Exception("Needs implementation!");
            }

            UpdateSettings();
        }

        private void Machine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Machine.PersistantFilepath))
            {
                UpdateSettings();
            }
        }

        public ICoreMachine AddPreview(string filepath, IFileSystem fileSystem)
        {
            if (String.IsNullOrEmpty(filepath))
            {
                return null;
            }

            lock (_machines)
            {
                ICoreMachine existingMachine = _machines.FirstOrDefault(
                    m => {
                        if (m is IPersistableMachine pm)
                        {
                            return String.Compare(pm.PersistantFilepath, filepath, true) == 0;
                        }

                        return false;
                });

                if (existingMachine != null)
                {
                    return existingMachine;
                }

                Machine machine = Machine.OpenPreview(fileSystem, filepath);

                _machines.Add(machine);

                return machine;
            }
        }

        public ICoreMachine AddMachine(string filepath, IFileSystem fileSystem)
        {
            if (String.IsNullOrEmpty(filepath))
            {
                return null;
            }

            lock (_machines)
            {
                ICoreMachine existingMachine = _machines.FirstOrDefault(
                    m => {
                        if (m is IPersistableMachine pm)
                        {
                            return String.Compare(pm.PersistantFilepath, filepath, true) == 0;
                        }

                        return false;

                    });

                if (existingMachine != null)
                {
                    return existingMachine;
                }

                // Ugly!!!
                Machine machine = Machine.OpenPreview(fileSystem, filepath);
                machine.OpenFromFile(fileSystem);
                machine.Start();

                _machines.Add(machine);

                return machine;
            }
        }

        public void AddMachine(ICoreMachine machine)
        {
            lock (_machines)
            {
                _machines.Add(machine);
            }
        }

        public void RemoveMachine(ICoreMachine machine)
        {
            lock (_machines)
            {
                _machines.Remove(machine);
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
                foreach (ICoreMachine machine in _machines)
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
                        AddPreview(machineStr, fileSystem);
                    }
                    catch
                    {
                        Diagnostics.Trace("Unable to load \"{0}\".", machineStr);
                    }
                }
            }
        }
    }
}
