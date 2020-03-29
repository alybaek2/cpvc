using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class SocketServer : SocketCommon
    {
        private System.Net.Sockets.Socket _listeningSocket;

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

            if (_remoteSocket != null)
            {
                System.Net.Sockets.Socket socket = _remoteSocket;
                _remoteSocket = null;
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
            if (clientSocket == null)
            {
                return;
            }

            _remoteSocket = clientSocket;

            _remoteSocket.BeginReceive(_receiveData, 0, _receiveData.Length, System.Net.Sockets.SocketFlags.None, new AsyncCallback(ReceiveData), null);
        }
    }
}
