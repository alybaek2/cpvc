using System.ComponentModel;

namespace CPvC
{
    public class SelectRemoteMachineEventArgs : HandledEventArgs
    {
        public SelectRemoteMachineEventArgs(ServerInfo serverInfo)
        {
            ServerInfo = serverInfo;
        }

        public ServerInfo ServerInfo { get; private set; }

        public RemoteMachine SelectedMachine { get; set; }
    }

    public delegate void SelectRemoteMachineEventHandler(object sender, SelectRemoteMachineEventArgs e);
}
