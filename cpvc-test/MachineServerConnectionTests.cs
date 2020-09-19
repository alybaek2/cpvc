using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        private List<Core> _cores;

        private Mock<ICoreMachine>[] _mockMachines;

        [SetUp]
        public void Setup()
        {
            _cores = new List<Core>();
            _mockMachines = new Mock<ICoreMachine>[2];

            for (int i = 0; i < _mockMachines.Length; i++)
            {
                Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
                core.KeepRunning = false;

                _cores.Add(core);

                _mockMachines[i] = new Mock<ICoreMachine>();

                _mockMachines[i].SetupGet(m => m.Name).Returns(String.Format("Machine{0}", i));
                _mockMachines[i].SetupGet(m => m.Core).Returns(core);
                _mockMachines[i].SetupGet(m => m.Ticks).Returns(() => core.Ticks);
                _mockMachines[i].SetupSet(m => m.Auditors = It.IsAny<MachineAuditorDelegate>()).Callback<MachineAuditorDelegate>(callback => core.Auditors += (Core c, CoreRequest r, CoreAction a) => callback(a));
            }

            _machines = _mockMachines.Select(m => m.Object).ToList();

            _mockRemote = new Mock<IRemote>();

            _receivePing = null;
            _mockRemote.SetupSet(r => r.ReceivePing = It.IsAny<ReceivePingDelegate>()).Callback<ReceivePingDelegate>(callback => _receivePing = callback);
            _mockRemote.SetupSet(r => r.ReceiveRequestAvailableMachines = It.IsAny<ReceiveRequestAvailableMachinesDelegate>()).Callback<ReceiveRequestAvailableMachinesDelegate>(callback => _receiveRequestAvailableMachines = callback);
            _mockRemote.SetupSet(r => r.ReceiveSelectMachine = It.IsAny<ReceiveSelectMachineDelegate>()).Callback<ReceiveSelectMachineDelegate>(callback => _receiveSelectMachine = callback);
            _mockRemote.SetupSet(r => r.ReceiveCoreRequest = It.IsAny<ReceiveCoreRequestDelegate>()).Callback<ReceiveCoreRequestDelegate>(callback => _receiveCoreAction = callback);

            _serverConnection = new MachineServerConnection(_mockRemote.Object, _machines);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReceivePing(bool response)
        {
            // Act
            _receivePing(response, 1234);

            // Verify
            _mockRemote.Verify(r => r.SendPing(true, 1234), Times.Exactly(response ? 0 : 1));
        }

        [Test]
        public void ReceiveRequestAvailableMachines()
        {
            // Act
            _receiveRequestAvailableMachines();

            // Verify
            IEnumerable<string> machineNames = _machines.Select(m => m.Name);
            _mockRemote.Verify(r => r.SendAvailableMachines(machineNames));
        }

        [Test]
        public void ReceiveRequestNoAvailableMachines()
        {
            // Setup
            _machines.Clear();

            // Act
            _receiveRequestAvailableMachines();

            // Verify
            _mockRemote.Verify(r => r.SendAvailableMachines(new List<string>()));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void ReceiveSelectMachine(int selectCount)
        {
            // Act
            for (int i = 0; i < selectCount; i++)
            {
                _receiveSelectMachine(_machines[0].Name);
            }

            // Verify
            _mockRemote.Verify(r => r.SendName(_machines[0].Name), Times.Once());
            _mockRemote.Verify(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.LoadCore)), Times.Once());
            _mockMachines[0].VerifySet(m => m.Auditors = It.IsAny<MachineAuditorDelegate>(), Times.Once());
        }

        [Test]
        public void ReceiveSelectDifferentMachines()
        {
            // Setup
            _mockRemote.SetupSet(r => r.ReceiveSelectMachine = It.IsAny<ReceiveSelectMachineDelegate>());
            _mockMachines[0].SetupSet(m => m.Auditors = It.Is<MachineAuditorDelegate>(d => d != null));
            _mockRemote.Setup(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.LoadCore)));
            _mockRemote.Setup(r => r.SendName(_machines[1].Name));
            _mockRemote.Setup(r => r.SendName(_machines[0].Name));

            // Act
            _receiveSelectMachine(_machines[0].Name);
            _receiveSelectMachine(_machines[1].Name);
            _receiveSelectMachine("UnknownMachine");

            // Verify - Todo: verify the sequence of these calls.
            _mockRemote.Verify(r => r.SendName(_machines[0].Name), Times.Once());
            _mockRemote.Verify(r => r.SendName(_machines[1].Name), Times.Once());
            _mockRemote.Verify(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.LoadCore)), Times.Exactly(2));
            _mockMachines[0].VerifySet(m => m.Auditors = It.Is<MachineAuditorDelegate>(d => d != null), Times.Once());
            _mockMachines[0].VerifySet(m => m.Auditors = null, Times.Once());
            _mockMachines[1].VerifySet(m => m.Auditors = It.Is<MachineAuditorDelegate>(d => d != null), Times.Once());
            _mockMachines[1].VerifySet(m => m.Auditors = null, Times.Once());
        }

        [Test]
        public void ReceiveCoreAction()
        {
            // Setup
            CoreAction coreAction = CoreAction.RunUntilForce(0, 1000, null);

            // Act
            _receiveSelectMachine(_mockMachines[0].Object.Name);
            _receiveCoreAction(coreAction);
            _cores[0].Start();
            Thread.Sleep(100);
            _cores[0].Stop();

            // Verify
            Assert.AreEqual(1000, _machines[0].Ticks);
            _mockRemote.Verify(r => r.SendName(_mockMachines[0].Object.Name));
            _mockRemote.Verify(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.LoadCore)));
            _mockMachines[0].VerifySet(m => m.Auditors = It.IsAny<MachineAuditorDelegate>());
        }

        [Test]
        public void MachineAuditor()
        {
            // Setup
            _receiveSelectMachine(_machines[0].Name);

            // Act
            _cores[0].PushRequest(CoreRequest.RunUntilForce(1));
            RunForAWhile(_cores[0], 1);

            // Verify
            _mockRemote.Verify(r => r.SendCoreAction(It.Is<CoreAction>(a => a.Type == CoreRequest.Types.RunUntilForce)));
        }
    }
}
