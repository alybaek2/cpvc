﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void ClientConnectDelegate(SocketConnection socket);

    public class SocketServer
    {
        private Socket _listeningSocket;

        public ClientConnectDelegate OnClientConnect { get; set; }

        public SocketServer()
        {
        }

        public void Start(UInt16 port)
        {
            System.Net.IPEndPoint ipEnd = new System.Net.IPEndPoint(System.Net.IPAddress.Any, port);

            if (_listeningSocket == null)
            {
                _listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }

            _listeningSocket.Bind(ipEnd);

            // Accept only one connection for now...
            _listeningSocket.Listen(1);

            _listeningSocket.BeginAccept(new AsyncCallback(ClientConnect), null);
        }

        public void Stop()
        {
            if (_listeningSocket != null)
            {
                Socket socket = _listeningSocket;
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

            try
            {
                Socket clientSocket = _listeningSocket.EndAccept(asyn);
                if (clientSocket != null)
                {
                    SocketConnection com = new SocketConnection(clientSocket);
                    OnClientConnect?.Invoke(com);
                }

                _listeningSocket.BeginAccept(new AsyncCallback(ClientConnect), null);
            }
            catch (SocketException ex)
            {
                Diagnostics.Trace("Exception during ClientConnect: {0}", ex.Message);
                Stop();
            }
        }
    }
}
