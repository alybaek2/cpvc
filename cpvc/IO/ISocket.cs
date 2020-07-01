using System;
using System.Net;
using System.Net.Sockets;

namespace CPvC
{
    public delegate void SendCallbackDelegate(SocketError error, int bytesTransferred);

    public interface ISocket : IDisposable
    {
        bool Connected { get; }
        IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback callback, object state);
        int EndReceive(IAsyncResult asyncResult);
        ISocket EndAccept(IAsyncResult asyncResult);
        bool SendAsync(byte[] buffer, SendCallbackDelegate callback);
        void Close();
        void Bind(EndPoint localEP);
        void Listen(int backlog);
        IAsyncResult BeginAccept(AsyncCallback callback, object state);
        IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback callback, object state);
    }
}
