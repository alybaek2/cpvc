﻿using Moq;
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

        [TestCase(new byte[] { 0x01, 0x02, 0xff       }, new byte[] { 0x01, 0x02 })]
        [TestCase(new byte[] { 0x01, 0xfe, 0x00, 0xff }, new byte[] { 0x01, 0xfe })]
        [TestCase(new byte[] { 0x01, 0xfe, 0x01, 0xff }, new byte[] { 0x01, 0xff })]
        public void Receive(byte[] message, byte[] expected)
        {
            // Setup
            System.Threading.ManualResetEvent e = new System.Threading.ManualResetEvent(true);
            Mock<IAsyncResult> mockResult = new Mock<IAsyncResult>();
            mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(e);
            AsyncCallback receiveCallback = null;
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            mockSocket.Setup(s => s.BeginReceive(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<SocketFlags>(), It.IsAny<AsyncCallback>(), null)).Callback<byte[], int, int, SocketFlags, AsyncCallback, object>((b, offset, s, f, c, o) => {
                message.CopyTo(b, 0);

                receiveCallback = c;
            });

            mockSocket.SetupGet(s => s.Connected).Returns(true);
            mockSocket.Setup(s => s.EndReceive(It.IsAny<IAsyncResult>())).Returns(message.Length);

            SocketConnection connection = new SocketConnection(mockSocket.Object);

            Mock<NewMessageDelegate> mockNewMessage = new Mock<NewMessageDelegate>();
            connection.OnNewMessage += mockNewMessage.Object;

            // Act
            receiveCallback?.Invoke(mockResult.Object);

            // Verify
            mockNewMessage.Verify(m => m(expected));
        }

        [TestCase(new byte[] { 0x01, 0x02 }, new byte[] { 0x01, 0x02, 0xff })]
        [TestCase(new byte[] { 0x01, 0xfe }, new byte[] { 0x01, 0xfe, 0x00, 0xff })]
        [TestCase(new byte[] { 0x01, 0xff }, new byte[] { 0x01, 0xfe, 0x01, 0xff })]
        public void Send(byte[] message, byte[] expected)
        {
            // Setup
            System.Threading.ManualResetEvent e = new System.Threading.ManualResetEvent(true);
            Mock<IAsyncResult> mockResult = new Mock<IAsyncResult>();
            mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(e);
            Mock<ISocket> mockSocket = new Mock<ISocket>();

            SocketAsyncEventArgs sendAsyncArgs = null;
            mockSocket.Setup(s => s.SendAsync(It.IsAny<SocketAsyncEventArgs>())).Callback<SocketAsyncEventArgs>((args) =>
            {
                sendAsyncArgs = args;
            });

            mockSocket.SetupGet(s => s.Connected).Returns(true);
            SocketConnection connection = new SocketConnection(mockSocket.Object);

            // Act
            connection.SendMessage(message);

            // Verify
            Assert.AreEqual(expected, sendAsyncArgs.Buffer);
        }
    }
}
