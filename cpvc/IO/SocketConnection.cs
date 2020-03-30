using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void NewMessageDelegate(byte[] message);

    public class SocketConnection : IConnection
    {
        private const byte _delimByte = 0xff;
        private const byte _escapeByte = 0xfe;
        private const byte _escapeEscapeByte = 0x00; // 0xfe 0x00 == 0xfe
        private const byte _escapeDelimByte = 0x01;  // 0xfe 0x01 == 0xff

        protected List<byte> _currentMessage;
        private bool _escaped;

        public NewMessageDelegate OnNewMessage { get; set; }

        protected byte[] _receiveData = new byte[1024];
        protected List<byte> _sendData;

        protected System.Net.Sockets.Socket _remoteSocket;

        public SocketConnection()
        {
            _currentMessage = new List<byte>();
            _sendData = new List<byte>();
        }

        public SocketConnection(System.Net.Sockets.Socket socket)
        {
            _remoteSocket = socket;

            _currentMessage = new List<byte>();
            _sendData = new List<byte>();

            _remoteSocket.BeginReceive(_receiveData, 0, _receiveData.Length, System.Net.Sockets.SocketFlags.None, new AsyncCallback(ReceiveData), null);
        }

        public SocketConnection(string hostname, UInt16 port)
        {
            Connect(hostname, port);
        }

        private bool Connect(string hostname, UInt16 port)
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

            System.Net.Sockets.Socket remoteSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);

            System.Net.IPEndPoint ipEnd = new System.Net.IPEndPoint(ipAddr, port);

            remoteSocket.Connect(ipEnd);

            if (remoteSocket.Connected)
            {
                _currentMessage = new List<byte>();
                _sendData = new List<byte>();

                remoteSocket.BeginReceive(_receiveData, 0, _receiveData.Length, System.Net.Sockets.SocketFlags.None, new AsyncCallback(ReceiveData), null);

                _remoteSocket = remoteSocket;

                return true;
            }

            return false;
        }

        public void Close()
        {
            _remoteSocket.Close();
        }

        private void ReceiveData(byte[] bytes, int count)
        {
            for (int i = 0; i < count; i++)
            {
                byte b = bytes[i];
                if (b == _delimByte)
                {
                    // Message done!
                    byte[] message = _currentMessage.ToArray();
                    _currentMessage.Clear();
                    _escaped = false;

                    OnNewMessage?.Invoke(message);
                }
                else if (b == _escapeByte)
                {
                    _escaped = true;
                }
                else if (_escaped)
                {
                    _escaped = false;

                    switch (b)
                    {
                        case _escapeEscapeByte:
                            _currentMessage.Add(_escapeByte);
                            break;
                        case _escapeDelimByte:
                            _currentMessage.Add(_delimByte);
                            break;
                        default:
                            // Unrecognized escape sequence!
                            break;
                    }
                }
                else
                {
                    _currentMessage.Add(b);
                }
            }
        }

        public void SendMessage(byte[] msg)
        {
            byte[] escapedMsg = EscapeMessageForSending(msg);

            lock (_sendData)
            {
                if (_remoteSocket == null)
                {
                    return;
                }

                bool isEmpty = (_sendData.Count == 0);
                _sendData.AddRange(escapedMsg);

                if (isEmpty)
                {
                    SendQueuedDataASync();
                }
            }
        }

        private void SendQueuedDataASync()
        {
            if (_remoteSocket == null)
            {
                // Nobody to send to!
                return;
            }

            byte[] bytesToSend = _sendData.ToArray();
            System.Net.Sockets.SocketAsyncEventArgs sendArgs = new System.Net.Sockets.SocketAsyncEventArgs();
            sendArgs.SetBuffer(bytesToSend, 0, bytesToSend.Length);
            sendArgs.Completed += new EventHandler<System.Net.Sockets.SocketAsyncEventArgs>(SendCallback);

            _remoteSocket.SendAsync(sendArgs);
        }

        private void SendCallback(object sender, System.Net.Sockets.SocketAsyncEventArgs e)
        {
            if (e.SocketError == System.Net.Sockets.SocketError.Success)
            {
                int bytesSuccessfullySent = e.BytesTransferred;

                lock (_sendData)
                {
                    _sendData.RemoveRange(0, bytesSuccessfullySent);

                    if (_sendData.Count > 0)
                    {
                        SendQueuedDataASync();
                    }
                }
            }
        }

        private byte[] EscapeMessageForSending(byte[] msg)
        {
            List<byte> escapedMsg = new List<byte>(msg);

            for (int i = escapedMsg.Count - 1; i >= 0; i--)
            {
                if (escapedMsg[i] == _delimByte)
                {
                    escapedMsg[i] = _escapeByte;
                    escapedMsg.Insert(i + 1, _escapeDelimByte);
                }
                else if (escapedMsg[i] == _escapeByte)
                {
                    escapedMsg[i] = _escapeByte;
                    escapedMsg.Insert(i + 1, _escapeEscapeByte);
                }
            }

            escapedMsg.Add(_delimByte);

            return escapedMsg.ToArray();
        }


        protected void ReceiveData(IAsyncResult asyn)
        {
            if (_remoteSocket == null)
            {
                return;
            }

            if (!_remoteSocket.Connected)
            {
                return;
            }

            int bytesReceived = 0;
            try
            {
                bytesReceived = _remoteSocket.EndReceive(asyn);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return;
            }

            ReceiveData(_receiveData, bytesReceived);

            if (_remoteSocket.Connected)
            {
                try
                {
                    _remoteSocket.BeginReceive(_receiveData, 0, _receiveData.Length, System.Net.Sockets.SocketFlags.None, new AsyncCallback(ReceiveData), null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    System.Diagnostics.Debug.WriteLine(e);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Disconnected");
            }
        }
    }
}
