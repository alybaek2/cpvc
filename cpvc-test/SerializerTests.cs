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

        static private object[] MachineRequestCases =
        {
            new object[] { new KeyPressRequest(Keys.A, true), new byte[] { 0x01, 58, 0xFF } , false },
            new object[] { new ResetRequest(), new byte[] { 0x02 } , false },
            new object[] { new LoadDiscRequest(1, MemoryBlob.Create(new byte[] { 0x01, 0x02 })), new byte[] { 0x03, 0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { new LoadTapeRequest(MemoryBlob.Create(new byte[] { 0x01, 0x02 })), new byte[] { 0x04, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { new RunUntilRequest(0x0123456789abcdef ), new byte[] { 0x05, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 } , false },
            new object[] { new LoadCoreRequest(MemoryBlob.Create(new byte[] { 0x01, 0x02 }), null), new byte[] { 0x06, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { new CoreVersionRequest(257), new byte[] { 0x07, 0x01, 0x01, 0x00, 0x00 } , false },
            new object[] { new CreateSnapshotRequest(42), new byte[] { 0x08, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { new DeleteSnapshotRequest(42), new byte[] { 0x09, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { new RevertToSnapshotRequest(42), new byte[] { 0x0a, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { new TestHelpers.UnknownRequest(), new byte[] { }, true }
        };

        static private object[] CoreActionCases =
        {
            new object[] { MachineAction.KeyPress(0x01234567, Keys.A, true), new byte[] { 0x01, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 58, 0xFF } , false },
            new object[] { MachineAction.Reset(0x01234567), new byte[] { 0x02, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00 } , false },
            new object[] { MachineAction.LoadDisc(0x01234567, 1, MemoryBlob.Create(new byte[] { 0x01, 0x02 })), new byte[] { 0x03, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineAction.LoadTape(0x01234567, MemoryBlob.Create(new byte[] { 0x01, 0x02 })), new byte[] { 0x04, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineAction.RunUntil(0x01234567, 0x0123456789abcdef, null), new byte[] { 0x05, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0xef, 0xcd, 0xab, 0x89, 0x67, 0x45, 0x23, 0x01 } , false },
            new object[] { MachineAction.LoadCore(0x01234567, MemoryBlob.Create(new byte[] { 0x01, 0x02 })), new byte[] { 0x06, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02 } , false },
            new object[] { MachineAction.CoreVersion(0x01234567, 257), new byte[] { 0x07, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x00, 0x00 } , false },
            new object[] { MachineAction.CreateSnapshot(0x01234567, 42), new byte[] { 0x08, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { MachineAction.DeleteSnapshot(0x01234567, 42), new byte[] { 0x09, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { MachineAction.RevertToSnapshot(0x01234567, 42), new byte[] { 0x0a, 0x67, 0x45, 0x23, 0x01, 0x00, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00 }, false },
            new object[] { new TestHelpers.UnknownAction(), new byte[] { }, true }
        };

        [TestCaseSource(nameof(MachineRequestCases))]
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

        [TestCaseSource(nameof(MachineRequestCases))]
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
        public void CoreActionToBytes(IMachineAction coreAction, byte[] expectedBytes, bool throws)
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
        public void CoreActionFromBytes(IMachineAction expectedCoreAction, byte[] bytes, bool throws)
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
                IMachineAction coreAction = Serializer.CoreActionFromBytes(stream);
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
