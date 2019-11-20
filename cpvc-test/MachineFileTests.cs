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
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(25, 100, timestamp, new Bookmark(100, false, new byte[] { 0x01, 0x02 }));

            // Act
            file.WriteHistoryEvent(historyEvent);

            // Verify
            Assert.AreEqual(lines.Count, 1);
            Assert.AreEqual(lines[0], "checkpoint:25:100:2:1234:0102");
        }
    }
}
