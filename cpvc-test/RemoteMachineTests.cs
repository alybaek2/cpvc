using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;

namespace CPvC.Test
{
    public class RemoteMachineTests
    {
        private ReceiveCoreActionDelegate _receiveCoreAction;
        private ReceiveRequestAvailableMachinesDelegate _receiveRequestAvailableMachines;
        private ReceiveSelectMachineDelegate _receiveSelectMachine;
        private ReceivePingDelegate _receivePing;
        private ReceiveNameDelegate _receiveName;
        private CloseConnectionDelegate _closeConnection;
        private Mock<IRemote> _mockRemote;
        private Mock<MachineAuditorDelegate> _mockAuditor;

        [SetUp]
        public void Setup()
        {
            _mockRemote = new Mock<IRemote>();
            _mockAuditor = new Mock<MachineAuditorDelegate>();

            _receiveCoreAction = null;
            _mockRemote.SetupSet(r => r.ReceivePing = It.IsAny<ReceivePingDelegate>()).Callback<ReceivePingDelegate>(callback => _receivePing = callback);
            _mockRemote.SetupSet(r => r.ReceiveName = It.IsAny<ReceiveNameDelegate>()).Callback<ReceiveNameDelegate>(callback => _receiveName = callback);
            _mockRemote.SetupSet(r => r.ReceiveRequestAvailableMachines = It.IsAny<ReceiveRequestAvailableMachinesDelegate>()).Callback<ReceiveRequestAvailableMachinesDelegate>(callback => _receiveRequestAvailableMachines = callback);
            _mockRemote.SetupSet(r => r.ReceiveSelectMachine = It.IsAny<ReceiveSelectMachineDelegate>()).Callback<ReceiveSelectMachineDelegate>(callback => _receiveSelectMachine = callback);
            _mockRemote.SetupSet(r => r.ReceiveCoreAction = It.IsAny<ReceiveCoreActionDelegate>()).Callback<ReceiveCoreActionDelegate>(callback => _receiveCoreAction = callback);
            _mockRemote.SetupSet(r => r.CloseConnection = It.IsAny<CloseConnectionDelegate>()).Callback<CloseConnectionDelegate>(callback => _closeConnection = callback);
        }

        [Test]
        public void ReceiveCoreAction()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                machine.Start();

                // Act
                _receiveCoreAction(CoreAction.RunUntilForce(0, 1));
                Thread.Sleep(100);
                machine.Stop();

                // Verify
                Assert.Greater(machine.Ticks, 0);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReceiveCoreActionAuditor(bool closed)
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                machine.Auditors += _mockAuditor.Object;

                if (closed)
                {
                    machine.Close();
                }

                // Act
                _receiveCoreAction(CoreAction.RunUntilForce(0, 1));

                // Verify
                _mockAuditor.Verify(a => a(It.Is<CoreAction>(action => action.Type == CoreRequest.Types.RunUntilForce)), Times.Exactly(closed ? 0 : 1));
            }
        }

        // Ensures that pings are "throttled". That is, if two CoreActions are processed
        // in quick succession, only one ping will be sent. This test could probably be
        // a bit more precise in terms of testing the 100 ms threshold for sending
        // successive ping messages.
        [Test]
        public void SendOnePing()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
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

        [Test]
        public void CloseConnection()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Act
                _closeConnection();

                // Verify
                Assert.AreEqual("Connection closed", machine.Status);
            }
        }

        [Test]
        public void ReceiveName()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Act
                _receiveName("Test");

                // Verify
                Assert.AreEqual("Test (remote)", machine.Name);
            }
        }

        [Test]
        public void CanClose()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Verify
                Assert.True(machine.CanClose());
            }
        }

        [Test]
        public void Key()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Act
                machine.Key(Keys.A, true);

                // Verify
                _mockRemote.Verify(remote => remote.SendCoreRequest(It.Is<CoreRequest>(request => request.Type == CoreRequest.Types.KeyPress && request.KeyCode == Keys.A && request.KeyDown)), Times.Once());
            }
        }

        [Test]
        public void LoadDisc()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Act
                machine.LoadDisc(1, new byte[] { 0x01, 0x02 });

                // Verify
                _mockRemote.Verify(remote => remote.SendCoreRequest(It.Is<CoreRequest>(request => request.Type == CoreRequest.Types.LoadDisc && request.Drive == 1 && request.MediaBuffer.GetBytes().SequenceEqual(new byte[] { 0x01, 0x02 }))), Times.Once());
            }
        }

        [Test]
        public void LoadTape()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Act
                machine.LoadTape(new byte[] { 0x01, 0x02 });

                // Verify
                _mockRemote.Verify(remote => remote.SendCoreRequest(It.Is<CoreRequest>(request => request.Type == CoreRequest.Types.LoadTape && request.MediaBuffer.GetBytes().SequenceEqual(new byte[] { 0x01, 0x02 }))), Times.Once());
            }
        }

        [Test]
        public void Reset()
        {
            // Setup
            using (RemoteMachine machine = new RemoteMachine(_mockRemote.Object))
            {
                // Act
                machine.Reset();

                // Verify
                _mockRemote.Verify(remote => remote.SendCoreRequest(It.Is<CoreRequest>(request => request.Type == CoreRequest.Types.Reset)), Times.Once());
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Dispose(int displayCount)
        {
            // Setup
            RemoteMachine machine = new RemoteMachine(_mockRemote.Object);

            // Act
            for (int c = 0; c < displayCount; c++)
            {
                machine.Dispose();
            }

            // Verify
            Assert.Null(machine.Core);
            _mockRemote.Verify(r => r.Dispose(), Times.Exactly(displayCount));
        }
    }
}
