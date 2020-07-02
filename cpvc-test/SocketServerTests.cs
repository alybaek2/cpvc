using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class SocketServerTests
    {
        [Test]
        public void Start()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            SocketServer socketServer = new SocketServer();

            // Act
            socketServer.Start(mockSocket.Object, 6128);

            // Verify
            mockSocket.Verify(s => s.Bind(It.IsAny<System.Net.EndPoint>()), Times.Once());
            mockSocket.Verify(s => s.Listen(1), Times.Once());
            mockSocket.Verify(s => s.BeginAccept(It.IsNotNull<AsyncCallback>(), null), Times.Once());
            mockSocket.VerifyNoOtherCalls();
        }

        [Test]
        public void StartTwice()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            SocketServer socketServer = new SocketServer();

            // Act
            socketServer.Start(mockSocket.Object, 6128);
            socketServer.Start(mockSocket.Object, 6128);

            // Verify
            mockSocket.Verify(s => s.Bind(It.IsAny<System.Net.EndPoint>()), Times.Exactly(2));
            mockSocket.Verify(s => s.Listen(1), Times.Exactly(2));
            mockSocket.Verify(s => s.BeginAccept(It.IsNotNull<AsyncCallback>(), null), Times.Exactly(2));
            mockSocket.Verify(s => s.Close(), Times.Once());
            mockSocket.VerifyNoOtherCalls();
        }

        [Test]
        public void Stop()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            SocketServer socketServer = new SocketServer();
            socketServer.Start(mockSocket.Object, 6128);

            // Act
            socketServer.Stop();

            // Verify
            mockSocket.Verify(s => s.Close(), Times.Once());
        }

        [Test]
        public void ClientConnect()
        {
            // Setup
            AsyncCallback callback = null;
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            Mock<ISocket> mockClientSocket = new Mock<ISocket>();
            Mock<IAsyncResult> mockAsyncResult = new Mock<IAsyncResult>();
            SocketServer socketServer = new SocketServer();
            mockSocket.Setup(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null)).Callback<AsyncCallback, object>((c, o) => callback = c);
            mockSocket.Setup(s => s.EndAccept(mockAsyncResult.Object)).Returns(mockClientSocket.Object);

            // Act
            socketServer.Start(mockSocket.Object, 6128);
            callback.Invoke(mockAsyncResult.Object);

            // Verify
            mockSocket.Verify(s => s.Bind(It.IsAny<System.Net.EndPoint>()), Times.Once());
            mockSocket.Verify(s => s.Listen(1), Times.Once());
            mockSocket.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null), Times.Exactly(2));
            mockClientSocket.Verify(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<System.Net.Sockets.SocketFlags>(), It.IsAny<AsyncCallback>(), It.IsAny<object>()));

            // Note that calling VerifyNoOtherCalls on one Mock<ISocket> checks all Mock<ISocket> instances.
            // This is apparently a known issue with Moq. See https://github.com/moq/moq4/issues/892.
            mockSocket.VerifyNoOtherCalls();
        }

        [Test]
        public void ClientConnectEndAcceptThrows()
        {
            // Setup
            AsyncCallback callback = null;
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            Mock<IAsyncResult> mockAsyncResult = new Mock<IAsyncResult>();
            SocketServer socketServer = new SocketServer();
            mockSocket.Setup(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null)).Callback<AsyncCallback, object>((c, o) => callback = c);
            mockSocket.Setup(s => s.EndAccept(mockAsyncResult.Object)).Throws<System.Net.Sockets.SocketException>();

            // Act
            socketServer.Start(mockSocket.Object, 6128);
            callback.Invoke(mockAsyncResult.Object);

            // Verify
            mockSocket.Verify(s => s.Bind(It.IsAny<System.Net.EndPoint>()), Times.Once());
            mockSocket.Verify(s => s.Listen(1), Times.Once());
            mockSocket.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null), Times.Once());
            mockSocket.Verify(s => s.Close(), Times.Once());
            mockSocket.Verify(s => s.EndAccept(mockAsyncResult.Object), Times.Once());
            mockSocket.VerifyNoOtherCalls();
        }

        [Test]
        public void ClientConnectAfterStop()
        {
            // Setup
            AsyncCallback callback = null;
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            Mock<IAsyncResult> mockAsyncResult = new Mock<IAsyncResult>();
            SocketServer socketServer = new SocketServer();
            mockSocket.Setup(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null)).Callback<AsyncCallback, object>((c, o) => callback = c);

            // Act
            socketServer.Start(mockSocket.Object, 6128);
            socketServer.Stop();
            callback.Invoke(mockAsyncResult.Object);

            // Verify
            mockSocket.Verify(s => s.Bind(It.IsAny<System.Net.EndPoint>()), Times.Once());
            mockSocket.Verify(s => s.Listen(1), Times.Once());
            mockSocket.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null), Times.Once());
            mockSocket.Verify(s => s.Close(), Times.Once());
            mockSocket.VerifyNoOtherCalls();
        }
    }
}
