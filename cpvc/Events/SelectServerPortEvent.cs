using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class SelectServerPortEventArgs : EventArgs
    {
        public SelectServerPortEventArgs()
        {
        }

        public UInt16 DefaultPort { get; set; }

        public UInt16? SelectedPort { get; set; }
    }

    public delegate void SelectServerPortEventHandler(object sender, SelectServerPortEventArgs e);
}
