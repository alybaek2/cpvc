using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void NewMessageDelegate(byte[] message);

    public class SocketConnection : IConnection, IDisposable
    {
        private const byte _delimByte = 0xff;
        private const byte _escapeByte = 0xfe;
        private const byte _escapeEscapeByte = 0x00; // 0xfe 0x00 == 0xfe
        private const byte _escapeDelimByte = 0x01;  // 0xfe 0x01 == 0xff

        protected List<byte> _currentMessage;
        private bool _escaped;

        public event NewMessageDelegate OnNewMessage;

        protected byte[] _receiveData = new byte[1024];
        protected List<byte> _sendData;

        protected Socket _socket;

        public SocketConnection()
        {
            _currentMessage = new List<byte>();
            _sendData = new List<byte>();
        }

        public SocketConnection(Socket socket) : this()
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

        public void Dispose()
        {
            Close();
        }

        static public SocketConnection ConnectToServer(string hostname, UInt16 port)
        {
            SocketConnection conn = new SocketConnection();
            if (conn.Connect(hostname, port))
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
                if (host.AddressList[f].AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddr = host.AddressList[f];
                    break;
                }
            }

            if (ipAddr == null)
            {
                return false;
            }

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                if (_socket == null)
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
            if (_socket == null)
            {
                // Nobody to send to!
                return;
            }

            byte[] bytesToSend = _sendData.ToArray();
            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.SetBuffer(bytesToSend, 0, bytesToSend.Length);
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCallback);

            _socket.SendAsync(sendArgs);
        }

        private void SendCallback(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
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
