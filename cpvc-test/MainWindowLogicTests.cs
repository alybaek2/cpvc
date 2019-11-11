using CPvC;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace cpvc_test
{
    public class MainWindowLogicTests
    {
        static private string AnyString()
        {
            return It.IsAny<string>();
        }

        static private Expression<Func<IUserInterface, string>> PromptForFile()
        {
            return userInterface => userInterface.PromptForFile(It.IsAny<FileTypes>(), It.IsAny<bool>());
        }

        static private Expression<Func<IUserInterface, string>> SelectItem()
        {
            return userInterface => userInterface.SelectItem(It.IsAny<List<string>>());
        }

        static private Expression<Action<IUserInterface>> ReportError()
        {
            return userInterface => userInterface.ReportError(AnyString());
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

        static private Expression<Func<IFileSystem, byte[]>> ReadBytes()
        {
            return fileSystem => fileSystem.ReadBytes(AnyString());
        }

        static private Expression<Func<IFileSystem, byte[]>> ReadBytes(string filename)
        {
            return fileSystem => fileSystem.ReadBytes(filename);
        }

        static private Expression<Action<IFileSystem>> DeleteFile(string filename)
        {
            return fileSystem => fileSystem.DeleteFile(filename);
        }

        static private Mock<IFileSystem> GetFileSystem(IFile file, string filename)
        {
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);
            mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(file);
            mockFileSystem.Setup(DeleteFile(filename));

            return mockFileSystem;
        }

        [Test]
        public void ReportsErrorWhenNewMachineFails()
        {
            // Setup
            string filepath = "test.cpvc";
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(ReadBytes()).Throws(new Exception("File missing"));
            mockFileSystem.Setup(DeleteFile(filepath));

            Mock<IUserInterface> mockUserInterface = new Mock<IUserInterface>(MockBehavior.Strict);
            mockUserInterface.Setup(PromptForFile()).Returns(filepath);
            mockUserInterface.Setup(ReportError());

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act
            MainWindowLogic logic = new MainWindowLogic(mockUserInterface.Object, mockFileSystem.Object, mockSettings.Object);
            logic.NewMachine();

            // Verify
            mockUserInterface.Verify(PromptForFile(), Times.Once());
            mockUserInterface.Verify(ReportError(), Times.Once());
            mockUserInterface.VerifyNoOtherCalls();
        }

        [TestCase(null)]
        [TestCase("test.cpvc")]
        public void NewMachine(string filepath)
        {
            // Setup
            Mock<IFile> mockFile = new Mock<IFile>();
            Mock<IFileSystem> mockFileSystem = GetFileSystem(mockFile.Object, filepath);
            Mock<IUserInterface> mockUserInterface = new Mock<IUserInterface>(MockBehavior.Strict);
            mockUserInterface.Setup(PromptForFile()).Returns(filepath);
            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act
            MainWindowLogic logic = new MainWindowLogic(mockUserInterface.Object, mockFileSystem.Object, mockSettings.Object);
            logic.NewMachine();
            logic.CloseAll();

            // Verify
            mockUserInterface.Verify(PromptForFile(), Times.Once());
            mockUserInterface.VerifyNoOtherCalls();
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LoadDiscWithoutMachine(byte drive)
        {
            // Setup
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            Mock<IUserInterface> mockUserInterface = new Mock<IUserInterface>(MockBehavior.Strict);
            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act
            MainWindowLogic logic = new MainWindowLogic(mockUserInterface.Object, mockFileSystem.Object, mockSettings.Object)
            {
                ActiveMachine = null
            };

            logic.LoadDisc(drive);

            // Verify
            mockUserInterface.VerifyNoOtherCalls();
            mockFileSystem.VerifyNoOtherCalls();
            Assert.IsNull(logic.ActiveMachine);
        }

        [Test]
        public void LoadTapeWithoutMachine()
        {
            // Setup
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            Mock<IUserInterface> mockUserInterface = new Mock<IUserInterface>(MockBehavior.Strict);
            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act
            MainWindowLogic logic = new MainWindowLogic(mockUserInterface.Object, mockFileSystem.Object, mockSettings.Object)
            {
                ActiveMachine = null
            };

            logic.LoadTape();

            // Verify
            mockUserInterface.VerifyNoOtherCalls();
            mockFileSystem.VerifyNoOtherCalls();
            Assert.IsNull(logic.ActiveMachine);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void LoadNonZipMedia(byte drive)
        {
            // Setup
            Mock<IFile> mockFile = new Mock<IFile>();
            string filename = (drive == 2) ? "test.cdt" : "test.dsk";
            Mock<IFileSystem> mockFileSystem = GetFileSystem(mockFile.Object, "test.cpvc");
            Mock<IUserInterface> mockUserInterface = new Mock<IUserInterface>(MockBehavior.Strict);
            mockUserInterface.Setup(PromptForFile()).Returns(filename);

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act
            MainWindowLogic logic = new MainWindowLogic(mockUserInterface.Object, mockFileSystem.Object, mockSettings.Object);
            Machine machine = Machine.New("test", "test.cpvc", mockFileSystem.Object);
            logic.ActiveMachine = machine;
            if (drive == 2)
            {
                logic.LoadTape();
            }
            else
            {
                logic.LoadDisc(drive);
            }

            // Verify
            mockUserInterface.Verify(PromptForFile(), Times.Once());
            mockUserInterface.VerifyNoOtherCalls();
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
            Mock<IFile> mockFile = new Mock<IFile>();
            string zipFilename = "test.zip";
            Mock<IFileSystem> mockFileSystem = GetFileSystem(mockFile.Object, "test.cpvc");

            List<string> entries = new List<string>();
            for (int e = 0; e < entryCount; e++)
            {
                entries.Add(String.Format("test{0}.{1}", e, (drive == 2) ? "cdt" : "dsk"));
            }

            mockFileSystem.Setup(GetZipFileEntryNames(zipFilename)).Returns(entries);

            Mock<IUserInterface> mockUserInterface = new Mock<IUserInterface>(MockBehavior.Strict);
            mockUserInterface.Setup(PromptForFile()).Returns(zipFilename);

            if (entryCount > 0)
            {
                mockFileSystem.Setup(GetZipFileEntry(zipFilename, entries[0])).Returns(new byte[1]);

                if (entryCount > 1)
                {
                    mockUserInterface.Setup(SelectItem()).Returns(selectFile ? entries[0] : null);
                }
            }

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act
            MainWindowLogic logic = new MainWindowLogic(mockUserInterface.Object, mockFileSystem.Object, mockSettings.Object);
            Machine machine = Machine.New("test", "test.cpvc", mockFileSystem.Object);
            logic.ActiveMachine = machine;
            if (drive == 2)
            {
                logic.LoadTape();
            }
            else
            {
                logic.LoadDisc(drive);
            }

            // Verify
            mockUserInterface.Verify(PromptForFile(), Times.Once());

            if (entryCount == 0)
            {
                mockFileSystem.Verify(GetZipFileEntry(zipFilename), Times.Never());
            }
            else if (entryCount == 1)
            {
                mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), Times.Once());
                mockUserInterface.Verify(SelectItem(), Times.Never());
            }
            else if (entryCount > 1)
            {
                mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), selectFile ? Times.Once() : Times.Never());

                mockUserInterface.Verify(SelectItem(), Times.Once());
            }

            mockFileSystem.Verify(GetZipFileEntryNames(zipFilename), Times.Once());
            mockUserInterface.VerifyNoOtherCalls();
        }
    }
}
