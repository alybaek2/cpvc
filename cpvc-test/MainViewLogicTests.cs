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

            return mockFileSystem;
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
            Mock<IFileSystem> mockFileSystem = SetupFileSystem("test.cpvc", "test");
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, filename);
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, mockFileSystem.Object);
            MainViewLogic logic = new MainViewLogic(viewModel);
            Machine machine = Machine.New("test", "test.cpvc", mockFileSystem.Object);
            logic.ActiveMachine = machine;

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

        [TestCase(0, 0, false)]
        [TestCase(1, 0, false)]
        [TestCase(2, 0, false)]
        [TestCase(0, 1, false)]
        [TestCase(1, 1, false)]
        [TestCase(2, 1, false)]
        [TestCase(0, 2, false)]
        [TestCase(1, 2, false)]
        [TestCase(2, 2, false)]
        [TestCase(0, 2, false)]
        [TestCase(1, 2, false)]
        [TestCase(2, 2, false)]
        [TestCase(0, 2, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 2, true)]
        public void LoadZipMedia(byte drive, int entryCount, bool selectFile)
        {
            // Setup
            string zipFilename = "test.zip";
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

            mockFileSystem.Verify(GetZipFileEntryNames(zipFilename), Times.Once());
            mockSelect.VerifyNoOtherCalls();
        }
    }
}
