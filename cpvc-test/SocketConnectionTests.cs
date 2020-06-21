using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace CPvC.Test
{
    public class SocketConnectionTests
    {
        private Mock<IAsyncResult> _mockResult;
        private Mock<ISocket> _mockSocket;
        private ManualResetEvent _mockEvent;
        private byte[] _sendMessage;
        private AsyncCallback _receiveCallback;

        private Moq.Language.Flow.ISetup<ISocket, IAsyncResult> _mockBeginConnect;
        private Moq.Language.Flow.ISetup<ISocket, IAsyncResult> _mockBeginReceive;
        private Moq.Language.Flow.ISetup<ISocket, int> _mockEndReceive;
        private Moq.Language.Flow.ISetupGetter<ISocket, bool> _mockConnected;

        [SetUp]
        public void Setup()
        {
            _mockEvent = new ManualResetEvent(true);
            _mockResult = new Mock<IAsyncResult>();
            _mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(_mockEvent);

            _mockSocket = new Mock<ISocket>();

            _mockBeginConnect = _mockSocket.Setup(s => s.BeginConnect(It.IsAny<System.Net.EndPoint>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()));
            _mockBeginConnect.Returns(_mockResult.Object);

            _mockBeginReceive = _mockSocket.Setup(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SocketFlags>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()));
            _mockEndReceive = _mockSocket.Setup(s => s.EndReceive(It.IsAny<IAsyncResult>()));
            _mockConnected = _mockSocket.SetupGet(s => s.Connected);
            _mockConnected.Returns(true);

            _mockBeginReceive.Callback<byte[], int, int, SocketFlags, AsyncCallback, object>((b, offset, s, f, c, o) => {
                _sendMessage?.CopyTo(b, 0);

                _receiveCallback = c;
            });

            _sendMessage = null;
            _receiveCallback = null;
        }

        [TestCase(null)]
        [TestCase(false)]
        [TestCase(true)]
        public void IsConnected(bool? connected)
        {
            // Setup
            SocketConnection connection;
            Mock<ISocket> mockSocket;
            if (connected.HasValue)
            {
                mockSocket = new Mock<ISocket>();
                mockSocket.SetupGet(s => s.Connected).Returns(connected.Value);

                connection = new SocketConnection(mockSocket.Object);
            }
            else
            {
                connection = new SocketConnection();
            }

            // Act
            bool isConnected = connection.IsConnected;

            // Verify
            Assert.AreEqual(connected ?? false, isConnected);
        }

        [Test]
        public void ConnectToServer([Values(false, true)] bool success)
        {
            // Setup
            _mockConnected.Returns(success);

            // Act
            SocketConnection connection =  SocketConnection.ConnectToServer(_mockSocket.Object, "localhost", 6128);

            // Verify
            if (success)
            {
                Assert.IsNotNull(connection);
                Assert.True(connection.IsConnected);
            }
            else
            {
                Assert.IsNull(connection);
            }
        }

        [Test]
        public void DisposeTwice()
        {
            // Setup
            SocketConnection connection = new SocketConnection();
            connection.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => connection.Dispose());
        }

        [Test]
        public void BeginConnectException()
        {
            // Setup
            _mockBeginConnect.Throws<SocketException>();
            _mockConnected.Returns(false);

            // Act
            SocketConnection connection = SocketConnection.ConnectToServer(_mockSocket.Object, "localhost", 6128);

            // Verify
            _mockSocket.Verify(s => s.Close(), Times.Never());
        }

        [Test]
        public void ConnectInvalidHostname()
        {
            // Setup
            _mockConnected.Returns(false);

            // Act
            SocketConnection connection = SocketConnection.ConnectToServer(_mockSocket.Object, "ZZZ", 6128);

            // Verify
            Assert.IsNull(connection);
            _mockSocket.VerifyNoOtherCalls();
        }

        [Test]
        public void BeginReceiveException()
        {
            // Setup
            _mockBeginReceive.Throws<SocketException>();
            _mockConnected.Returns(true);

            // Act
            SocketConnection connection = SocketConnection.ConnectToServer(_mockSocket.Object, "localhost", 6128);

            // Verify
            _mockSocket.Verify(s => s.Close(), Times.Once());
        }

        [TestCase(new byte[] { 0x01, 0x02, 0xff       }, new byte[] { 0x01, 0x02 })]
        [TestCase(new byte[] { 0x01, 0xfe, 0x00, 0xff }, new byte[] { 0x01, 0xfe })]
        [TestCase(new byte[] { 0x01, 0xfe, 0x01, 0xff }, new byte[] { 0x01, 0xff })]
        [TestCase(new byte[] { 0x01, 0xfe, 0x03, 0xff }, new byte[] { 0x01 })]
        public void Receive(byte[] message, byte[] expected)
        {
            // Setup
            _sendMessage = message;

            SocketConnection connection = new SocketConnection(_mockSocket.Object);

            Mock<NewMessageDelegate> mockNewMessage = new Mock<NewMessageDelegate>();
            connection.OnNewMessage += mockNewMessage.Object;

            _mockSocket.Setup(s => s.EndReceive(It.IsAny<IAsyncResult>())).Returns(message.Length);

            // Act
            _receiveCallback?.Invoke(_mockResult.Object);

            // Verify
            mockNewMessage.Verify(m => m(expected));
        }

        [Test]
        public void SendAfterClosed()
        {
            // Setup
            SocketConnection connection = new SocketConnection(_mockSocket.Object);

            // Act
            connection.Close();
            bool success = connection.SendMessage(new byte[] { 0x01, 0x02 });

            // Verify
            Assert.False(success);
        }

        [Test]
        public void Close()
        {
            // Setup
            SocketConnection connection = new SocketConnection(_mockSocket.Object);

            // Act
            connection.Close();
            _receiveCallback?.Invoke(_mockResult.Object);

            // Verify
            _mockSocket.Verify(s => s.EndReceive(It.IsAny<IAsyncResult>()), Times.Never());
        }

        [Test]
        public void Disconnected()
        {
            // Setup
            SocketConnection connection = new SocketConnection(_mockSocket.Object);

            // Act
            _mockConnected.Returns(false);
            _receiveCallback?.Invoke(_mockResult.Object);

            // Verify
            _mockSocket.Verify(s => s.EndReceive(It.IsAny<IAsyncResult>()), Times.Never());
        }

        [Test]
        public void EndReceiveException()
        {
            // Setup
            _mockEndReceive.Throws<SocketException>();

            SocketConnection connection = new SocketConnection(_mockSocket.Object);

            // Act
            _receiveCallback?.Invoke(_mockResult.Object);

            // Verify
            _mockSocket.Verify(s => s.EndReceive(It.IsAny<IAsyncResult>()), Times.Once());
            _mockSocket.Verify(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SocketFlags>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()), Times.Once());
        }

        [Test]
        public void ResumeReceiveException()
        {
            // Setup
            SocketConnection connection = new SocketConnection(_mockSocket.Object);
            _mockBeginReceive.Throws<SocketException>();

            // Act
            _receiveCallback?.Invoke(_mockResult.Object);

            // Verify
            _mockSocket.Verify(s => s.EndReceive(It.IsAny<IAsyncResult>()), Times.Once());
            _mockSocket.Verify(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SocketFlags>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()), Times.Exactly(2));
            Assert.False(connection.IsConnected);
        }

        [Test]
        public void ReceiveCloseConnection()
        {
            // Setup
            byte[] message = new byte[] { 0x01, 0xfe, 0x02 };
            AsyncCallback receiveCallback = null;
            _mockBeginReceive.Callback<byte[], int, int, SocketFlags, AsyncCallback, object>((b, offset, s, f, c, o) => {
                message.CopyTo(b, 0);

                receiveCallback = c;
            });
            _mockConnected.Returns(true);
            _mockEndReceive.Returns(message.Length);

            Mock<CloseConnectionDelegate> mockClose = new Mock<CloseConnectionDelegate>();

            SocketConnection connection = new SocketConnection(_mockSocket.Object);
            connection.OnCloseConnection += mockClose.Object;

            Mock<NewMessageDelegate> mockNewMessage = new Mock<NewMessageDelegate>();
            connection.OnNewMessage += mockNewMessage.Object;

            // Act
            receiveCallback?.Invoke(_mockResult.Object);

            // Verify
            mockClose.Verify();
            Assert.False(connection.IsConnected);
        }

        [TestCase(new byte[] { 0x01, 0x02 }, new byte[] { 0x01, 0x02, 0xff })]
        [TestCase(new byte[] { 0x01, 0xfe }, new byte[] { 0x01, 0xfe, 0x00, 0xff })]
        [TestCase(new byte[] { 0x01, 0xff }, new byte[] { 0x01, 0xfe, 0x01, 0xff })]
        [TestCase(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 }, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0xff })]
        public void Send(byte[] message, byte[] expected)
        {
            // Setup
            List<byte> sent = new List<byte>();
            _mockSocket.Setup(s => s.SendAsync(It.IsAny<byte[]>(), It.IsAny<SendCallbackDelegate>())).Callback<byte[], SendCallbackDelegate>((buffer, callback) =>
            {
                // Pretend we're sending 4 bytes at a time.
                int sentBytes = Math.Min(4, buffer.Length);
                sent.AddRange(buffer.Take(sentBytes));

                callback.Invoke(SocketError.Success, sentBytes);
            });

            SocketConnection connection = new SocketConnection(_mockSocket.Object);

            // Act
            connection.SendMessage(message);
            connection.SendComplete.WaitOne(2000);

            // Verify
            Assert.AreEqual(expected, sent);
            Assert.True(connection.SendComplete.WaitOne(0));
        }
    }
}
