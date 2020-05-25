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
    }
}
