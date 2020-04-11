using System;
using System.Collections.Generic;

namespace CPvC
{
    /// <summary>
    /// Interface for application-wide configuration settings.
    /// </summary>
    public interface ISettings
    {
        string RecentlyOpened { get; set; }

        string GetFolder(FileTypes fileType);
        void SetFolder(FileTypes fileType, string folder);

        string RemoteServers { get; set; }
    }
}
