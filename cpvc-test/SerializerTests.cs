﻿using NUnit.Framework;
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

        static object[] CoreActionCases =
        {
            new object[] { CoreAction.KeyPress(0x01234567, Keys.A, true), new byte[] { 0x01, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 58, 0xFF } },
            new object[] { CoreAction.Reset(0x01234567), new byte[] { 0x02, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00 } },
            new object[] { CoreAction.LoadDisc(0x01234567, 1, new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x03, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } },
            new object[] { CoreAction.LoadTape(0x01234567, new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x04, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } },
            new object[] { CoreAction.RunUntilForce(0x01234567, 0x0123456789abcdef ), new byte[] { 0x05, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 } },
            new object[] { CoreAction.LoadCore(0x01234567, new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x06, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } },
            new object[] { CoreAction.CoreVersion(0x01234567, 257), new byte[] { 0x07, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00 } }
        };

        static object[] CoreRequestCases =
        {
            new object[] { CoreRequest.KeyPress(Keys.A, true), new byte[] { 0x01, 58, 0xFF } }
        };

        [TestCaseSource("CoreRequestCases")]
        public void CoreRequestToBytes(CoreRequest coreRequest, byte[] expectedBytes)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act
            Serializer.CoreRequestToBytes(stream, coreRequest);

            // Verify
            Assert.AreEqual(expectedBytes, stream.AsBytes());
        }

        [TestCaseSource("CoreRequestCases")]
        public void CoreRequestFromBytes(CoreRequest expectedCoreRequest, byte[] expectedBytes)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(expectedBytes);

            // Act
            CoreRequest coreRequest = Serializer.CoreRequestFromBytes(stream);

            // Verify
            Assert.True(CoreRequestsEqual(expectedCoreRequest, coreRequest));
        }

        [TestCaseSource("CoreActionCases")]
        public void CoreActionToBytes(CoreAction coreAction, byte[] expectedBytes)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act
            Serializer.CoreActionToBytes(stream, coreAction);

            // Verify
            Assert.AreEqual(expectedBytes, stream.AsBytes());
        }

        [TestCaseSource("CoreActionCases")]
        public void CoreActionFromBytes(CoreAction expectedCoreAction, byte[] bytes)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(bytes);

            // Act
            CoreAction coreAction = Serializer.CoreActionFromBytes(stream);

            // Verify
            Assert.True(CoreActionsEqual(expectedCoreAction, coreAction));
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
