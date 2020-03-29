using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class SocketClient : SocketCommon
    {
        public SocketClient()
        {
        }

        public bool Connect(string hostname, UInt16 port)
        {
            System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(hostname);
            if (host.AddressList.Length <= 0)
            {
                return false;
            }

            System.Net.IPAddress ipAddr = null;
            for (int f = 0; f < host.AddressList.Length; f++)
            {
                if (host.AddressList[f].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipAddr = host.AddressList[f];
                    break;
                }
            }

            if (ipAddr == null)
            {
                return false;
            }

            _remoteSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            System.Net.IPEndPoint ipEnd = new System.Net.IPEndPoint(ipAddr, port);

            _remoteSocket.Connect(ipEnd);

            if (_remoteSocket.Connected)
            {
                _remoteSocket.BeginReceive(_receiveData, 0, _receiveData.Length, System.Net.Sockets.SocketFlags.None, new AsyncCallback(ReceiveData), null);

                return true;
            }

            return false;
        }

        public void Disconnect()
        {
            _remoteSocket.Disconnect(false);

            if (_remoteSocket != null)
            {
                System.Net.Sockets.Socket socket = _remoteSocket;
                _remoteSocket = null;
                socket.Close();
            }
        }
    }
}
