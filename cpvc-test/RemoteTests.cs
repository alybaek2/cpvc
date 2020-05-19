using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class RemoteTests
    {
        private Remote _remote;
        private Mock<IConnection> _mockConnection;
        private Mock<ReceivePingDelegate> _mockPing;
        private Mock<ReceiveNameDelegate> _mockName;
        private Mock<ReceiveCoreActionDelegate> _mockCoreAction;
        private Mock<ReceiveCoreRequestDelegate> _mockCoreRequest;
        private Mock<ReceiveAvailableMachinesDelegate> _mockAvailableMachines;
        private Mock<ReceiveSelectMachineDelegate> _mockSelectMachine;
        private Mock<ReceiveRequestAvailableMachinesDelegate> _mockRequestAvailableMachines;

        private byte[] _nameMsg = new byte[] { 0x06, 0x03, 0x00, 0x00, 0x00, 0x61, 0x62, 0x63 };
        private string _name = "abc";

        private byte[] _pingMsg = new byte[] { 0x04, 0xff, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 };
        private bool _pingResponse = true;
        private UInt64 _pingId = 0x0123456789abcdef;


        private string _selectMachine = "abc";
        private byte[] _selectMachineMsg = new byte[]{ 0x02, 0x03, 0x00, 0x00, 0x00, 0x61, 0x62, 0x63 };

        private string[] _availableMachines = new string[] { "ABC", "DEF" };
        private byte[] _availableMachinesMsg = new byte[] {
                0x01,
                0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x41, 0x42, 0x43,
                0x03, 0x00, 0x00, 0x00, 0x44, 0x45, 0x46
            };

        private CoreAction _coreAction = CoreAction.KeyPress(0x01234567, Keys.A, true);
        private byte[] _coreActionMsg = new byte[] { 0x03, 0x01, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 58, 0xFF };

        private CoreRequest _coreRequest = CoreRequest.KeyPress(Keys.A, true);
        private byte[] _coreRequestMsg = new byte[] { 0x07, 0x01, 58, 0xFF };

        private byte[] _requestAvailableMachinesMsg = new byte[] { 0x05 };

        [SetUp]
        public void Setup()
        {
            _mockConnection = new Mock<IConnection>();
            _mockConnection.Setup(c => c.SendMessage(It.IsAny<byte[]>()));
            _mockConnection.SetupGet(c => c.IsConnected).Returns(true);

            _mockName = new Mock<ReceiveNameDelegate>();
            _mockName.Setup(p => p(It.IsAny<string>()));

            _mockCoreAction = new Mock<ReceiveCoreActionDelegate>();
            _mockCoreAction.Setup(c => c(It.IsAny<CoreAction>()));

            _mockCoreRequest = new Mock<ReceiveCoreRequestDelegate>();
            _mockCoreRequest.Setup(c => c(It.IsAny<CoreRequest>()));

            _mockPing = new Mock<ReceivePingDelegate>();
            _mockPing.Setup(p => p(It.IsAny<bool>(), It.IsAny<UInt64>()));

            _mockAvailableMachines = new Mock<ReceiveAvailableMachinesDelegate>();
            _mockAvailableMachines.Setup(a => a(It.IsAny<List<string>>()));

            _mockSelectMachine = new Mock<ReceiveSelectMachineDelegate>();
            _mockSelectMachine.Setup(s => s(It.IsAny<string>()));

            _mockRequestAvailableMachines = new Mock<ReceiveRequestAvailableMachinesDelegate>();
            _mockRequestAvailableMachines.Setup(r => r());

            _remote = new Remote(_mockConnection.Object);
            _remote.ReceiveName = _mockName.Object;
            _remote.ReceivePing = _mockPing.Object;
            _remote.ReceiveCoreAction = _mockCoreAction.Object;
            _remote.ReceiveCoreRequest = _mockCoreRequest.Object;
            _remote.ReceiveAvailableMachines = _mockAvailableMachines.Object;
            _remote.ReceiveSelectMachine = _mockSelectMachine.Object;
            _remote.ReceiveRequestAvailableMachines = _mockRequestAvailableMachines.Object;
        }

        [Test]
        public void SendSelectMachine()
        {
            // Act
            _remote.SendSelectMachine(_selectMachine);

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_selectMachineMsg));
        }

        [Test]
        public void SendRequestAvailableMachines()
        {
            // Act
            _remote.SendRequestAvailableMachines();

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_requestAvailableMachinesMsg));
        }

        [Test]
        public void SendCoreAction()
        {
            // Act
            _remote.SendCoreAction(_coreAction);

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_coreActionMsg));
        }

        [Test]
        public void SendCoreRequest()
        {
            // Act
            _remote.SendCoreRequest(_coreRequest);

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_coreRequestMsg));
        }

        [Test]
        public void SendName()
        {
            // Act
            _remote.SendName(_name);

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_nameMsg));
        }

        [Test]
        public void SendAvailableMachines()
        {
            // Act
            _remote.SendAvailableMachines(_availableMachines);

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_availableMachinesMsg));
        }

        [Test]
        public void SendPing()
        {
            // Act
            _remote.SendPing(_pingResponse, _pingId);

            // Verify
            _mockConnection.Verify(c => c.SendMessage(_pingMsg));
        }

        [Test]
        public void SendDisconnected()
        {
            // Setup
            _mockConnection.SetupGet(c => c.IsConnected).Returns(false);

            // Act
            _remote.SendPing(_pingResponse, _pingId);

            // Verify
            _mockConnection.VerifyGet(c => c.IsConnected);
            _mockConnection.VerifyNoOtherCalls();
        }

        [Test]
        public void SendWithoutConnection()
        {
            // Act and Verify
            Assert.Throws<Exception>(() => new Remote(null));
        }

        [Test]
        public void ReceiveUnknown([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceivePing = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, new byte[] { 99 });

            // Verify
            _mockPing.VerifyNoOtherCalls();
            _mockPing.VerifyNoOtherCalls();
            _mockName.VerifyNoOtherCalls();
            _mockCoreAction.VerifyNoOtherCalls();
            _mockCoreRequest.VerifyNoOtherCalls();
            _mockAvailableMachines.VerifyNoOtherCalls();
            _mockSelectMachine.VerifyNoOtherCalls();
            _mockRequestAvailableMachines.VerifyNoOtherCalls();
        }

        [Test]
        public void ReceivePing([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceivePing = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _pingMsg);

            // Verify
            if (handler)
            {
                _mockPing.Verify(p => p(_pingResponse, _pingId));
            }
            else
            {
                _mockPing.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ReceiveName([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceiveName = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _nameMsg);

            // Verify
            if (handler)
            {
                _mockName.Verify(p => p(_name));
            }
            else
            {
                _mockName.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ReceiveSelectMachine([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceiveSelectMachine = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _selectMachineMsg);

            // Verify
            if (handler)
            {
                _mockSelectMachine.Verify(s => s(_selectMachine));
            }
            else
            {
                _mockSelectMachine.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ReceiveCoreAction([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceiveCoreAction = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _coreActionMsg);

            // Verify
            if (handler)
            {
                _mockCoreAction.Verify(p => p(It.Is<CoreAction>(a => CoreActionsEqual(a, _coreAction))));
            }
            else
            {
                _mockCoreAction.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ReceiveCoreRequest([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceiveCoreRequest = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _coreRequestMsg);

            // Verify
            if (handler)
            {
                _mockCoreRequest.Verify(p => p(It.Is<CoreRequest>(r => CoreRequestsEqual(r, _coreRequest))));
            }
            else
            {
                _mockCoreRequest.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ReceiveRequestAvailableMachines([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceiveRequestAvailableMachines = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _requestAvailableMachinesMsg);

            // Verify
            if (handler)
            {
                _mockRequestAvailableMachines.Verify(p => p());
            }
            else
            {
                _mockRequestAvailableMachines.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ReceiveAvailableMachines([Values(false, true)] bool handler)
        {
            // Setup
            if (!handler)
            {
                _remote.ReceiveAvailableMachines = null;
            }

            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _availableMachinesMsg);

            // Verify
            if (handler)
            {
                _mockAvailableMachines.Verify(a => a(It.Is<List<string>>(m => m.SequenceEqual(_availableMachines))));
            }
            else
            {
                _mockAvailableMachines.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void Close()
        {
            // Act
            _remote.Close();

            // Verify
            _mockConnection.Verify(c => c.Close());
        }

        [Test]
        public void Dispose()
        {
            // Act
            _remote.Dispose();

            // Verify
            _mockConnection.Verify(c => c.Close());
        }
    }
}
