using System;

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
