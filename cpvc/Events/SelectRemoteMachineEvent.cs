using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class SelectRemoteMachineEventArgs : EventArgs
    {
        public SelectRemoteMachineEventArgs()
        {
        }

        public ServerInfo ServerInfo { get; set; }

        public RemoteMachine SelectedMachine { get; set; }
    }

    public delegate void SelectRemoteMachineEventHandler(object sender, SelectRemoteMachineEventArgs e);
}
