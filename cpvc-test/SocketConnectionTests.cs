using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class SocketConnectionTests
    {
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
            System.Threading.ManualResetEvent e = new System.Threading.ManualResetEvent(true);
            Mock<IAsyncResult> mockResult = new Mock<IAsyncResult>();
            mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(e);
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            mockSocket.Setup(s => s.BeginConnect(It.IsAny<System.Net.EndPoint>(), It.IsAny<AsyncCallback>(), It.IsAny<object>())).Returns(mockResult.Object);
            mockSocket.Setup(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()));
            mockSocket.SetupGet(s => s.Connected).Returns(success);

            // Act
            SocketConnection connection =  SocketConnection.ConnectToServer(mockSocket.Object, "localhost", 6128);

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
        public void BeginReceiveException()
        {
            // Setup
            System.Threading.ManualResetEvent e = new System.Threading.ManualResetEvent(true);
            Mock<IAsyncResult> mockResult = new Mock<IAsyncResult>();
            mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(e);
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            mockSocket.Setup(s => s.BeginConnect(It.IsAny<System.Net.EndPoint>(), It.IsAny<AsyncCallback>(), It.IsAny<object>())).Returns(mockResult.Object);
            mockSocket.Setup(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>(), It.IsAny<AsyncCallback>(), It.IsAny<object>())).Throws<SocketException>();
            mockSocket.SetupGet(s => s.Connected).Returns(true);

            // Act
            SocketConnection connection = SocketConnection.ConnectToServer(mockSocket.Object, "localhost", 6128);

            // Verify
            mockSocket.Verify(s => s.Close(), Times.Once());
        }

        [Test]
        public void Receive()
        {
            // Setup
            System.Threading.ManualResetEvent e = new System.Threading.ManualResetEvent(true);
            Mock<IAsyncResult> mockResult = new Mock<IAsyncResult>();
            mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(e);
            AsyncCallback receiveCallback = null;
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            mockSocket.Setup(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SocketFlags>(), It.IsAny<AsyncCallback>(), null)).Callback<byte[], int, int, SocketFlags, AsyncCallback, object>((b, offset, s, f, c, o) => {
                // Request available machines...
                b[0] = 0x05;
                b[1] = 0xff;

                receiveCallback = c;
                });

            mockSocket.SetupGet(s => s.Connected).Returns(true);
            mockSocket.Setup(s => s.EndReceive(It.IsAny<IAsyncResult>())).Returns(2);

            SocketConnection connection = new SocketConnection(mockSocket.Object);

            Mock<NewMessageDelegate> mockNewMessage = new Mock<NewMessageDelegate>();
            connection.OnNewMessage += mockNewMessage.Object;

            // Act
            receiveCallback?.Invoke(mockResult.Object);

            // Verify
            mockNewMessage.Verify(m => m(new byte[] { 0x05 }));
        }
    }
}
