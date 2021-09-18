using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    internal class MemoryFileByteStream : MemoryByteStream, IFileByteStream
    {
        public void Close()
        {
        }

        public void Dispose()
        {
        }
    }

    public class MachineFileTests
    {
        static readonly object[] CoreActionCases =
        {
            CoreAction.KeyPress(100, 42, true),
            CoreAction.CoreVersion(100, 2),
            CoreAction.LoadDisc(100, 1, new MemoryBlob(new byte[] { 0x01, 0x02 })),
            CoreAction.LoadTape(100, new MemoryBlob(new byte[] { 0x01, 0x02 })),
            CoreAction.Reset(100)
        };

        private MemoryFileByteStream _memStream;
        private MachineFile _file;
        private MachineHistory _writeHistory;

        [SetUp]
        public void Setup()
        {
            _memStream = new MemoryFileByteStream();
            _file = new MachineFile(_memStream);
            _writeHistory = new MachineHistory();
            _file.History = _writeHistory;
        }

        [TestCaseSource(nameof(CoreActionCases))]
        public void WriteAndReadCoreAction(CoreAction coreAction)
        {
            // Act
            _writeHistory.AddCoreAction(coreAction);
            _file.ReadFile(out _, out MachineHistory readHistory);

            // Verify
            Assert.True(HistoriesEqual(readHistory, _writeHistory));
        }

        [Test]
        public void WriteAndReadBookmark()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _writeHistory.AddBookmark(100, bookmark);
            _file.ReadFile(out _, out MachineHistory readHistory);

            // Verify
            Assert.True(HistoriesEqual(readHistory, _writeHistory));
        }

        [Test]
        public void WriteAndReadDelete()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _writeHistory.AddBookmark(100, bookmark);
            HistoryEvent historyEvent = _writeHistory.CurrentEvent;
            _writeHistory.SetCurrent(_writeHistory.RootEvent);
            _writeHistory.DeleteEvent(historyEvent);
            _file.ReadFile(out _, out MachineHistory readHistory);

            // Verify
            Assert.True(HistoriesEqual(readHistory, _writeHistory));
        }

        [Test]
        public void WriteAndReadDeleteAndChildren()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _writeHistory.AddBookmark(100, bookmark);
            HistoryEvent historyEvent = _writeHistory.CurrentEvent;
            _writeHistory.AddBookmark(200, bookmark);
            _writeHistory.SetCurrent(_writeHistory.RootEvent);
            _writeHistory.DeleteEventAndChildren(historyEvent);
            _file.ReadFile(out _, out MachineHistory readHistory);

            // Verify
            Assert.True(HistoriesEqual(readHistory, _writeHistory));
        }

        [Test]
        public void WriteAndReadName()
        {
            // Setup
            Machine machine = new Machine(String.Empty);
            _file.Machine = machine;

            // Act
            machine.Name = "Test";
            _file.ReadFile(out string name, out _);

            // Verify
            Assert.AreEqual(machine.Name, name);
        }
    }
}
