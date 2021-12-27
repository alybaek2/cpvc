using NUnit.Framework;
using System;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class MachineFileTests
    {
        static private readonly object[] CoreActionCases =
        {
            CoreAction.KeyPress(100, 42, true),
            CoreAction.CoreVersion(100, 2),
            CoreAction.LoadDisc(100, 1, new MemoryBlob(new byte[] { 0x01, 0x02 })),
            CoreAction.LoadTape(100, new MemoryBlob(new byte[] { 0x01, 0x02 })),
            CoreAction.Reset(100)
        };

        private MachineFileReader _fileReader;
        private MachineFileWriter _fileWriter;
        private History _history;
        private MockTextFile _mockFile;
        private byte[] _state;
        private byte[] _screen;

        [SetUp]
        public void Setup()
        {
            _mockFile = new MockTextFile();
            _fileReader = new MachineFileReader();
            _history = new History();
            _fileWriter = new MachineFileWriter(_mockFile, _history);

            _state = new byte[1000];
            _screen = new byte[1000];

            for (int i = 0; i < 1000; i++)
            {
                _state[i] = (byte)(i % 256);
                _screen[i] = (byte)((i + 1) % 256);
            }
        }

        [TestCaseSource(nameof(CoreActionCases))]
        public void WriteAndReadCoreAction(CoreAction coreAction)
        {
            // Act
            _history.AddCoreAction(coreAction);
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileReader.History, _history));
        }

        [Test]
        public void CreateWriterWithoutFile()
        {
            // Setup
            History history = new History();

            // Act and Verify
            ArgumentException thrown = Assert.Throws<ArgumentException>(() =>
            {
                MachineFileWriter fileWriter = new MachineFileWriter(null, history);
            });
            Assert.AreEqual("textFile", thrown.ParamName);
        }

        [Test]
        public void WriteAndReadRunUntil()
        {
            // Act
            _history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            _history.CurrentEvent = _history.RootEvent;
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileReader.History, _history));
        }

        [Test]
        public void WriteAndReadBookmark()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            _history.AddBookmark(100, bookmark);
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileReader.History, _history));
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
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileReader.History, _history));
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
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(HistoriesEqual(_fileReader.History, _history));
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
            _fileWriter.WriteHistory("Test");
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.AreEqual(1, _mockFile.Lines.Count(line => line.StartsWith("args:")));
            Assert.Zero(_mockFile.Lines.Count(line => line.StartsWith("arg:")));
            Assert.True(HistoriesEqual(_fileReader.History, _history));
        }

        [Test]
        public void WriteAndReadName()
        {
            // Setup
            LocalMachine machine = LocalMachine.New(String.Empty, null);
            _fileWriter.Machine = machine;

            // Act
            machine.Name = "Test";
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.AreEqual(machine.Name, _fileReader.Name);
        }

        [Test]
        public void WriteSetCurrentRoot()
        {
            // Setup
            _history.AddCoreAction(CoreAction.KeyPress(100, 42, true));
            _history.CurrentEvent = _history.RootEvent;

            // Act
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.AreEqual(_fileReader.History.RootEvent, _fileReader.History.CurrentEvent);
        }

        [Test]
        public void WriteSetCurrentNonRoot()
        {
            // Setup
            _history.AddCoreAction(CoreAction.KeyPress(100, 42, true));
            HistoryEvent historyEvent = _history.AddCoreAction(CoreAction.KeyPress(100, 42, true));

            // Act
            _history.CurrentEvent = historyEvent;
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(_history, _fileReader.History));
            Assert.AreEqual(_fileReader.History.CurrentEvent, _fileReader.History.RootEvent.Children[0].Children[0]);
        }

        [Test]
        public void MachineUnsubscribe()
        {
            // Setup
            LocalMachine machine1 = LocalMachine.New(null, null);
            LocalMachine machine2 = LocalMachine.New(null, null);
            _fileWriter.Machine = machine1;
            _fileWriter.Machine = machine2;

            // Act
            machine1.Name = "Test1";
            int len1 = _mockFile.LineCount();
            machine2.Name = "Test2";
            int len2 = _mockFile.LineCount();
            _fileWriter.Machine = null;
            machine1.Name = "Test3";
            machine2.Name = "Test4";
            int len3 = _mockFile.LineCount();

            // Verify
            Assert.Zero(len1);
            Assert.NotZero(len2);
            Assert.AreEqual(len2, len3);
        }

        [Test]
        public void Dispose()
        {
            // Setup
            LocalMachine machine1 = LocalMachine.New(null, null);
            machine1.Name = "Test";
            _history.AddCoreAction(CoreAction.Reset(100));
            int len1 = _mockFile.LineCount();

            // Act
            _fileWriter.Dispose();
            machine1.Name = "Test";
            _history.AddCoreAction(CoreAction.Reset(100));
            int len2 = _mockFile.LineCount();

            // Verify
            Assert.NotZero(len1);
            Assert.AreEqual(len1, len2);
        }

        [Test]
        public void DisposeWriterTwice()
        {
            // Setup
            _fileWriter.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => _fileWriter.Dispose());
        }

        [Test]
        public void SetCurrentInvalid()
        {
            // Setup
            _mockFile.WriteLine("current:42\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileReader.ReadFile(_mockFile));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadDeleteBookmarkInvalid()
        {
            // Setup
            _mockFile.WriteLine("deletebookmark:42\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileReader.ReadFile(_mockFile));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadDeleteBranchInvalid()
        {
            // Setup
            _mockFile.WriteLine("deletebranch:42\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileReader.ReadFile(_mockFile));
            Assert.AreEqual("id", ex.ParamName);
        }

        [Test]
        public void ReadUnknown()
        {
            // Setup
            _mockFile.WriteLine("unknown:\r\n");

            // Act and Verify
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _fileReader.ReadFile(_mockFile));
            Assert.AreEqual("type", ex.ParamName);
        }

        [Test]
        public void WriteUnknown()
        {
            // Setup
            CoreAction coreAction = new CoreAction((CoreRequest.Types)42, 0);

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
            Assert.Throws<Exception>(() => _fileReader.ReadFile(_mockFile));
        }

        [Test]
        public void DeleteInvalidBranch()
        {
            // Setup
            _mockFile.WriteLine("key:0,100,42,True");
            _mockFile.WriteLine("deletebranch:0");

            // Act and Verify
            Assert.Throws<InvalidOperationException>(() => _fileReader.ReadFile(_mockFile));
        }

        // There isn't anywhere in the code that writes out an uncompressed "arg" line, but
        // test it anyway to ensure it's propertly handled.
        [Test]
        public void ReadUncompressedArgument()
        {
            // Setup
            History expectedHistory = new History();
            expectedHistory.AddCoreAction(CoreAction.LoadTape(100, new MemoryBlob(new byte[] { 0x01, 0x02 })));
            _mockFile.WriteLine(String.Format("arg:1,False,0102"));
            _mockFile.WriteLine("tape:1,100,$1");

            // Act
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(expectedHistory, _fileReader.History));
        }

        // There isn't anywhere in the code that writes out an uncompressed "args" line, but
        // test it anyway to ensure it's propertly handled.
        [Test]
        public void ReadUncompressedArguments()
        {
            // Setup
            History expectedHistory = new History();
            expectedHistory.AddBookmark(100, new Bookmark(true, 1, _state, _screen));
            _mockFile.WriteLine(String.Format("args:False,1#{0}@2#{1}", Helpers.StrFromBytes(_state), Helpers.StrFromBytes(_screen)));
            _mockFile.WriteLine("bookmark:4,100,True,1,$1,$2");

            // Act
            _fileReader.ReadFile(_mockFile);

            // Verify
            Assert.True(TestHelpers.HistoriesEqual(expectedHistory, _fileReader.History));
        }
    }
}
