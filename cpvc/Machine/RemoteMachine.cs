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
        private SocketClient _client;

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

            _client = new SocketClient();
            _client.Connect("localhost", 6128);
            _client.OnNewMessage += NewMessage;

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

        public void Close()
        {
            _client.Disconnect();
            _core.Stop();
            Core = null;

            OnClose.Invoke();
        }

    }
}
