using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class MachineServerConnectionTests
    {
        private ReceivePingDelegate _receivePing;
        private ReceiveRequestAvailableMachinesDelegate _receiveRequestAvailableMachines;
        private ReceiveSelectMachineDelegate _receiveSelectMachine;

        private Mock<IRemote> _mockRemote;
        private MachineServerConnection _serverConnection;

        private List<ICoreMachine> _machines;

        [SetUp]
        public void Setup()
        {
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            mockMachine.SetupGet(m => m.Core).Returns(Core.Create(Core.LatestVersion, Core.Type.CPC6128));

            _machines = new List<ICoreMachine>
            {
                mockMachine.Object
            };

            _mockRemote = new Mock<IRemote>();

            _receivePing = null;
            _mockRemote.SetupSet(r => r.ReceivePing = It.IsAny<ReceivePingDelegate>()).Callback<ReceivePingDelegate>(callback => _receivePing = callback);
            _mockRemote.SetupSet(r => r.ReceiveRequestAvailableMachines = It.IsAny<ReceiveRequestAvailableMachinesDelegate>()).Callback<ReceiveRequestAvailableMachinesDelegate>(callback => _receiveRequestAvailableMachines = callback);
            _mockRemote.SetupSet(r => r.ReceiveSelectMachine = It.IsAny<ReceiveSelectMachineDelegate>()).Callback<ReceiveSelectMachineDelegate>(callback => _receiveSelectMachine = callback);

            _serverConnection = new MachineServerConnection(_mockRemote.Object, _machines);
        }

        [Test]
        public void ReceivePing()
        {
            // Act
            _receivePing(false, 1234);

            // Verify
            _mockRemote.Verify(r => r.SendPing(true, 1234));
        }

        [Test]
        public void ReceiveRequestAvailableMachines()
        {
            // Act
            _receiveRequestAvailableMachines();

            // Verify
            _mockRemote.Verify(r => r.SendAvailableMachines(new List<string> { _machines[0].Name }));
        }

        [Test]
        public void ReceiveSelectMachine()
        {
            // Act
            _receiveSelectMachine(_machines[0].Name);

            // Verify
            _mockRemote.Verify(r => r.SendName(_machines[0].Name));
            _mockRemote.Verify(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.LoadCore)));
        }
    }
}
