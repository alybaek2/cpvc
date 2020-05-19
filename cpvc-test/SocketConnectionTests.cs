using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
