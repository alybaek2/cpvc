using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MachineEventArgs : EventArgs
    {
        public MachineEventArgs(IMachineAction action)
        {
            Action = action;
        }

        public IMachineAction Action { get; set; }
    }

    public delegate void MachineEventHandler(object sender, MachineEventArgs e);
}
