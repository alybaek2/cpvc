using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineEventArgs : EventArgs
    {
        public MachineEventArgs(CoreAction action)
        {
            Action = action;
        }

        public CoreAction Action { get; set; }
    }

    public delegate void MachineEventHandler(object sender, MachineEventArgs e);
}
