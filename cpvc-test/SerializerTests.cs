using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class SerializerTests
    {
        private byte[] _selectMachineBytes = new byte[] { 0x03, 0x00, 0x00, 0x00, 0x61, 0x62, 0x63 };
        private string _selectMachine = "abc";

        private string[] _availableMachines = new string[] { "ABC", "DEF" };
        private byte[] _availableMachinesBytes = new byte[] {
                0x02, 0x00, 0x00, 0x00,
                0x03, 0x00, 0x00, 0x00, 0x41, 0x42, 0x43,
                0x03, 0x00, 0x00, 0x00, 0x44, 0x45, 0x46
            };

        private CoreAction _coreAction = CoreAction.KeyPress(0x01234567, Keys.A, true);
        private byte[] _coreActionBytes = new byte[] { 0x01, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 58, 0xFF };

        private CoreRequest _coreRequest = CoreRequest.KeyPress(Keys.A, true);
        private byte[] _coreRequestBytes = new byte[] { 0x01, 58, 0xFF };

        [Test]
        public void CoreRequestToBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act
            Serializer.CoreRequestToBytes(stream, _coreAction);

            // Verify
            Assert.AreEqual(_coreRequestBytes, stream.AsBytes());
        }

        [Test]
        public void CoreRequestFromBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(_coreRequestBytes);

            // Act
            CoreRequest coreRequest = Serializer.CoreRequestFromBytes(stream);

            // Verify
            Assert.True(CoreRequestsEqual(_coreRequest, coreRequest));
        }


        [Test]
        public void CoreActionToBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act
            Serializer.CoreActionToBytes(stream, _coreAction);

            // Verify
            Assert.AreEqual(_coreActionBytes, stream.AsBytes());
        }

        [Test]
        public void CoreActionFromBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(_coreActionBytes);

            // Act
            CoreAction coreAction = Serializer.CoreActionFromBytes(stream);

            // Verify
            Assert.True(CoreActionsEqual(_coreAction, coreAction));
        }

        [Test]
        public void SelectMachineToBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act
            Serializer.SelectMachineToBytes(stream, _selectMachine);

            // Verify
            Assert.AreEqual(_selectMachineBytes, stream.AsBytes());
        }

        [Test]
        public void SelectMachineFromBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(_selectMachineBytes);

            // Act
            string selectMachine = Serializer.SelectMachineFromBytes(stream);

            // Verify
            Assert.AreEqual(_selectMachine, selectMachine);
        }

        [Test]
        public void AvailableMachinesToBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act
            Serializer.AvailableMachinesToBytes(stream, _availableMachines);

            // Verify
            Assert.AreEqual(_availableMachinesBytes, stream.AsBytes());
        }

        [Test]
        public void AvailableMachinesFromBytes()
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(_availableMachinesBytes);

            // Act
            List<string> availableMachines = Serializer.AvailableMachinesFromBytes(stream);

            // Verify
            Assert.AreEqual(_availableMachines, availableMachines);
        }
    }
}
