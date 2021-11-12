using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    internal class MemoryFileByteStream : MemoryByteStream, IFileByteStream
    {
        public MemoryFileByteStream() : base()
        {
        }

        public MemoryFileByteStream(byte[] bytes) : base(bytes)
        {
        }

        public void Close()
        {
        }

        public void Dispose()
        {
        }

        public void SeekToEnd()
        {
            Position = Length;
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
        public void WriteAndReadRunUntil()
        {
            // Act
            _writeHistory.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            _writeHistory.SetCurrent(_writeHistory.RootEvent);
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
            LocalMachine machine = new LocalMachine(String.Empty);
            _file.Machine = machine;

            // Act
            machine.Name = "Test";
            _file.ReadFile(out string name, out _);

            // Verify
            Assert.AreEqual(machine.Name, name);
        }

        [Test]
        public void WriteSetCurrentRoot()
        {
            // Setup
            _writeHistory.AddCoreAction(CoreAction.KeyPress(100, 42, true));

            // Act
            _writeHistory.SetCurrent(_writeHistory.RootEvent);
            _file.ReadFile(out _, out MachineHistory readHistory);

            // Verify
            Assert.AreEqual(readHistory.RootEvent, readHistory.CurrentEvent);
        }

        [Test]
        public void WriteSetCurrentNonRoot()
        {
            // Setup
            _writeHistory.AddCoreAction(CoreAction.KeyPress(100, 42, true));
            HistoryEvent historyEvent = _writeHistory.AddCoreAction(CoreAction.KeyPress(100, 42, true));

            // Act
            _writeHistory.SetCurrent(historyEvent);
            _file.ReadFile(out _, out MachineHistory readHistory);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(_writeHistory, readHistory));
            Assert.AreEqual(readHistory.CurrentEvent, readHistory.RootEvent.Children[0].Children[0]);
        }

        [Test]
        public void MachineUnsubscribe()
        {
            // Setup
            LocalMachine machine1 = LocalMachine.New(null, null);
            LocalMachine machine2 = LocalMachine.New(null, null);
            _file.Machine = machine1;
            _file.Machine = machine2;

            // Act
            machine1.Name = "Test1";
            long len1 = _memStream.Length;
            machine2.Name = "Test2";
            long len2 = _memStream.Length;
            _file.Machine = null;
            machine1.Name = "Test3";
            machine2.Name = "Test4";
            long len3 = _memStream.Length;

            // Verify
            Assert.Zero(len1);
            Assert.NotZero(len2);
            Assert.AreEqual(len2, len3);
        }

        [Test]
        public void MachineHistoryUnsubscribe()
        {
            // Setup
            MachineHistory history1 = new MachineHistory();
            MachineHistory history2 = new MachineHistory();
            _file.History = history1;
            _file.History = history2;

            // Act
            history1.AddCoreAction(CoreAction.Reset(100));
            long len1 = _memStream.Length;
            history2.AddCoreAction(CoreAction.Reset(100));
            long len2 = _memStream.Length;
            _file.History = null;
            history1.AddCoreAction(CoreAction.Reset(100));
            history2.AddCoreAction(CoreAction.Reset(100));
            long len3 = _memStream.Length;

            // Verify
            Assert.Zero(len1);
            Assert.NotZero(len2);
            Assert.AreEqual(len2, len3);
        }

        [Test]
        public void Close()
        {
            // Setup
            LocalMachine machine1 = LocalMachine.New(null, null);
            MachineHistory history1 = new MachineHistory();
            _file.History = history1;
            machine1.Name = "Test";
            history1.AddCoreAction(CoreAction.Reset(100));
            long len1 = _memStream.Length;

            // Act
            _file.Close();
            machine1.Name = "Test";
            history1.AddCoreAction(CoreAction.Reset(100));
            long len2 = _memStream.Length;

            // Verify
            Assert.NotZero(len1);
            Assert.AreEqual(len1, len2);
        }

        [Test]
        public void SetCurrentInvalid()
        {
            // Setup
            string line = "current:42\r\n";
            MemoryFileByteStream fileByteStream = new MemoryFileByteStream(Encoding.UTF8.GetBytes(line));
            MachineFile file = new MachineFile(fileByteStream);

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => file.ReadFile(out _, out MachineHistory history));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadSetCurrentInvalid()
        {
            // Setup
            string line = "current:42\r\n";
            MemoryFileByteStream fileByteStream = new MemoryFileByteStream(Encoding.UTF8.GetBytes(line));
            MachineFile file = new MachineFile(fileByteStream);

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => file.ReadFile(out _, out MachineHistory history));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadDeleteInvalid()
        {
            // Setup
            string line = "delete:42\r\n";
            MemoryFileByteStream fileByteStream = new MemoryFileByteStream(Encoding.UTF8.GetBytes(line));
            MachineFile file = new MachineFile(fileByteStream);

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => file.ReadFile(out _, out MachineHistory history));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadDeleteWithChildrenInvalid()
        {
            // Setup
            string line = "deletewithchildren:42\r\n";
            MemoryFileByteStream fileByteStream = new MemoryFileByteStream(Encoding.UTF8.GetBytes(line));
            MachineFile file = new MachineFile(fileByteStream);

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => file.ReadFile(out _, out MachineHistory history));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadUnknown()
        {
            // Setup
            string line = "unknown:\r\n";
            MemoryFileByteStream fileByteStream = new MemoryFileByteStream(Encoding.UTF8.GetBytes(line));
            MachineFile file = new MachineFile(fileByteStream);

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => file.ReadFile(out _, out MachineHistory history));
            Assert.AreEqual("type", ex.ParamName);
        }

        [Test]
        public void WriteUnknown()
        {
            // Setup
            CoreAction coreAction = new CoreAction((CoreRequest.Types)42, 0);

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _writeHistory.AddCoreAction(coreAction));
            Assert.AreEqual("type", ex.ParamName);
        }
    }
}
