using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MainViewModelTests
    {
        private Mock<ISettings> _mockSettings;
        private Mock<IFile> _mockFile;
        private Mock<IFileSystem> _mockFileSystem;

        private string[] _lines;
        private string _settingGet;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISettings>(MockBehavior.Strict);
            _mockSettings.SetupGet(x => x.RecentlyOpened).Returns(() => _settingGet);
            _mockSettings.SetupSet(x => x.RecentlyOpened = It.IsAny<string>());

            _mockFile = new Mock<IFile>();
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines(AnyString())).Returns(() => _lines);
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(_mockFile.Object);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);
        }

        [TearDown]
        public void Teardown()
        {
            _mockSettings = null;
            _mockFile = null;
            _mockFileSystem = null;

            _lines = null;
            _settingGet = null;
        }

        private MainViewModel SetupViewModel(int machineCount)
        {
            _settingGet = String.Join(",", Enumerable.Range(0, machineCount).Select(x => String.Format("Test{0};test{0}.cpvc", x)));
            _lines = new string[] { "name:test", "checkpoint:0:0:0:0" };

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            return viewModel;
        }

        static private Expression<Func<IFileSystem, byte[]>> GetZipFileEntry(string filepath)
        {
            return fileSystem => fileSystem.GetZipFileEntry(filepath, AnyString());
        }

        static private Expression<Func<IFileSystem, byte[]>> GetZipFileEntry(string filepath, string entryName)
        {
            return fileSystem => fileSystem.GetZipFileEntry(filepath, entryName);
        }

        static private Expression<Func<IFileSystem, List<string>>> GetZipFileEntryNames(string filepath)
        {
            return fileSystem => fileSystem.GetZipFileEntryNames(filepath);
        }

        static private Mock<MainViewModel.PromptForFileDelegate> SetupPrompt(FileTypes fileType, bool existing, string filepath)
        {
            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = new Mock<MainViewModel.PromptForFileDelegate>();
            mockPrompt.Setup(x => x(fileType, existing)).Returns(filepath);

            return mockPrompt;
        }

        [Test]
        public void ThrowsWhenNewMachineFails()
        {
            // Setup
            string filepath = "test.cpvc";
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(filepath)).Throws(new Exception("File not found"));
            mockFileSystem.Setup(ReadBytes()).Throws(new Exception("File missing"));
            mockFileSystem.Setup(DeleteFile(filepath));
            mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(false);

            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act and Verify
            Exception ex = Assert.Throws<Exception>(() =>
            {
                MainViewModel viewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);
                viewModel.NewMachine(prompt.Object, mockFileSystem.Object);
            });
            Assert.AreEqual(ex.Message, "File not found");
        }

        [Test]
        public void NewNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0);
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, null);

            // Act
            Machine machine = viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(machine);
        }

        [Test]
        public void OpenNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0);
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, null);

            // Act
            Machine machine = viewModel.OpenMachine(prompt.Object, null, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(machine);
        }

        [TestCase(null, null, null)]
        [TestCase(null, "test.cpvc", "test")]
        [TestCase("test.cpvc", null, "test")]
        public void OpenMachine(string filepath, string promptedFilepath, string expectedMachineName)
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, promptedFilepath);
            MainViewModel viewModel = SetupViewModel(0);
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(true);

            // Act
            Machine machine = viewModel.OpenMachine(prompt.Object, filepath, _mockFileSystem.Object);

            // Verify
            prompt.Verify(x => x(FileTypes.Machine, true), (filepath != null) ? Times.Never() : Times.Once());
            prompt.VerifyNoOtherCalls();

            if (expectedMachineName != null)
            {
                Assert.AreEqual(1, viewModel.Machines.Count);
                Assert.AreEqual(expectedMachineName, viewModel.Machines[0].Name);
                Assert.IsNotNull(machine);
                Assert.Contains(machine, viewModel.Machines);
                _mockSettings.VerifySet(x => x.RecentlyOpened = "test;test.cpvc", Times.Once);
            }
            else
            {
                Assert.IsEmpty(viewModel.Machines);
                _mockFileSystem.Verify(fileSystem => fileSystem.ReadLines(AnyString()), Times.Never);
            }
        }

        [Test]
        public void OpenInvalid()
        {
            // Setup
            _settingGet = "Test;test.cpvc";
            _lines = new string[] { "invalid" };
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, "test.cpvc");
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(true);

            // Act and Verify
            Assert.Throws<Exception>(() => viewModel.OpenMachine(prompt.Object, "test.cpvc", _mockFileSystem.Object));
        }

        [TestCase(null, null)]
        [TestCase("test.cpvc", "test")]
        public void NewMachine(string filepath, string expectedMachineName)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0);
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(false);

            // Act
            viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

            // Verify
            prompt.Verify(x => x(FileTypes.Machine, false), Times.Once());
            prompt.VerifyNoOtherCalls();

            if (expectedMachineName != null)
            {
                Assert.AreEqual(1, viewModel.Machines.Count);
                Assert.AreEqual(expectedMachineName, viewModel.Machines[0].Name);
                _mockFileSystem.Verify(fileSystem => fileSystem.DeleteFile(filepath), Times.Once());
                _mockFileSystem.Verify(fileSystem => fileSystem.OpenFile(filepath), Times.Once());
            }
            else
            {
                Assert.IsEmpty(viewModel.Machines);
                _mockFileSystem.VerifyNoOtherCalls();
            }
        }

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(true, "test2")]
        public void RenameMachine(bool active, string newName)
        {
            // Setup
            string oldName = "test";
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            Mock<MainViewModel.PromptForNameDelegate> mockNamePrompt = new Mock<MainViewModel.PromptForNameDelegate>(MockBehavior.Loose);
            mockNamePrompt.Setup(x => x(oldName)).Returns(newName);

            if (active)
            {
                viewModel.ActiveMachine = machine;
            }

            // Act
            viewModel.RenameMachine(mockNamePrompt.Object);

            // Verify
            if (active && newName != null)
            {
                Assert.AreEqual(newName, machine.Name);
            }
            else
            {
                Assert.AreEqual(oldName, machine.Name);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LoadNonZipMedia(byte drive)
        {
            // Setup
            string filename = (drive == 2) ? "test.cdt" : "test.dsk";
            FileTypes fileType = (drive == 2) ? FileTypes.Tape : FileTypes.Disc;
            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, filename);

            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = machine;

            // Act
            if (fileType == FileTypes.Tape)
            {
                viewModel.LoadTape(_mockFileSystem.Object, mockPrompt.Object, null);
            }
            else
            {
                viewModel.LoadDisc(drive, _mockFileSystem.Object, mockPrompt.Object, null);
            }

            // Verify
            mockPrompt.Verify(x => x(fileType, true), Times.Once());
            mockPrompt.VerifyNoOtherCalls();
            _mockFileSystem.Verify(ReadBytes(filename), Times.Once());
        }

        [TestCase(null, 0, 0, false)]
        [TestCase("test.zip", 0, 0, false)]
        [TestCase("test.zip", 1, 0, false)]
        [TestCase("test.zip", 2, 0, false)]
        [TestCase("test.zip", 0, 1, false)]
        [TestCase("test.zip", 1, 1, false)]
        [TestCase("test.zip", 2, 1, false)]
        [TestCase("test.zip", 0, 2, false)]
        [TestCase("test.zip", 1, 2, false)]
        [TestCase("test.zip", 2, 2, false)]
        [TestCase("test.zip", 0, 2, true)]
        [TestCase("test.zip", 1, 2, true)]
        [TestCase("test.zip", 2, 2, true)]
        public void LoadZipMedia(string zipFilename, byte drive, int entryCount, bool selectFile)
        {
            // Setup
            FileTypes fileType = (drive == 2) ? FileTypes.Tape : FileTypes.Disc;

            List<string> entries = new List<string>();
            for (int e = 0; e < entryCount; e++)
            {
                entries.Add(String.Format("test{0}.{1}", e, (drive == 2) ? "cdt" : "dsk"));
            }

            // Throw in a file with a different extension in order to verify it isn't shown
            // in the Select Item window.
            List<string> entriesWithExtraneousFile = new List<string>(entries)
            {
                "test.txt"
            };

            _mockFileSystem.Setup(GetZipFileEntryNames(zipFilename)).Returns(entriesWithExtraneousFile);

            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, zipFilename);
            Mock<MainViewModel.SelectItemDelegate> mockSelect = new Mock<MainViewModel.SelectItemDelegate>();

            if (entryCount > 0)
            {
                _mockFileSystem.Setup(GetZipFileEntry(zipFilename, entries[0])).Returns(new byte[1]);

                if (entryCount > 1)
                {
                    mockSelect.Setup(x => x(entries)).Returns(selectFile ? entries[0] : null);
                }
            }

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            viewModel.ActiveMachine = machine;

            // Act
            if (drive == 2)
            {
                viewModel.LoadTape(_mockFileSystem.Object, mockPrompt.Object, mockSelect.Object);
            }
            else
            {
                viewModel.LoadDisc(drive, _mockFileSystem.Object, mockPrompt.Object, mockSelect.Object);
            }

            // Verify
            mockPrompt.Verify(x => x(fileType, true), Times.Once());

            if (entryCount == 0)
            {
                _mockFileSystem.Verify(GetZipFileEntry(zipFilename), Times.Never());
            }
            else if (entryCount == 1)
            {
                _mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), Times.Once());
            }
            else if (entryCount > 1)
            {
                _mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), selectFile ? Times.Once() : Times.Never());
                mockSelect.Verify(x => x(entries), Times.Once());
            }

            if (zipFilename != null)
            {
                _mockFileSystem.Verify(GetZipFileEntryNames(zipFilename), Times.Once());
            }

            mockSelect.VerifyNoOtherCalls();
        }

        [Test]
        public void NullActiveMachine()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = null;

            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, null);

            // Act and Verify
            Assert.DoesNotThrow(() =>
            {
                viewModel.Key(Keys.A, true);
                viewModel.Reset(null);
                viewModel.Pause(null);
                viewModel.Resume(null);
                viewModel.LoadDisc(0, _mockFileSystem.Object, prompt.Object, null);
                viewModel.LoadTape(_mockFileSystem.Object, prompt.Object, null);
                viewModel.AddBookmark();
                viewModel.SeekToLastBookmark();
                viewModel.EnableTurbo(true);
                viewModel.CompactFile();
                viewModel.Close(null);
            });
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SelectBookmark(bool active, bool selectEvent)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            Mock<MainViewModel.PromptForBookmarkDelegate> prompt = new Mock<MainViewModel.PromptForBookmarkDelegate>(MockBehavior.Strict);
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 0, DateTime.Now, null);
            prompt.Setup(p => p()).Returns(selectEvent ? historyEvent : null);
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.SelectBookmark(prompt.Object);

            // Verify
            if (active)
            {
                prompt.Verify(p => p(), Times.Once());
                if (selectEvent)
                {
                    Assert.AreEqual(historyEvent, machine.CurrentEvent);
                }
                else
                {
                    Assert.AreNotEqual(historyEvent, machine.CurrentEvent);
                }
            }
            else
            {
                prompt.Verify(p => p(), Times.Never());
                Assert.AreNotEqual(historyEvent, machine.CurrentEvent);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadAudio(bool active)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = active ? machine : null;

            // Run the machine enough to fill up the audio buffer.
            Run(machine, 4000000, true);

            // Act
            byte[] buffer = new byte[4];
            int samples = viewModel.ReadAudio(buffer, 0, 1);

            // Verify
            Assert.AreEqual(active ? 1 : 0, samples);

            // Since the machine's audio buffer was been filled up, it should not have been runnable when calling
            // RunUntil with StopReasons.AudioOverrun. If the call to ReadAudio above has been successful, then
            // the machine's audio buffer won't be full and the machine can now be run.
            UInt64 ticks = Run(machine, 1, true);
            Assert.AreNotEqual(0, ticks);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SetActiveMachine(bool closed)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            if (closed)
            {
                machine.Close();
            }
            else
            {
                machine.Open();
            }

            // Act
            viewModel.ActiveMachine = machine;

            // Verify
            Assert.IsFalse(machine.RequiresOpen);
            Assert.AreEqual(machine, viewModel.ActiveMachine);
        }

        [Test]
        public void SetActiveNonMachine()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0);
            object nonMachine = new object();

            // Act
            viewModel.ActiveItem = nonMachine;

            // Verify
            Assert.IsNull(viewModel.ActiveMachine);
            Assert.AreEqual(nonMachine, viewModel.ActiveItem);
        }

        [Test]
        public void Remove()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            machine.Open();

            // Act
            viewModel.Remove(machine);

            // Verify
            Assert.IsEmpty(viewModel.Machines);
            _mockSettings.VerifySet(x => x.RecentlyOpened = "", Times.Once);
        }

        [Test]
        public void OpenAndCloseMultiple()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(2);
            foreach (Machine machine in viewModel.Machines)
            {
                machine.Open();
            }

            // Act
            viewModel.CloseAll();

            // Verify
            Assert.AreEqual(2, viewModel.Machines.Count);
            Assert.IsTrue(viewModel.Machines[0].RequiresOpen);
            Assert.IsTrue(viewModel.Machines[1].RequiresOpen);
        }

        [Test]
        public void Toggle()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Machine machine = viewModel.Machines[0];
            machine.Open();

            // Act
            bool runningState1 = machine.Core.Running;
            viewModel.ToggleRunning(machine);
            bool runningState2 = machine.Core.Running;
            viewModel.ToggleRunning(machine);
            bool runningState3 = machine.Core.Running;

            // Verify
            Assert.AreEqual(runningState1, runningState3);
            Assert.AreNotEqual(runningState1, runningState2);
        }

        [Test]
        public void ToggleNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0);

            // Act and Verify
            Assert.DoesNotThrow(() => viewModel.ToggleRunning(null));
        }
    }
}
