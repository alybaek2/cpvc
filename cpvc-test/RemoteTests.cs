using Moq;
using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class RemoteTests
    {
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
        public void NewMessage()
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
