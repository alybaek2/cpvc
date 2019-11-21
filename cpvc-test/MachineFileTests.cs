using NUnit.Framework;
using System;
using System.Collections.Generic;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MachineFileTests
    {
        private List<string> _lines;
        private IFile _mockWriter;
        private MachineFile _file;

        [SetUp]
        public void Setup()
        {
            _lines = new List<string>();
            _mockWriter = MockFileWriter(_lines);
            _file = new MachineFile(_mockWriter);
        }

        [Test]
        public void WriteName()
        {
            // Act
            _file.WriteName("test");

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "name:test");
        }

        [Test]
        public void WriteCurrentEvent()
        {
            // Setup
            HistoryEvent historyEvent = new HistoryEvent(25, HistoryEvent.Types.Checkpoint, 100);

            // Act
            _file.WriteCurrentEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "current:25");
        }

        [Test]
        public void WriteDelete()
        {
            // Setup
            HistoryEvent historyEvent = new HistoryEvent(25, HistoryEvent.Types.Checkpoint, 100);

            // Act
            _file.WriteDelete(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "delete:25");
        }

        [Test]
        public void WriteBookmark()
        {
            // Setup
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);
            Bookmark bookmark = new Bookmark(false, new byte[] { 0x01, 0x02 });

            // Act
            _file.WriteBookmark(historyEvent, bookmark);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "bookmark:25:2:0102");
        }

        [Test]
        public void WriteNullBookmark()
        {
            // Setup
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);

            // Act
            _file.WriteBookmark(historyEvent, null);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "bookmark:25:0");
        }

        [Test]
        public void WriteCheckpointWithoutBookmark()
        {
            // Setup
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "checkpoint:25:100:0:1234");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteCheckpointWithBookmark(bool system)
        {
            // Setup
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, new Bookmark(system, new byte[] { 0x01, 0x02 }));

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], String.Format("checkpoint:25:100:{0}:1234:0102", system ? "1" : "2"));
        }

        [Test]
        public void WriteCoreReset()
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.Reset(100));

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "reset:25:100");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteCoreKeyPress(bool down)
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.KeyPress(100, Keys.A, down));

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], String.Format("key:25:100:58:{0}", down ? "1" : "0"));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void WriteDisc(byte drive)
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.LoadDisc(100, drive, new byte[] { 0x01, 0x02 }));

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], String.Format("disc:25:100:{0}:0102", drive));
        }

        [Test]
        public void WriteTape()
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.LoadTape(100, new byte[] { 0x01, 0x02 }));

            // Act
            _file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(_lines.Count, 1);
            Assert.AreEqual(_lines[0], "tape:25:100:0102");
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
            string[] tokens = { "reset", "25", "100" };

            // Act
            HistoryEvent historyEvent = MachineFile.ParseResetLine(tokens);

            // Verify
            Assert.AreEqual(historyEvent.Id, 25);
            Assert.AreEqual(historyEvent.Ticks, 100);
            Assert.AreEqual(historyEvent.Type, HistoryEvent.Types.CoreAction);
            Assert.IsNotNull(historyEvent.CoreAction);
            Assert.AreEqual(historyEvent.CoreAction.Type, CoreActionBase.Types.Reset);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadKey(bool down)
        {
            // Setup
            string[] tokens = { "key", "25", "100", Keys.A.ToString(), down ? "1" : "0" };

            // Act
            HistoryEvent historyEvent = MachineFile.ParseKeyPressLine(tokens);

            // Verify
            Assert.AreEqual(historyEvent.Id, 25);
            Assert.AreEqual(historyEvent.Ticks, 100);
            Assert.AreEqual(historyEvent.Type, HistoryEvent.Types.CoreAction);
            Assert.IsNotNull(historyEvent.CoreAction);
            Assert.AreEqual(historyEvent.CoreAction.Type, CoreActionBase.Types.KeyPress);
            Assert.AreEqual(historyEvent.CoreAction.KeyCode, Keys.A);
            Assert.AreEqual(historyEvent.CoreAction.KeyDown, down);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ReadDisc(byte drive)
        {
            // Setup
            string[] tokens = { "disc", "25", "100", drive.ToString(), "0102" };

            // Act
            HistoryEvent historyEvent = MachineFile.ParseDiscLine(tokens);

            // Verify
            Assert.AreEqual(historyEvent.Id, 25);
            Assert.AreEqual(historyEvent.Ticks, 100);
            Assert.AreEqual(historyEvent.Type, HistoryEvent.Types.CoreAction);
            Assert.IsNotNull(historyEvent.CoreAction);
            Assert.AreEqual(historyEvent.CoreAction.Type, CoreActionBase.Types.LoadDisc);
            Assert.AreEqual(historyEvent.CoreAction.Drive, drive);
            Assert.AreEqual(historyEvent.CoreAction.MediaBuffer, new byte[] { 0x01, 0x02 });
        }

        [Test]
        public void ReadTape()
        {
            // Setup
            string[] tokens = { "disc", "25", "100", "0102" };

            // Act
            HistoryEvent historyEvent = MachineFile.ParseTapeLine(tokens);

            // Verify
            Assert.AreEqual(historyEvent.Id, 25);
            Assert.AreEqual(historyEvent.Ticks, 100);
            Assert.AreEqual(historyEvent.Type, HistoryEvent.Types.CoreAction);
            Assert.IsNotNull(historyEvent.CoreAction);
            Assert.AreEqual(historyEvent.CoreAction.Type, CoreActionBase.Types.LoadTape);
            Assert.AreEqual(historyEvent.CoreAction.MediaBuffer, new byte[] { 0x01, 0x02 });
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadCheckpointWithBookmark(bool system)
        {
            // Setup
            string[] tokens = { "checkpoint", "25", "100", system ? "1" : "2", "1234", "0102" };

            // Act
            HistoryEvent historyEvent = MachineFile.ParseCheckpointLine(tokens);

            // Verify
            Assert.IsNotNull(historyEvent);
            Assert.AreEqual(historyEvent.Id, 25);
            Assert.AreEqual(historyEvent.Ticks, 100);
            Assert.IsNotNull(historyEvent.Bookmark);
            Assert.AreEqual(historyEvent.Bookmark.System, system);
            Assert.AreEqual(historyEvent.Bookmark.State, new byte[] { 0x01, 0x02 });
        }

        [Test]
        public void ReadCheckpointWithInvalidBookmarkType()
        {
            // Setup
            string[] tokens = { "checkpoint", "25", "100", "999", "1234", "0102" };

            // Act and Verify
            Assert.Throws<Exception>(() => MachineFile.ParseCheckpointLine(tokens));
        }

        public void ReadCheckpointWithoutBookmark()
        {
            // Setup
            string[] tokens = { "checkpoint", "25", "100", "0" };

            // Act
            HistoryEvent historyEvent = MachineFile.ParseCheckpointLine(tokens);

            // Verify
            Assert.IsNotNull(historyEvent);
            Assert.AreEqual(historyEvent.Id, 25);
            Assert.AreEqual(historyEvent.Ticks, 100);
            Assert.IsNull(historyEvent.Bookmark);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadBookmark(bool system)
        {
            // Setup
            string[] tokens = { "bookmark", "25", system ? "1" : "2", "0102" };

            // Act
            Bookmark bookmark = MachineFile.ParseBookmarkLine(tokens);

            // Verify
            Assert.IsNotNull(bookmark);
            Assert.AreEqual(bookmark.System, system);
            Assert.AreEqual(bookmark.State, new byte[] { 0x01, 0x02 });
        }

        [Test]
        public void ReadNullBookmark()
        {
            // Setup
            string[] tokens = { "bookmark", "25", "0" };

            // Act
            Bookmark bookmark = MachineFile.ParseBookmarkLine(tokens);

            // Verify
            Assert.IsNull(bookmark);
        }

        [TestCase("")]
        [TestCase("-1")]
        [TestCase("3")]
        [TestCase("abcdef")]
        public void ReadInvalidBookmarkType(string bookmarkType)
        {
            // Setup
            string[] tokens = { "bookmark", "25", bookmarkType };

            // Act and Verify
            Assert.Throws<Exception>(() => MachineFile.ParseBookmarkLine(tokens));
        }
    }
}
