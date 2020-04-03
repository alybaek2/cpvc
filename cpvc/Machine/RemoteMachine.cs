using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public sealed class RemoteMachine : CoreMachine,
        IClosableMachine,
        IRemoteReceiver,
        INotifyPropertyChanged,
        IDisposable
    {
        private IConnection _connection;
        private Remote _remote;

        public string Name
        {
            get; set;
        }

        public OnCloseDelegate OnClose { get; set; }

        public RemoteMachine()
        {
            Display = new Display();
            Display.GetFromBookmark(null);

            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            core.KeepRunning = false;
            Core = core;
            core.Start();

            _connection = SocketConnection.ConnectToServer("localhost", 6128);

            _remote = new Remote(_connection, this);            
        }

        public void ReceiveCoreAction(CoreAction coreAction)
        {
            _core.PushRequest(coreAction);
        }

        public void ReceiveSelectMachine(string machineName)
        {

        }

        public void ReceiveAvailableMachines(List<string> availableMachines)
        {
            if (availableMachines.Count > 0)
            {
                _remote.SendSelectMachine(availableMachines[0]);
            }
        }

        public void Dispose()
        {
            Close();

            Display?.Dispose();
            Display = null;
        }

        public bool CanClose()
        {
            return true;
        }

        public void Close()
        {
            _connection.Close();
            _core.Stop();
            Core = null;

            OnClose?.Invoke();
        }
    }
}
