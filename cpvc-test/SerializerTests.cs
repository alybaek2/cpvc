using NUnit.Framework;
using System;
using System.Collections.Generic;
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

        static private object[] CoreRequestCases =
        {
            new object[] { MachineRequest.KeyPress(Keys.A, true), new byte[] { 0x01, 58, 0xFF } , false },
            new object[] { MachineRequest.Reset(), new byte[] { 0x02 } , false },
            new object[] { MachineRequest.LoadDisc(1, new byte[] { 0x01, 0x02 }), new byte[] { 0x03, 0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineRequest.LoadTape(new byte[] { 0x01, 0x02 }), new byte[] { 0x04, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineRequest.RunUntil(0x0123456789abcdef ), new byte[] { 0x05, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 } , false },
            new object[] { MachineRequest.LoadCore(new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x06, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineRequest.CoreVersion(257), new byte[] { 0x07, 0x01, 0x01, 0x00, 0x00 } , false },
            new object[] { MachineRequest.CreateSnapshot(42), new byte[] { 0x08, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { MachineRequest.DeleteSnapshot(42), new byte[] { 0x09, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { MachineRequest.RevertToSnapshot(42), new byte[] { 0x0a, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { new MachineRequest((MachineRequest.Types) 99), new byte[] { 99 }, true }
        };

        static private object[] CoreActionCases =
        {
            new object[] { MachineAction.KeyPress(0x01234567, Keys.A, true), new byte[] { 0x01, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 58, 0xFF } , false },
            new object[] { MachineAction.Reset(0x01234567), new byte[] { 0x02, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00 } , false },
            new object[] { MachineAction.LoadDisc(0x01234567, 1, new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x03, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineAction.LoadTape(0x01234567, new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x04, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineAction.RunUntil(0x01234567, 0x0123456789abcdef, null), new byte[] { 0x05, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 } , false },
            new object[] { MachineAction.LoadCore(0x01234567, new MemoryBlob(new byte[] { 0x01, 0x02 })), new byte[] { 0x06, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineAction.CoreVersion(0x01234567, 257), new byte[] { 0x07, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00 } , false },
            new object[] { MachineAction.CreateSnapshot(0x01234567, 42), new byte[] { 0x08, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { MachineAction.DeleteSnapshot(0x01234567, 42), new byte[] { 0x09, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { MachineAction.RevertToSnapshot(0x01234567, 42), new byte[] { 0x0a, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { new MachineAction((MachineRequest.Types) 99, 0x01234567), new byte[] { 99 }, true }
        };

        [TestCaseSource(nameof(CoreRequestCases))]
        public void CoreRequestToBytes(MachineRequest coreRequest, byte[] expectedBytes, bool throws)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act and Verify
            if (throws)
            {
                Assert.Throws<Exception>(() => Serializer.CoreRequestToBytes(stream, coreRequest));
            }
            else
            {
                Serializer.CoreRequestToBytes(stream, coreRequest);

                Assert.AreEqual(expectedBytes, stream.AsBytes());
            }
        }

        [TestCaseSource(nameof(CoreRequestCases))]
        public void CoreRequestFromBytes(MachineRequest expectedCoreRequest, byte[] expectedBytes, bool throws)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(expectedBytes);

            // Act and Verify
            if (throws)
            {
                Assert.Throws<Exception>(() => Serializer.CoreRequestFromBytes(stream));
            }
            else
            {
                MachineRequest coreRequest = Serializer.CoreRequestFromBytes(stream);

                Assert.True(CoreRequestsEqual(expectedCoreRequest, coreRequest));
            }
        }

        [TestCaseSource(nameof(CoreActionCases))]
        public void CoreActionToBytes(MachineAction coreAction, byte[] expectedBytes, bool throws)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream();

            // Act

            // Act and Verify
            if (throws)
            {
                Assert.Throws<Exception>(() => Serializer.CoreActionToBytes(stream, coreAction));
            }
            else
            {
                Serializer.CoreActionToBytes(stream, coreAction);
                Assert.AreEqual(expectedBytes, stream.AsBytes());
            }
        }

        [TestCaseSource(nameof(CoreActionCases))]
        public void CoreActionFromBytes(MachineAction expectedCoreAction, byte[] bytes, bool throws)
        {
            // Setup
            MemoryByteStream stream = new MemoryByteStream(bytes);

            // Act and Verify
            if (throws)
            {
                Assert.Throws<Exception>(() => Serializer.CoreActionFromBytes(stream));
            }
            else
            {
                MachineAction coreAction = Serializer.CoreActionFromBytes(stream);
                Assert.True(CoreActionsEqual(expectedCoreAction, coreAction));
            }
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
