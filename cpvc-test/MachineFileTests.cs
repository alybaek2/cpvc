using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MachineFileTests
    {
        private List<string> _lines;
        private MockFileByteStream _mockBinaryWriter;
        private MachineFile _file;

        [SetUp]
        public void Setup()
        {
            _lines = new List<string>();
            _mockBinaryWriter = new MockFileByteStream();
            _file = new MachineFile(_mockBinaryWriter.Object);
        }

        [TearDown]
        public void Teardown()
        {
            _lines = null;
            _mockBinaryWriter = null;
            _file = null;
        }

        [Test]
        public void WriteName()
        {
            // Setup
            byte[] expected = new byte[]
            {
                0x00,
                      0x04, 0x00, 0x00, 0x00,
                      (byte)'t', (byte)'e', (byte)'s', (byte)'t'
            };

            // Act
            _file.WriteName("test");

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteCurrentEvent()
        {
            // Setup
            HistoryEvent historyEvent = new HistoryEvent(25, HistoryEvent.Types.Checkpoint, 0);
            byte[] expected = new byte[]
            {
                0x07,
                      0x19, 0x00, 0x00, 0x00
            };

            // Act
            _file.WriteCurrent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteDelete()
        {
            // Setup
            HistoryEvent historyEvent = new HistoryEvent(25, HistoryEvent.Types.Checkpoint, 100);
            byte[] expected = new byte[]
            {
                0x06,
                      0x19, 0x00, 0x00, 0x00
            };
            
            // Act
            _file.WriteDelete(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteBookmark()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, new byte[] { 0x01, 0x02 }, null);
            byte[] expected = new byte[]
            {
                0x08,
                      0x19, 0x00, 0x00, 0x00,
                      0x01,
                      0x00,
                      0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02,
                      0x00
            };

            // Act
            _file.WriteBookmark(0x19, bookmark);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteNullBookmark()
        {
            // Setup
            byte[] expected = new byte[]
            {
                0x08,
                      0x19, 0x00, 0x00, 0x00,
                      0x00
            };

            // Act
            _file.WriteBookmark(0x19, null);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteCheckpointWithoutBookmark()
        {
            // Setup
            DateTime timestamp = Helpers.NumberToDateTime(0);
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);
            byte[] expected = new byte[]
            {
                0x05,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00
            };

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteCheckpointWithBookmark(bool system)
        {
            // Setup
            DateTime timestamp = Helpers.NumberToDateTime(0);
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, new Bookmark(system, new byte[] { 0x01, 0x02 }, null));
            byte[] expected = new byte[]
            {
                0x05,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x01,
                      (byte)(system ? 0x01 : 0x00),
                      0x03, 0x04, 0x00, 0x00, 0x00, 0x63, 0x64, 0x02, 0x00,
                      0x00
            };

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteCoreReset()
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.Reset(100));
            byte[] expected = new byte[]
            {
                0x02,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteCoreKeyPress(bool down)
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.KeyPress(100, Keys.A, down));
            byte[] expected = new byte[]
            {
                0x01,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      (byte) ((down ? 0x80 : 0x00) + 58)
            };

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void WriteDisc(byte drive)
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.LoadDisc(100, drive, new byte[] { 0x01, 0x02 }));
            byte[] expected = new byte[]
            {
                0x03,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      drive,
                      0x03, 0x04, 0x00, 0x00, 0x00, 0x63, 0x64, 0x02, 0x00
            };

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteTape()
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.LoadTape(100, new byte[] { 0x01, 0x02 }));
            byte[] expected = new byte[]
            {
                0x04,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x03, 0x04, 0x00, 0x00, 0x00, 0x63, 0x64, 0x02, 0x00
            };

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.IsTrue(expected.SequenceEqual(_mockBinaryWriter.Content));
        }

        [Test]
        public void WriteInvalidHistoryEventType()
        {
            // Setup
            HistoryEvent historyEvent = new HistoryEvent(25, (HistoryEvent.Types)99, 100);

            // Act and Verify
            Assert.Throws<Exception>(() => _file.WriteHistoryEvent(historyEvent));
        }

        [Test]
        public void WriteInvalidCoreActionType()
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, new CoreAction((CoreActionBase.Types)99, 100));

            // Act and Verify
            Assert.Throws<Exception>(() => _file.WriteHistoryEvent(historyEvent));
        }

        [Test]
        public void ReadReset()
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte> {
                0x02,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.AddHistoryEvent(CoreActionEvent(0x19, 100, CoreActionBase.Types.Reset)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadKey(bool down)
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte> {
                0x01,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      (byte) ((down ? 0x80 : 0x00) + Keys.A)
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.AddHistoryEvent(KeyPressEvent(0x19, 100, Keys.A, down)));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ReadDisc(byte drive)
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte> {
                0x03,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      drive,
                      0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.AddHistoryEvent(LoadDiscEvent(0x19, 100, drive, new byte[] { 0x01, 0x02 })));
        }

        [Test]
        public void ReadTape()
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte> {
                0x04,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.AddHistoryEvent(LoadTapeEvent(0x19, 100, new byte[] { 0x01, 0x02 })));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadCheckpointWithBookmark(bool system)
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte> {
                0x05,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x01,
                      (byte)(system ? 0x01 : 0x00),
                      0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02,
                      0x00
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.AddHistoryEvent(CheckpointWithBookmarkEvent(0x19, 100, system, 23)));
        }

        [Test]
        public void ReadCheckpointWithoutBookmark()
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte> {
                0x05,
                      0x19, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.AddHistoryEvent(CheckpointWithoutBookmarkEvent(0x19, 100)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadBookmark(bool system)
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);
            MockFileByteStream binaryFile = new MockFileByteStream(new List<byte>
            {
                0x08,
                      0x19, 0x00, 0x00, 0x00,
                      0x01,
                      (byte) (system ? 0x01 : 0x00),
                      0x01, 0x02, 0x00, 0x00, 0x00, 0x01, 0x02,
                      0x00
            });

            MachineFile file = new MachineFile(binaryFile.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.SetBookmark(0x19, BookmarkMatch(system, 7, 14)));
        }

        [Test]
        public void ReadNullBookmark()
        {
            // Setup
            Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);

            MockFileByteStream mockWriter = new MockFileByteStream(new List<byte>
            {
                0x08,
                      0x19, 0x00, 0x00, 0x00,
                      0x00
            });

            MachineFile file = new MachineFile(mockWriter.Object);

            // Act
            file.ReadFile(mockFileReader.Object);

            // Verify
            mockFileReader.Verify(reader => reader.SetBookmark(0x19, null));
            mockFileReader.VerifyNoOtherCalls();
        }
    }
}
