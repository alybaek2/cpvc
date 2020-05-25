using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace CPvC.Test
{
    public class MachineServerListenerTests
    {
        [Test]
        public void StartAndStop()
        {
            // Setup
            MachineServerListener listener = new MachineServerListener(null);
            Mock<ISocket> mockSocket = new Mock<ISocket>();

            // Act
            listener.Start(mockSocket.Object, 6128);
            listener.Stop();

            // Verify
            mockSocket.Verify(s => s.Listen(It.IsAny<int>()));
            mockSocket.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), It.IsAny<object>()));
            mockSocket.Verify(s => s.Close());
        }

        [Test]
        public void Connect()
        {
            // Setup
            System.Threading.ManualResetEvent e = new System.Threading.ManualResetEvent(true);
            Mock<IAsyncResult> mockResult = new Mock<IAsyncResult>();
            mockResult.SetupGet(r => r.AsyncWaitHandle).Returns(e);

            List<MachineViewModel> models = new List<MachineViewModel>();

            MachineServerListener listener = new MachineServerListener(models);
            Mock<ISocket> mockSocketListener = new Mock<ISocket>();
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            AsyncCallback callback = null;
            mockSocketListener.Setup(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null)).Callback<AsyncCallback, object>((c, o) => callback = c);
            mockSocketListener.Setup(s => s.EndAccept(mockResult.Object)).Returns(mockSocket.Object);
            mockSocket.Setup(s => s.BeginConnect(It.IsAny<System.Net.EndPoint>(), It.IsAny<AsyncCallback>(), null)).Returns(mockResult.Object);

            // Act
            listener.Start(mockSocketListener.Object, 6128);
            callback?.Invoke(mockResult.Object);

            // Verify
            Assert.AreEqual(1, listener.Connections.Count());
        }
    }
}
