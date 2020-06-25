using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class MachineServerConnectionTests
    {
        private ReceivePingDelegate _receivePing;
        private ReceiveRequestAvailableMachinesDelegate _receiveRequestAvailableMachines;
        private ReceiveSelectMachineDelegate _receiveSelectMachine;
        private ReceiveCoreRequestDelegate _receiveCoreAction;

        private Mock<IRemote> _mockRemote;
        private MachineServerConnection _serverConnection;

        private List<ICoreMachine> _machines;
        private Mock<ICoreMachine> _mockMachine;

        [SetUp]
        public void Setup()
        {
            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            core.KeepRunning = false;

            _mockMachine = new Mock<ICoreMachine>();
            _mockMachine.SetupGet(m => m.Core).Returns(core);
            _mockMachine.SetupGet(m => m.Ticks).Returns(() => core.Ticks);

            _machines = new List<ICoreMachine>
            {
                _mockMachine.Object
            };

            _mockRemote = new Mock<IRemote>();

            _receivePing = null;
            _mockRemote.SetupSet(r => r.ReceivePing = It.IsAny<ReceivePingDelegate>()).Callback<ReceivePingDelegate>(callback => _receivePing = callback);
            _mockRemote.SetupSet(r => r.ReceiveRequestAvailableMachines = It.IsAny<ReceiveRequestAvailableMachinesDelegate>()).Callback<ReceiveRequestAvailableMachinesDelegate>(callback => _receiveRequestAvailableMachines = callback);
            _mockRemote.SetupSet(r => r.ReceiveSelectMachine = It.IsAny<ReceiveSelectMachineDelegate>()).Callback<ReceiveSelectMachineDelegate>(callback => _receiveSelectMachine = callback);
            _mockRemote.SetupSet(r => r.ReceiveCoreRequest = It.IsAny<ReceiveCoreRequestDelegate>()).Callback<ReceiveCoreRequestDelegate>(callback => _receiveCoreAction = callback);

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
            _mockMachine.VerifySet(m => m.Auditors = It.IsAny<MachineAuditorDelegate>());
        }

        [Test]
        public void ReceiveCoreAction()
        {
            // Setup
            CoreAction coreAction = CoreAction.RunUntilForce(0, 1000);

            // Act
            _receiveSelectMachine(_mockMachine.Object.Name);
            _receiveCoreAction(coreAction);
            _mockMachine.Object.Core.Start();
            Thread.Sleep(100);
            _mockMachine.Object.Core.Stop();

            // Verify
            Assert.AreEqual(1000, _machines[0].Ticks);
            _mockRemote.Verify(r => r.SendName(_mockMachine.Object.Name));
            _mockRemote.Verify(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.LoadCore)));
            _mockMachine.VerifySet(m => m.Auditors = It.IsAny<MachineAuditorDelegate>());
        }
    }
}
