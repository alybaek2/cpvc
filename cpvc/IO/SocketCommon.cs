using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public delegate void NewMessageDelegate(byte[] message);

    public class SocketCommon
    {
        private const byte _delimByte = 0xff;
        private const byte _escapeByte = 0xfe;
        private const byte _escapeEscapeByte = 0x00; // 0xfe 0x00 == 0xfe
        private const byte _escapeDelimByte = 0x01;  // 0xfe 0x01 == 0xff

        protected List<byte> _currentMessage;
        private bool _escaped;

        public NewMessageDelegate OnNewMessage { get; set; }

        protected byte[] _receiveData = new byte[1024];

        protected System.Net.Sockets.Socket _remoteSocket;

        public SocketCommon()
        {
            _currentMessage = new List<byte>();
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
            if (_remoteSocket == null)
            {
                // Nobody to send to!
                return;
            }

            byte[] escapedMsg = EscapeMessageForSending(msg);

            int bytesToSend = escapedMsg.Length;
            int offset = 0;

            try
            {
                while (bytesToSend > 0)
                {
                    int bytesSent = _remoteSocket.Send(escapedMsg, offset, bytesToSend, System.Net.Sockets.SocketFlags.None);

                    bytesToSend -= bytesSent;
                    offset += bytesSent;
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                _remoteSocket = null;
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


        public void ReceiveData(IAsyncResult asyn)
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
