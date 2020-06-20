using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void NewMessageDelegate(byte[] message);
    public delegate void CloseConnectionDelegate();

    public class SocketConnection : IConnection, IDisposable
    {
        private const byte _delimByte = 0xff;
        private const byte _escapeByte = 0xfe;
        private const byte _escapeEscapeByte = 0x00; // 0xfe 0x00 == 0xfe
        private const byte _escapeDelimByte = 0x01;  // 0xfe 0x01 == 0xff
        private const byte _closeConnection = 0x02;

        protected List<byte> _currentMessage;
        private bool _escaped;

        public event NewMessageDelegate OnNewMessage;
        public event CloseConnectionDelegate OnCloseConnection;

        protected byte[] _receiveData = new byte[1024];
        protected List<byte> _sendData;

        private ManualResetEvent _sendComplete;

        protected ISocket _socket;

        public SocketConnection()
        {
            _currentMessage = new List<byte>();
            _sendData = new List<byte>();
            _sendComplete = new ManualResetEvent(false);
        }

        public SocketConnection(ISocket socket) : this()
        {
            _socket = socket;

            BeginReceive();
        }

        public bool IsConnected
        {
            get
            {
                return _socket?.Connected ?? false;
            }
        }

        public ManualResetEvent SendComplete
        {
            get
            {
                return _sendComplete;
            }
        }

        public void Dispose()
        {
            Close();
        }

        static public SocketConnection ConnectToServer(ISocket socket, string hostname, UInt16 port)
        {
            SocketConnection conn = new SocketConnection();
            if (conn.Connect(socket, hostname, port))
            {
                return conn;
            }

            return null;
        }

        private void BeginReceive()
        {
            _currentMessage = new List<byte>();
            _sendData = new List<byte>();

            ResumeReceive();
        }

        private void ResumeReceive()
        {
            try
            {
                _socket.BeginReceive(_receiveData, 0, _receiveData.Length, SocketFlags.None, new AsyncCallback(ReceiveData), null);
            }
            catch (SocketException ex)
            {
                Diagnostics.Trace("Exception during ResumeReceive: {0}", ex.Message);
                Close();
            }
        }

        private bool Connect(ISocket socket, string hostname, UInt16 port)
        {
            System.Net.IPAddress[] addrs;

            try
            {
                addrs = System.Net.Dns.GetHostAddresses(hostname);
            }
            catch (SocketException ex)
            {
                return false;
            }

            if (addrs.Length <= 0)
            {
                return false;
            }

            System.Net.IPAddress ipAddr = null;
            for (int f = 0; f < addrs.Length; f++)
            {
                if (addrs[f].AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddr = addrs[f];
                    break;
                }
            }

            if (ipAddr == null)
            {
                return false;
            }

            System.Net.IPEndPoint ipEnd = new System.Net.IPEndPoint(ipAddr, port);

            try
            {
                IAsyncResult result = socket.BeginConnect(ipEnd, null, null);
                bool waitResult = result.AsyncWaitHandle.WaitOne(2000);

                if (waitResult && socket.Connected)
                {
                    _socket = socket;

                    BeginReceive();

                    return true;
                }
            }
            catch (SocketException ex)
            {
                Diagnostics.Trace("Exception during Connect: {0}", ex.Message);
                Close();
            }

            return false;
        }

        public void Close()
        {
            SendRawMessage(new byte[] { _escapeByte, _closeConnection });

            SocketClose();
        }

        private void SocketClose()
        {
            _socket?.Close();
            _socket = null;
        }

        private void ProcessData(byte[] bytes, int count)
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
                        case _closeConnection:
                            OnCloseConnection?.Invoke();
                            SocketClose();

                            return;
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

        public bool SendMessage(byte[] msg)
        {
            byte[] escapedMsg = EscapeMessageForSending(msg);

            return SendRawMessage(escapedMsg);
        }

        private bool SendRawMessage(byte[] msg)
        {
            lock (_sendData)
            {
                if (_socket == null)
                {
                    return false;
                }

                bool isEmpty = (_sendData.Count == 0);
                _sendData.AddRange(msg);

                if (_sendData.Count > 0)
                {
                    _sendComplete.Reset();
                }

                if (isEmpty)
                {
                    SendQueuedDataASync();
                }

                return true;
            }
        }

        private void SendQueuedDataASync()
        {
            if (_socket == null)
            {
                // Nobody to send to!
                return;
            }

            _socket.SendAsync(_sendData.ToArray(), SendCallback);
        }

        private void SendCallback(SocketError error, int bytesTransferred)
        {
            if (error == SocketError.Success)
            {
                lock (_sendData)
                {
                    _sendData.RemoveRange(0, bytesTransferred);

                    if (_sendData.Count > 0)
                    {
                        SendQueuedDataASync();
                    }
                    else
                    {
                        _sendComplete.Set();
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


        private void ReceiveData(IAsyncResult asyn)
        {
            if (_socket == null)
            {
                return;
            }

            if (!_socket.Connected)
            {
                return;
            }

            int bytesReceived = 0;
            try
            {
                bytesReceived = _socket.EndReceive(asyn);
            }
            catch (SocketException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return;
            }

            ProcessData(_receiveData, bytesReceived);

            if (_socket != null && _socket.Connected)
            {
                try
                {
                    ResumeReceive();
                }
                catch (SocketException e)
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
