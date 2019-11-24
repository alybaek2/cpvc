using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MainViewLogicTests
    {
        private Mock<ISettings> _mockSettings;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISettings>(MockBehavior.Loose);
        }

        static private Mock<IFileSystem> SetupFileSystem(string machineFilepath, string machineName)
        {
            Mock<IFile> mockFile = new Mock<IFile>();

            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);
            mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(mockFile.Object);
            mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(machineFilepath));

            string[] lines = new string[] { String.Format("name:{0}", machineName), "checkpoint:0:0:0:0" };
            mockFileSystem.Setup(fileSystem => fileSystem.ReadLines(machineFilepath)).Returns(lines);

            mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));

            return mockFileSystem;
        }

        static private MainViewLogic SetupViewLogic()
        {
            // Setup
            Mock<ISettings> settings = new Mock<ISettings>(MockBehavior.Loose);
            Mock<IFileSystem> fileSystem = SetupFileSystem("test.cpvc", "test");
            MainViewModel viewModel = new MainViewModel(settings.Object, fileSystem.Object);

            MainViewLogic logic = new MainViewLogic(viewModel);
            logic.OpenMachine("test.cpvc", fileSystem.Object, null);

            return logic;
        }

        static private Mock<MainViewLogic.PromptForFileDelegate> SetupPrompt(FileTypes fileType, bool existing, string filepath)
        {
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = new Mock<MainViewLogic.PromptForFileDelegate>();
            mockPrompt.Setup(x => x(fileType, existing)).Returns(filepath);

            return mockPrompt;
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

        [TestCase(null, null)]
        [TestCase("test.cpvc", "test")]
        public void NewMachine(string filepath, string expectedMachineName)
        {
            // Setup
            Mock<IFileSystem> fileSystem = SetupFileSystem(filepath, null);
            Mock<MainViewLogic.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);
            MainViewModel mockViewModel = new MainViewModel(_mockSettings.Object, fileSystem.Object);
            MainViewLogic logic = new MainViewLogic(mockViewModel);

            // Act
            logic.NewMachine(fileSystem.Object, prompt.Object);

            // Verify
            prompt.Verify(x => x(FileTypes.Machine, false), Times.Once());
            prompt.VerifyNoOtherCalls();

            if (expectedMachineName != null)
            {
                Assert.AreEqual(mockViewModel.Machines.Count, 1);
                Assert.AreEqual(mockViewModel.Machines[0].Name, expectedMachineName);
            }
            else
            {
                Assert.AreEqual(mockViewModel.Machines.Count, 0);
            }
        }

        [TestCase(null, null, null)]
        [TestCase(null, "test.cpvc", "test")]
        [TestCase("test.cpvc", null, "test")]
        public void OpenMachine(string filepath, string promptedFilepath, string expectedMachineName)
        {
            // Setup
            Mock<MainViewLogic.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, promptedFilepath);
            Mock<IFileSystem> fileSystem = SetupFileSystem(filepath ?? promptedFilepath, expectedMachineName);
            MainViewModel mockViewModel = new MainViewModel(_mockSettings.Object, fileSystem.Object);

            // Act
            MainViewLogic logic = new MainViewLogic(mockViewModel);
            logic.OpenMachine(filepath, fileSystem.Object, prompt.Object);

            // Verify
            prompt.Verify(x => x(FileTypes.Machine, true), (filepath != null) ? Times.Never() : Times.Once());
            prompt.VerifyNoOtherCalls();

            if (expectedMachineName != null)
            {
                Assert.AreEqual(mockViewModel.Machines.Count, 1);
                Assert.AreEqual(expectedMachineName, mockViewModel.Machines[0].Name);
            }
            else
            {
                Assert.AreEqual(mockViewModel.Machines.Count, 0);
            }
        }

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(true, "test2")]
        public void RenameMachine(bool active, string newName)
        {
            // Setup
            string oldName = "test";
            Mock<IFileSystem> mockFileSystem = SetupFileSystem("test.cpvc", oldName);
            MainViewModel mockViewModel = new MainViewModel(_mockSettings.Object, mockFileSystem.Object);
            MainViewLogic logic = new MainViewLogic(mockViewModel);
            Mock<MainViewLogic.PromptForNameDelegate> mockPrompt = new Mock<MainViewLogic.PromptForNameDelegate>(MockBehavior.Loose);
            mockPrompt.Setup(x => x(oldName)).Returns(newName);

            Machine machine = mockViewModel.OpenMachine("test.cpvc", mockFileSystem.Object);
            if (active)
            {
                logic.ActiveMachine = machine;
            }

            // Act
            logic.RenameMachine(mockPrompt.Object);

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
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, filename);

            Mock<IFileSystem> mockFileSystem = SetupFileSystem("test.cpvc", "test");
            MainViewLogic logic = SetupViewLogic();
            logic.ActiveMachine = logic.ViewModel.Machines[0];

            // Act
            if (fileType == FileTypes.Tape)
            {
                logic.LoadTape(mockFileSystem.Object, mockPrompt.Object, null);
            }
            else
            {
                logic.LoadDisc(drive, mockFileSystem.Object, mockPrompt.Object, null);
            }

            // Verify
            mockPrompt.Verify(x => x(fileType, true), Times.Once());
            mockPrompt.VerifyNoOtherCalls();
            mockFileSystem.Verify(ReadBytes(filename), Times.Once());
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
            Mock<IFileSystem> mockFileSystem = SetupFileSystem("test.cpvc", "test");

            List<string> entries = new List<string>();
            for (int e = 0; e < entryCount; e++)
            {
                entries.Add(String.Format("test{0}.{1}", e, (drive == 2) ? "cdt" : "dsk"));
            }

            // Throw in a file with a different extension in order to verify it isn't shown
            // in the Select Item window.
            List<string> entriesWithExtraneousFile = new List<string>(entries);
            entriesWithExtraneousFile.Add("test.txt");

            mockFileSystem.Setup(GetZipFileEntryNames(zipFilename)).Returns(entriesWithExtraneousFile);

            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, zipFilename);
            Mock<MainViewLogic.SelectItemDelegate> mockSelect = new Mock<MainViewLogic.SelectItemDelegate>();

            if (entryCount > 0)
            {
                mockFileSystem.Setup(GetZipFileEntry(zipFilename, entries[0])).Returns(new byte[1]);

                if (entryCount > 1)
                {
                    mockSelect.Setup(x => x(entries)).Returns(selectFile ? entries[0] : null);
                }
            }

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);
            MainViewModel viewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);
            MainViewLogic logic = new MainViewLogic(viewModel);
            Machine machine = Machine.New("test", "test.cpvc", mockFileSystem.Object);
            logic.ActiveMachine = machine;

            // Act
            if (drive == 2)
            {
                logic.LoadTape(mockFileSystem.Object, mockPrompt.Object, mockSelect.Object);
            }
            else
            {
                logic.LoadDisc(drive, mockFileSystem.Object, mockPrompt.Object, mockSelect.Object);
            }

            // Verify
            mockPrompt.Verify(x => x(fileType, true), Times.Once());

            if (entryCount == 0)
            {
                mockFileSystem.Verify(GetZipFileEntry(zipFilename), Times.Never());
            }
            else if (entryCount == 1)
            {
                mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), Times.Once());
            }
            else if (entryCount > 1)
            {
                mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), selectFile ? Times.Once() : Times.Never());
                mockSelect.Verify(x => x(entries), Times.Once());
            }

            if (zipFilename != null)
            {
                mockFileSystem.Verify(GetZipFileEntryNames(zipFilename), Times.Once());
            }

            mockSelect.VerifyNoOtherCalls();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullActiveMachine(bool active)
        {
            // Setup
            Mock<IFileSystem> fileSystem = SetupFileSystem("test.cpvc", "test");
            Mock<MainViewLogic.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, null);
            MainViewLogic logic = SetupViewLogic();
            Machine machine = logic.ViewModel.Machines[0];

            logic.ActiveMachine = active ? machine : null;

            // Act and Verify
            Assert.DoesNotThrow(() =>
            {
                logic.Key(Keys.A, true);
                logic.Reset();
                logic.Pause();
                logic.Resume();
                logic.LoadDisc(0, fileSystem.Object, prompt.Object, null);
                logic.LoadTape(fileSystem.Object, prompt.Object, null);
                logic.AddBookmark();
                logic.SeekToLastBookmark();
                logic.EnableTurbo(true);
                logic.CompactFile();
                logic.Close();
            });
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SelectBookmark(bool active, bool selectEvent)
        {
            // Setup
            Mock<MainViewLogic.PromptForBookmarkDelegate> prompt = new Mock<MainViewLogic.PromptForBookmarkDelegate>(MockBehavior.Strict);
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 0, DateTime.Now, null);
            prompt.Setup(p => p()).Returns(selectEvent ? historyEvent : null);
            Mock<IFileSystem> fileSystem = SetupFileSystem("test.cpvc", "test");
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, fileSystem.Object);

            // Act
            MainViewLogic logic = new MainViewLogic(viewModel);
            logic.OpenMachine("test.cpvc", fileSystem.Object, null);
            logic.ActiveMachine = active ? viewModel.Machines[0] : null;

            // Act
            logic.SelectBookmark(prompt.Object);

            // Verify
            if (active)
            {
                prompt.Verify(p => p(), Times.Once());
                if (selectEvent)
                {
                    Assert.AreEqual(historyEvent, logic.ActiveMachine.CurrentEvent);
                }
                else
                {
                    Assert.AreNotEqual(historyEvent, logic.ActiveMachine.CurrentEvent);
                }
            }
            else
            {
                prompt.Verify(p => p(), Times.Never());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadAudio(bool active)
        {
            // Setup
            MainViewLogic logic = SetupViewLogic();
            Machine machine = logic.ViewModel.Machines[0];
            if (!active)
            {
                logic.ActiveMachine = null;
            }

            // Run the machine enough to fill up the audio buffer.
            Run(machine, 4000000, true);

            // Act
            byte[] buffer = new byte[4];
            int samples = logic.ReadAudio(buffer, 0, 1);

            // Verify
            Assert.AreEqual(active ? 1 : 0, samples);

            // Since the machine's audio buffer was been filled up, it should not be runnable when calling RunUntil
            // with StopReasons.AudioOverrun. If the call to ReadAudio above has been successful, then the machine's
            // audio buffer won't be full and the machine can be run.
            UInt64 ticks = Run(machine, 1, true);
            Assert.AreNotEqual(0, ticks);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SetActiveMachine(bool closed)
        {
            // Setup
            MainViewLogic logic = SetupViewLogic();
            Machine machine = logic.ViewModel.Machines[0];
            if (closed)
            {
                machine.Close();
            }

            // Act
            logic.ActiveMachine = machine;

            // Verify
            Assert.IsFalse(machine.RequiresOpen);
            Assert.AreEqual(machine, logic.ActiveMachine);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SetActiveNonMachine(bool closed)
        {
            // Setup
            MainViewLogic logic = SetupViewLogic();
            object nonMachine = new object();

            // Act
            logic.ActiveItem = nonMachine;

            // Verify
            Assert.IsNull(logic.ActiveMachine);
            Assert.AreEqual(nonMachine, logic.ActiveItem);
        }
    }
}
