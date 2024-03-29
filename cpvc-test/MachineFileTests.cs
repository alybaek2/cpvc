﻿using NUnit.Framework;
using System;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class MachineFileTests
    {
        static private readonly object[] CoreActionCases =
        {
            MachineAction.KeyPress(100, 42, true),
            MachineAction.CoreVersion(100, 2),
            MachineAction.LoadDisc(100, 1, MemoryBlob.Create(new byte[] { 0x01, 0x02 })),
            MachineAction.LoadTape(100, MemoryBlob.Create(new byte[] { 0x01, 0x02 })),
            MachineAction.Reset(100)
        };

        private MachineFileInfo _fileInfo;
        private MachineFile _file;
        private History _history;
        private MockTextFile _mockFile;
        private byte[] _state;
        private byte[] _screen;

        [SetUp]
        public void Setup()
        {
            _mockFile = new MockTextFile();
            _history = new History();
            _file = new MachineFile(_mockFile, _history);

            _state = new byte[1000];
            _screen = new byte[1000];

            for (int i = 0; i < 1000; i++)
            {
                _state[i] = (byte)(i % 256);
                _screen[i] = (byte)((i + 1) % 256);
            }
        }

        [TestCaseSource(nameof(CoreActionCases))]
        public void WriteAndReadCoreAction(IMachineAction coreAction)
        {
            // Act
            _history.AddCoreAction(coreAction, 123456);
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileInfo.History, _history));
        }

        [Test]
        public void CreateWriterWithoutFile()
        {
            // Setup
            History history = new History();

            // Act and Verify
            ArgumentException thrown = Assert.Throws<ArgumentNullException>(() =>
            {
                MachineFile fileWriter = new MachineFile(null, history);
            });
            Assert.AreEqual("textFile", thrown.ParamName);
        }

        [Test]
        public void WriteAndReadRunUntil()
        {
            // Act
            _history.AddCoreAction(MachineAction.RunUntil(100, 200, null));
            _history.CurrentEvent = _history.RootEvent;
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileInfo.History, _history));
        }

        [Test]
        public void WriteAndReadBookmark()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _history.AddBookmark(100, bookmark);
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileInfo.History, _history));
        }

        [Test]
        public void WriteAndReadDelete()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _history.AddBookmark(100, bookmark);
            HistoryEvent historyEvent = _history.CurrentEvent;
            _history.CurrentEvent = _history.RootEvent;
            _history.DeleteBookmark(historyEvent);
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileInfo.History, _history));
        }

        [Test]
        public void WriteAndReadDeleteBranch()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _history.AddBookmark(100, bookmark);
            HistoryEvent historyEvent = _history.CurrentEvent;
            _history.AddBookmark(200, bookmark);
            _history.CurrentEvent = _history.RootEvent;
            _history.DeleteBranch(historyEvent);
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileInfo.History, _history));
        }

        [Test]
        public void WriteAndReadArguments()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, _state, _screen);

            // Act
            _history.AddBookmark(100, bookmark);
            _history.AddBookmark(200, bookmark);
            _mockFile.Clear();
            _file.WriteHistory("Test");
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.AreEqual(1, _mockFile.Lines.Count(line => line.StartsWith("args:")));
            Assert.Zero(_mockFile.Lines.Count(line => line.StartsWith("arg:")));
            Assert.True(HistoriesEqual(_fileInfo.History, _history));
        }

        [Test]
        public void WriteAndReadName()
        {
            // Setup
            using (LocalMachine machine = LocalMachine.New(String.Empty, null))
            {
                _file.Machine = machine;

                // Act
                machine.Name = "Test";
                _fileInfo = MachineFile.Read(_mockFile);

                // Verify
                Assert.AreEqual(machine.Name, _fileInfo.Name);
            }
        }

        [Test]
        public void WriteSetCurrentRoot()
        {
            // Setup
            _history.AddCoreAction(MachineAction.KeyPress(100, 42, true));
            _history.CurrentEvent = _history.RootEvent;

            // Act
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.AreEqual(_fileInfo.History.RootEvent, _fileInfo.History.CurrentEvent);
        }

        [Test]
        public void WriteSetCurrentNonRoot()
        {
            // Setup
            _history.AddCoreAction(MachineAction.KeyPress(100, 42, true));
            HistoryEvent historyEvent = _history.AddCoreAction(MachineAction.KeyPress(100, 42, true));

            // Act
            _history.CurrentEvent = historyEvent;
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(_history, _fileInfo.History));
            Assert.AreEqual(_fileInfo.History.CurrentEvent, _fileInfo.History.RootEvent.Children[0].Children[0]);
        }

        [Test]
        public void MachineUnsubscribe()
        {
            // Setup
            using (LocalMachine machine1 = LocalMachine.New(null, null))
            using (LocalMachine machine2 = LocalMachine.New(null, null))
            {
                _file.Machine = machine1;
                _file.Machine = machine2;

                // Act
                machine1.Name = "Test1";
                int len1 = _mockFile.LineCount();
                machine2.Name = "Test2";
                int len2 = _mockFile.LineCount();
                _file.Machine = null;
                machine1.Name = "Test3";
                machine2.Name = "Test4";
                int len3 = _mockFile.LineCount();

                // Verify
                Assert.Zero(len1);
                Assert.NotZero(len2);
                Assert.AreEqual(len2, len3);
            }
        }

        [Test]
        public void Dispose()
        {
            // Setup
            LocalMachine machine1 = LocalMachine.New(null, null);
            machine1.Name = "Test";
            _history.AddCoreAction(MachineAction.Reset(100));
            int len1 = _mockFile.LineCount();

            // Act
            _file.Dispose();
            machine1.Name = "Test";
            _history.AddCoreAction(MachineAction.Reset(100));
            int len2 = _mockFile.LineCount();

            // Verify
            Assert.NotZero(len1);
            Assert.AreEqual(len1, len2);
        }

        [Test]
        public void DisposeWriterTwice()
        {
            // Setup
            _file.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => _file.Dispose());
        }

        [Test]
        public void SetCurrentInvalid()
        {
            // Setup
            _mockFile.WriteLine("current:42\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileInfo = MachineFile.Read(_mockFile));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadDeleteBookmarkInvalid()
        {
            // Setup
            _mockFile.WriteLine("deletebookmark:42\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileInfo = MachineFile.Read(_mockFile));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadDeleteBranchInvalid()
        {
            // Setup
            _mockFile.WriteLine("deletebranch:42\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileInfo = MachineFile.Read(_mockFile));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadUnknown()
        {
            // Setup
            _mockFile.WriteLine("unknown:\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileInfo = MachineFile.Read(_mockFile));
            Assert.AreEqual("type", ex.ParamName);
        }

        [Test]
        public void WriteUnknown()
        {
            // Setup
            IMachineAction coreAction = new UnknownAction();

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _history.AddCoreAction(coreAction));
            Assert.AreEqual("type", ex.ParamName);
        }

        [Test]
        public void MissingColon()
        {
            // Setup
            _mockFile.WriteLine("key");

            // Act and Verify
            Assert.Throws<Exception>(() => _fileInfo = MachineFile.Read(_mockFile));
        }

        [Test]
        public void DeleteInvalidBranch()
        {
            // Setup
            _mockFile.WriteLine("key:0,100,42,True");
            _mockFile.WriteLine("deletebranch:0");

            // Act and Verify
            Assert.Throws<InvalidOperationException>(() => _fileInfo = MachineFile.Read(_mockFile));
        }

        // There isn't anywhere in the code that writes out an uncompressed "arg" line, but
        // test it anyway to ensure it's propertly handled.
        [Test]
        public void ReadUncompressedArgument()
        {
            // Setup
            History expectedHistory = new History();
            expectedHistory.AddCoreAction(MachineAction.LoadTape(100, MemoryBlob.Create(new byte[] { 0x01, 0x02 })));
            _mockFile.WriteLine(String.Format("arg:1,False,0102"));
            _mockFile.WriteLine("tape:0,100,$1");

            // Act
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(expectedHistory, _fileInfo.History));
        }

        // There isn't anywhere in the code that writes out an uncompressed "args" line, but
        // test it anyway to ensure it's propertly handled.
        [Test]
        public void ReadUncompressedArguments()
        {
            // Setup
            History expectedHistory = new History();
            expectedHistory.AddBookmark(100, new Bookmark(true, 1, _state, _screen), new DateTime(123456789, DateTimeKind.Utc));
            _mockFile.WriteLine(String.Format("args:False,1#{0}@2#{1}", Helpers.StrFromBytes(_state), Helpers.StrFromBytes(_screen)));
            _mockFile.WriteLine("bookmark:0,100,True,1,123456789,$1,$2");

            // Act
            _fileInfo = MachineFile.Read(_mockFile);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(expectedHistory, _fileInfo.History));
        }

        [Test]
        public void ReadAddBookmarkInvalidId()
        {
            // Setup
            _mockFile.WriteLine("key:0,100,42,True");
            _mockFile.WriteLine("bookmark:0,100,True,1,123456789,0102,0304");

            // Act and Verify
            Assert.Throws<InvalidOperationException>(() => _fileInfo = MachineFile.Read(_mockFile));
        }

        [Test]
        public void ReadAddCoreActionInvalidId()
        {
            // Setup
            _mockFile.WriteLine("key:0,100,42,True");
            _mockFile.WriteLine("key:0,100,42,True");

            // Act and Verify
            Assert.Throws<InvalidOperationException>(() => _fileInfo = MachineFile.Read(_mockFile));
        }
    }
}
