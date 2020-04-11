using System;
using System.Collections.Generic;

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
        private const string _remoteServersSettingName = "RemoteServers";

        static private string GetStringSetting(string name)
        {
            return Properties.Settings.Default[name] as string;
        }

        static private void SetStringSetting(string name, string value)
        {
            Properties.Settings.Default[name] = value;
            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Gets the default folder for a file type.
        /// </summary>
        /// <param name="fileType">The file type.</param>
        /// <returns>The default folder for the given file type.</returns>
        public string GetFolder(FileTypes fileType)
        {
            switch (fileType)
            {
                case FileTypes.Disc:
                    return GetStringSetting(_discsFolderSettingName);
                case FileTypes.Tape:
                    return GetStringSetting(_tapesFolderSettingName);
                case FileTypes.Machine:
                    return GetStringSetting(_machinesFolderSettingName);
                default:
                    throw new Exception(String.Format("Unknown FileTypes value {0}.", fileType));
            }
        }

        /// <summary>
        /// Sets the default folder for a file type
        /// </summary>
        /// <param name="fileType">The file type.</param>
        /// <param name="folder">The value to set the default folder to.</param>
        public void SetFolder(FileTypes fileType, string folder)
        {
            switch (fileType)
            {
                case FileTypes.Disc:
                    SetStringSetting(_discsFolderSettingName, folder);
                    break;
                case FileTypes.Tape:
                    SetStringSetting(_tapesFolderSettingName, folder);
                    break;
                case FileTypes.Machine:
                    SetStringSetting(_machinesFolderSettingName, folder);
                    break;
                default:
                    throw new Exception(String.Format("Unknown FileTypes value {0}.", fileType));
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

        public string RemoteServers
        {
            get
            {
                return GetStringSetting(_remoteServersSettingName);
            }

            set
            {
                SetStringSetting(_remoteServersSettingName, value);
            }
        }
    }
}
