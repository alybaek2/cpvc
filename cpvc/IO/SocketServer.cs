using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void ClientConnectDelegate(SocketConnection socket);

    public class SocketServer
    {
        private System.Net.Sockets.Socket _listeningSocket;

        public ClientConnectDelegate OnClientConnect { get; set; }

        public SocketServer()
        {
            _listeningSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
        }

        public void Start(UInt16 port)
        {
            System.Net.IPEndPoint ipEnd = new System.Net.IPEndPoint(System.Net.IPAddress.Any, port);
            _listeningSocket.Bind(ipEnd);

            // Accept only one connection for now...
            _listeningSocket.Listen(1);

            _listeningSocket.BeginAccept(new AsyncCallback(ClientConnect), null);
        }

        public void Stop()
        {
            if (_listeningSocket != null)
            {
                System.Net.Sockets.Socket socket = _listeningSocket;
                _listeningSocket = null;
                socket.Close();
            }
        }

        public void ClientConnect(IAsyncResult asyn)
        {
            if (_listeningSocket == null)
            {
                return;
            }

            System.Net.Sockets.Socket clientSocket = _listeningSocket.EndAccept(asyn);
            if (clientSocket != null)
            {
                SocketConnection com = new SocketConnection(clientSocket);
                OnClientConnect?.Invoke(com);
            }

            _listeningSocket.BeginAccept(new AsyncCallback(ClientConnect), null);
        }
    }
}
