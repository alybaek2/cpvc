using System;
using System.Net;
using System.Net.Sockets;

namespace CPvC
{
    public class Socket : ISocket
    {
        private System.Net.Sockets.Socket _socket;

        public Socket()
        {
            _socket = new System.Net.Sockets.Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public Socket(System.Net.Sockets.Socket socket)
        {
            _socket = socket;
        }

        public bool Connected
        {
            get
            {
                return _socket.Connected;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state)
        {
            return _socket.BeginReceive(buffer, offset, size, socketFlags, callback, state);
        }

        public int EndReceive(IAsyncResult asyncResult)
        {
            return _socket.EndReceive(asyncResult);
        }

        public ISocket EndAccept(IAsyncResult asyncResult)
        {
            return new Socket(_socket.EndAccept(asyncResult));
        }

        public bool SendAsync(byte[] buffer, SendCallbackDelegate callback)
        {
            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.SetBuffer(buffer, 0, buffer.Length);
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>((o, e) =>
            {
                callback(e.SocketError, e.BytesTransferred);
            });

            return _socket.SendAsync(sendArgs);
        }

        public void Close()
        {
            _socket.Close();
        }

        public void Bind(EndPoint localEP)
        {
            _socket.Bind(localEP);
        }

        public void Listen(int backlog)
        {
            _socket.Listen(backlog);
        }

        public IAsyncResult BeginAccept(AsyncCallback callback, object state)
        {
            return _socket.BeginAccept(callback, state);
        }

        public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state)
        {
            return _socket.BeginConnect(remoteEP, callback, state);
        }
    }
}
