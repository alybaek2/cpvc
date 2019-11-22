using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class MainViewLogicTests
    {
        static private Mock<IFileSystem> GetFileSystem(string filename, string name)
        {
            Mock<IFile> mockFile = new Mock<IFile>();
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);
            mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(mockFile.Object);
            mockFileSystem.Setup(DeleteFile(filename));

            string[] lines = new string[] { String.Format("name:{0}", name), "checkpoint:0:0:0:0" };

            mockFileSystem.Setup(fileSystem => fileSystem.ReadLines(filename)).Returns(lines);

            return mockFileSystem;
        }

        static private Mock<MainViewLogic.PromptForFileDelegate> GetPrompt(FileTypes fileType, bool existing, string filepath)
        {
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = new Mock<MainViewLogic.PromptForFileDelegate>();
            mockPrompt.Setup(x => x(fileType, existing)).Returns(filepath);

            return mockPrompt;
        }

        static private Expression<Func<IFileSystem, byte[]>> GetZipFileEntry(string filename)
        {
            return fileSystem => fileSystem.GetZipFileEntry(filename, AnyString());
        }

        static private Expression<Func<IFileSystem, byte[]>> GetZipFileEntry(string filename, string entryName)
        {
            return fileSystem => fileSystem.GetZipFileEntry(filename, entryName);
        }

        static private Expression<Func<IFileSystem, List<string>>> GetZipFileEntryNames(string filename)
        {
            return fileSystem => fileSystem.GetZipFileEntryNames(filename);
        }

        [TestCase(null, null)]
        [TestCase("test.cpvc", "test")]
        public void NewMachine(string filepath, string expectedMachineName)
        {
            // Setup
            Mock<IFileSystem> mockFileSystem = GetFileSystem(filepath, expectedMachineName);
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = GetPrompt(FileTypes.Machine, false, filepath);
            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);
            MainViewModel mockViewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);

            // Act
            MainViewLogic logic = new MainViewLogic(mockViewModel);
            logic.NewMachine(mockFileSystem.Object, mockPrompt.Object);

            // Verify
            mockPrompt.Verify(x => x(FileTypes.Machine, false), Times.Once());
            mockPrompt.VerifyNoOtherCalls();

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
        public void OpenMachine(string filepath, string promptedFilePath, string expectedMachineName)
        {
            // Setup
            Mock<IFileSystem> mockFileSystem = GetFileSystem(filepath ?? promptedFilePath, expectedMachineName);
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = GetPrompt(FileTypes.Machine, true, promptedFilePath);
            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);
            MainViewModel mockViewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);

            // Act
            MainViewLogic logic = new MainViewLogic(mockViewModel);
            logic.OpenMachine(filepath, mockFileSystem.Object, mockPrompt.Object);

            // Verify
            mockPrompt.Verify(x => x(FileTypes.Machine, true), (filepath != null) ? Times.Never() : Times.Once());
            mockPrompt.VerifyNoOtherCalls();

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

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LoadNonZipMedia(byte drive)
        {
            // Setup
            string filename = (drive == 2) ? "test.cdt" : "test.dsk";
            FileTypes fileType = (drive == 2) ? FileTypes.Tape : FileTypes.Disc;
            Mock<IFileSystem> mockFileSystem = GetFileSystem("test.cpvc", "test");
            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = GetPrompt(fileType, true, filename);
            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);
            MainViewModel viewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);

            // Act
            MainViewLogic logic = new MainViewLogic(viewModel);
            Machine machine = Machine.New("test", "test.cpvc", mockFileSystem.Object);
            logic.ActiveMachine = machine;
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
        [TestCase(0, 2, true)]
        [TestCase(1, 2, true)]
        [TestCase(2, 2, true)]
        public void LoadZipMedia(byte drive, int entryCount, bool selectFile)
        {
            // Setup
            string zipFilename = "test.zip";
            FileTypes fileType = (drive == 2) ? FileTypes.Tape : FileTypes.Disc;
            Mock<IFileSystem> mockFileSystem = GetFileSystem("test.cpvc", "test");

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

            Mock<MainViewLogic.PromptForFileDelegate> mockPrompt = GetPrompt(fileType, true, zipFilename);
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

            // Act
            Machine machine = Machine.New("test", "test.cpvc", mockFileSystem.Object);
            logic.ActiveMachine = machine;
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
