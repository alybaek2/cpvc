using System;
using System.ComponentModel;

namespace CPvC
{
    public class SelectServerPortEventArgs : HandledEventArgs
    {
        public SelectServerPortEventArgs(UInt16 defaultPort)
        {
            DefaultPort = defaultPort;
        }

        public UInt16 DefaultPort { get; private set; }

        public UInt16? SelectedPort { get; set; }
    }

    public delegate void SelectServerPortEventHandler(object sender, SelectServerPortEventArgs e);
}
