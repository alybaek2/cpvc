using Moq;
using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class RemoteTests
    {
        [Test]
        public void SendSelectMachine()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendSelectMachine("abc");

            // Verify
            mockConnection.Verify(c => c.SendMessage(new byte[] { 0x02, 0x03, 0x00, 0x00, 0x00, 0x61, 0x62, 0x63 }));
        }

        [Test]
        public void SendRequestAvailableMachines()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendRequestAvailableMachines();

            // Verify
            mockConnection.Verify(c => c.SendMessage(new byte[] { 0x05 }));
        }

        [Test]
        public void SendCoreAction()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendCoreAction(CoreAction.KeyPress(0x01234567, Keys.A, true));

            // Verify
            mockConnection.Verify(c => c.SendMessage(new byte[] { 0x03, 0x01, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 58, 0xFF }));
        }

        [Test]
        public void SendCoreRequest()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendCoreRequest(CoreRequest.KeyPress(Keys.A, true));

            // Verify
            mockConnection.Verify(c => c.SendMessage(new byte[] { 0x07, 0x01, 58, 0xFF }));
        }

        [Test]
        public void SendName()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendName("abc");

            // Verify
            mockConnection.Verify(c => c.SendMessage(new byte[] { 0x06, 0x03, 0x00, 0x00, 0x00, 0x61, 0x62, 0x63 }));
        }

        [Test]
        public void SendAvailableMachines()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendAvailableMachines(new string[] { "ABC", "DEF" });

            // Verify
            byte[] expectedMsg = new byte[] {
                0x01,
                0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x41, 0x42, 0x43,
                0x03, 0x00, 0x00, 0x00, 0x44, 0x45, 0x46
            };

            mockConnection.Verify(c => c.SendMessage(expectedMsg));
        }

        [Test]
        public void SendPing()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Remote remote = new Remote(mockConnection.Object);

            // Act
            remote.SendPing(true, 0x0123456789abcdef);

            // Verify
            byte[] expectedMsg = new byte[] {
                0x04,
                0xff,
                0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01
            };

            mockConnection.Verify(c => c.SendMessage(expectedMsg));
        }

        [Test]
        public void ReceivePing()
        {
            // Setup
            Mock<IConnection> mockConnection = new Mock<IConnection>();
            mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            Mock<ReceivePingDelegate> mockPing = new Mock<ReceivePingDelegate>();
            mockPing.Setup(p => p(It.IsAny<bool>(), It.IsAny<UInt64>()));

            Remote remote = new Remote(mockConnection.Object);
            remote.ReceivePing += mockPing.Object;

            byte[] msg = new byte[] { 0x04, 0xff, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };

            // Act
            mockConnection.Raise(c => c.OnNewMessage += null, msg);

            // Verify
            mockPing.Verify(p => p(true, 0x0123456789abcdef));
        }
    }
}
