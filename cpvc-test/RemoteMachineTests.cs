using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class RemoteMachineTests
    {
        private ReceiveCoreActionDelegate _receiveCoreAction;
        private ReceiveRequestAvailableMachinesDelegate _receiveRequestAvailableMachines;
        private ReceiveSelectMachineDelegate _receiveSelectMachine;
        private ReceivePingDelegate _receivePing;
        private Mock<IRemote> _mockRemote;

        [SetUp]
        public void Setup()
        {
            _mockRemote = new Mock<IRemote>();

            _receiveCoreAction = null;
            _mockRemote.SetupSet(r => r.ReceivePing = It.IsAny<ReceivePingDelegate>()).Callback<ReceivePingDelegate>(callback => _receivePing = callback);
            _mockRemote.SetupSet(r => r.ReceiveRequestAvailableMachines = It.IsAny<ReceiveRequestAvailableMachinesDelegate>()).Callback<ReceiveRequestAvailableMachinesDelegate>(callback => _receiveRequestAvailableMachines = callback);
            _mockRemote.SetupSet(r => r.ReceiveSelectMachine = It.IsAny<ReceiveSelectMachineDelegate>()).Callback<ReceiveSelectMachineDelegate>(callback => _receiveSelectMachine = callback);
            _mockRemote.SetupSet(r => r.ReceiveCoreAction = It.IsAny<ReceiveCoreActionDelegate>()).Callback<ReceiveCoreActionDelegate>(callback => _receiveCoreAction = callback);
        }

        [Test]
        public void ReceiveCoreAction()
        {
            // Setup
            RemoteMachine machine = new RemoteMachine(_mockRemote.Object);
            machine.Start();

            // Act
            _receiveCoreAction(CoreAction.RunUntilForce(0, 1));
            Thread.Sleep(100);
            machine.Stop();

            // Verify
            Assert.Greater(machine.Ticks, 0);
        }

        // Ensures that pings are "throttled". That is, if two CoreActions are processed
        // in quick succession, only one ping will be sent. This test could probably be
        // a bit more precise in terms of testing the 100 ms threshold for sending
        // successive ping messages.
        [Test]
        public void SendOnePing()
        {
            // Setup
            RemoteMachine machine = new RemoteMachine(_mockRemote.Object);
            machine.Start();

            // Act
            _receiveCoreAction(CoreAction.RunUntilForce(0, 1));
            _receiveCoreAction(CoreAction.RunUntilForce(0, 1));
            Thread.Sleep(100);
            machine.Stop();

            // Verify
            _mockRemote.Verify(r => r.SendPing(false, It.IsAny<UInt64>()), Times.Once());
        }
    }
}
