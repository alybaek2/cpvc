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
        INotifyPropertyChanged,
        IDisposable
    {
        private IConnection _connection;

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
            _connection.OnNewMessage += NewMessage;
        }

        public void NewMessage(byte[] message)
        {
            CoreAction action = MachineServer.Deserialize(message);
            action.ExpectedExecutionTime = Ticks;

            _core.PushRequest(action);
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
