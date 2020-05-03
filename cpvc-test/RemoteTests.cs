﻿using Moq;
using NUnit.Framework;
using System;

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

        private byte[] _requestAvailableMachines = new byte[] { 0x05 };

        private bool CoreRequestsEqual(CoreRequest request1, CoreRequest request2)
        {
            if (request1 == request2)
            {
                return true;
            }

            if (request1 == null || request2 == null)
            {
                return false;
            }

            if (request1.Type != request2.Type)
            {
                return false;
            }

            switch (request1.Type)
            {
                case CoreRequest.Types.CoreVersion:
                    return request1.Version == request2.Version;
                case CoreRequest.Types.KeyPress:
                    return request1.KeyCode == request2.KeyCode && request1.KeyDown == request2.KeyDown;
                case CoreRequest.Types.LoadCore:
                    return request1.CoreState == request2.CoreState;
                case CoreRequest.Types.LoadDisc:
                    return request1.Drive == request2.Drive && request1.MediaBuffer == request2.MediaBuffer;
                case CoreRequest.Types.LoadTape:
                    return request1.MediaBuffer == request2.MediaBuffer;
                case CoreRequest.Types.Quit:
                    return true;
                case CoreRequest.Types.Reset:
                    return true;
                case CoreRequest.Types.RunUntilForce:
                    return request1.StopTicks == request2.StopTicks;
            }

            return false;
        }

        private bool CoreActionsEqual(CoreAction action1, CoreAction action2)
        {
            if (!CoreRequestsEqual(action1, action2))
            {
                return false;
            }

            if (action1 == action2)
            {
                return true;
            }

            if (action1 == null || action2 == null)
            {
                return false;
            }

            if (action1.Type != action2.Type)
            {
                return false;
            }

            if (action1.Ticks != action2.Ticks)
            {
                return false;
            }


            return true;
        }

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

            _remote = new Remote(_mockConnection.Object);
            _remote.ReceiveName = _mockName.Object;
            _remote.ReceivePing = _mockPing.Object;
            _remote.ReceiveCoreAction = _mockCoreAction.Object;
            _remote.ReceiveCoreRequest = _mockCoreRequest.Object;
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
            _mockConnection.Verify(c => c.SendMessage(_requestAvailableMachines));
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
        public void ReceivePing()
        {
            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _pingMsg);

            // Verify
            _mockPing.Verify(p => p(_pingResponse, _pingId));
        }

        [Test]
        public void ReceiveName()
        {
            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _nameMsg);

            // Verify
            _mockName.Verify(p => p(_name));
        }

        [Test]
        public void ReceiveCoreAction()
        {
            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _coreActionMsg);

            // Verify
            _mockCoreAction.Verify(p => p(It.Is<CoreAction>(a => CoreActionsEqual(a, _coreAction))));
        }

        [Test]
        public void ReceiveCoreRequest()
        {
            // Act
            _mockConnection.Raise(c => c.OnNewMessage += null, _coreRequestMsg);

            // Verify
            _mockCoreRequest.Verify(p => p(It.Is<CoreRequest>(r => CoreRequestsEqual(r, _coreRequest))));
        }
    }
}