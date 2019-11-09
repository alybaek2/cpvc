namespace CPvC
{
    /// <summary>
    /// Encapsulates all application-wide configuration settings.
    /// </summary>
    public class Settings : ISettings
    {
        private const string _machinesFolderSettingName = "MachinesFolder";
        private const string _discsFolderSettingName = "DiscsFolder";
        private const string _tapesFolderSettingName = "TapesFolder";
        private const string _recentlyOpenedSettingName = "RecentlyOpened";

        static private string GetStringSetting(string name)
        {
            return Properties.Settings.Default[name].ToString();
        }

        static private void SetStringSetting(string name, string value)
        {
            Properties.Settings.Default[name] = value;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Default folder for loading machines.
        /// </summary>
        public string MachinesFolder
        {
            get
            {
                return GetStringSetting(_machinesFolderSettingName);
            }

            set
            {
                SetStringSetting(_machinesFolderSettingName, value);
            }
        }

        /// <summary>
        /// Default folder for loading disc images.
        /// </summary>
        public string DiscsFolder
        {
            get
            {
                return GetStringSetting(_discsFolderSettingName);
            }

            set
            {
                SetStringSetting(_discsFolderSettingName, value);
            }
        }

        /// <summary>
        /// Default folder for loading tape images.
        /// </summary>
        public string TapesFolder
        {
            get
            {
                return GetStringSetting(_tapesFolderSettingName);
            }

            set
            {
                SetStringSetting(_tapesFolderSettingName, value);
            }
        }

        /// <summary>
        /// Information on all recently opened machines.
        /// </summary>
        public string RecentlyOpened
        {
            get
            {
                return GetStringSetting(_recentlyOpenedSettingName);
            }

            set
            {
                SetStringSetting(_recentlyOpenedSettingName, value);
            }
        }
    }
}
