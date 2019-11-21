using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class MachineFileTests
    {
        [Test]
        public void WriteName()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);

            // Act
            file.WriteName("test");

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "name:test");
        }

        [Test]
        public void WriteCurrentEvent()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            HistoryEvent historyEvent = new HistoryEvent(25, HistoryEvent.Types.Checkpoint, 100);

            // Act
            file.WriteCurrentEvent(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "current:25");
        }

        [Test]
        public void WriteDelete()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            HistoryEvent historyEvent = new HistoryEvent(25, HistoryEvent.Types.Checkpoint, 100);

            // Act
            file.WriteDelete(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "delete:25");
        }

        [Test]
        public void WriteBookmark()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);
            Bookmark bookmark = new Bookmark(false, new byte[] { 0x01, 0x02 });

            // Act
            file.WriteBookmark(historyEvent, bookmark);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "bookmark:25:2:0102");
        }

        [Test]
        public void WriteNullBookmark()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);

            // Act
            file.WriteBookmark(historyEvent, null);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "bookmark:25:0");
        }

        [Test]
        public void WriteCheckpointWithoutBookmark()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, null);

            // Act
            file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "checkpoint:25:100:0:1234");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteCheckpointWithBookmark(bool system)
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            DateTime timestamp = Helpers.NumberToDateTime("1234");
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, new Bookmark(system, new byte[] { 0x01, 0x02 }));

            // Act
            file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], String.Format("checkpoint:25:100:{0}:1234:0102", system ? "1" : "2"));
        }

        [Test]
        public void WriteCoreReset()
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.Reset(100));

            // Act
            file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "reset:25:100");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteCoreKeyPress(bool down)
        {
            // Setup
            List<string> lines = new List<string>();
            IFile mockWriter = MockFileWriter(lines);
            MachineFile file = new MachineFile(mockWriter);
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(25, CoreAction.KeyPress(100, Keys.A, down));

            // Act
            file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], String.Format("key:25:100:58:{0}", down ? "1" : "0"));
        }
    }
}
