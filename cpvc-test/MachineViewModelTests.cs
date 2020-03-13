using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CPvC.Test
{
    public class MachineViewModelTests
    {
        static private void TestLoadMedia(
            bool nullMachine,
            FileTypes fileType,
            bool isZipped,
            int entryCount,
            bool selectFile,
            Func<MachineViewModel, ICommand> getCommand,
            Func<byte[], System.Linq.Expressions.Expression<Action<IInteractiveMachine>>> verifyLoad)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IInteractiveMachine> mockInteractiveMachine = mockMachine.As<IInteractiveMachine>();
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>();
            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = new Mock<MainViewModel.PromptForFileDelegate>();
            Mock<MainViewModel.SelectItemDelegate> mockSelect = new Mock<MainViewModel.SelectItemDelegate>();

            string ext = (fileType == FileTypes.Tape) ? "cdt" : "dsk";
            string filename = isZipped ? "test.zip" : String.Format("test.{0}", ext);

            mockPrompt.Setup(p => p(fileType, true)).Returns(filename);

            if (isZipped)
            {
                List<string> entries = new List<string>();
                for (int e = 0; e < entryCount; e++)
                {
                    entries.Add(String.Format("test{0}.{1}", e, ext));
                }

                // Throw in a file with a different extension in order to verify it isn't shown
                // in the Select Item window.
                List<string> entriesWithExtraneousFile = new List<string>(entries)
                {
                    "test.txt"
                };

                mockFileSystem.Setup(fileSystem => fileSystem.GetZipFileEntryNames(filename)).Returns(entriesWithExtraneousFile);

                if (entryCount > 0)
                {
                    mockFileSystem.Setup(fileSystem => fileSystem.GetZipFileEntry(filename, entries[0])).Returns(new byte[1] { 0x01 });

                    if (entryCount > 1)
                    {
                        mockSelect.Setup(x => x(entries)).Returns(selectFile ? entries[0] : null);
                    }
                }
            }
            else
            {
                mockFileSystem.Setup(fileSystem => fileSystem.ReadBytes(filename)).Returns(new byte[1] { 0x02 });
            }

            MachineViewModel machineViewModel = new MachineViewModel(nullMachine ? null : mockMachine.Object, mockFileSystem.Object, mockPrompt.Object, null, null, mockSelect.Object);

            ICommand command = getCommand(machineViewModel);

            // Act
            command.Execute(null);

            // Verify
            if (!nullMachine)
            {
                mockInteractiveMachine.Verify(m => m.AutoPause());
            }

            if (selectFile && isZipped && !nullMachine)
            {
                if (entryCount > 0)
                {
                    mockInteractiveMachine.Verify(verifyLoad(new byte[1] { 0x01 }), Times.Once());
                }
                else
                {
                    mockInteractiveMachine.Verify(verifyLoad(It.IsAny<byte[]>()), Times.Never());
                }
            }
            else if (!isZipped && !nullMachine)
            {
                mockInteractiveMachine.Verify(verifyLoad(new byte[1] { 0x02 }), Times.Once());
            }

            mockMachine.VerifyNoOtherCalls();

            Assert.AreEqual(!nullMachine, command.CanExecute(null));
        }

        static private void TestCommand<T>(bool nullMachine, Func<MachineViewModel, ICommand> getCommand, System.Linq.Expressions.Expression<Action<T>> verifyCall) where T : class
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<T> mockOpenableMachine = mockMachine.As<T>();
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            getCommand(model).Execute(null);

            // Verify
            mockOpenableMachine.Verify(verifyCall, nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, getCommand(model).CanExecute(null));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Open(bool nullMachine, bool requiresOpen)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
            mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.OpenCommand.Execute(null);

            // Verify
            mockOpenableMachine.Verify(m => m.Open(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && requiresOpen, model.OpenCommand.CanExecute(null));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Close(bool nullMachine, bool requiresOpen)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
            mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.CloseCommand.Execute(null);

            // Verify
            mockOpenableMachine.Verify(m => m.Close(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && !requiresOpen, model.CloseCommand.CanExecute(null));
        }

        [Test]
        public void Pause(
            [Values(false, true)] bool nullMachine,
            [Values(false, true)] bool requiresOpen,
            [Values(false, true)] bool running,
            [Values(false, true)] bool isOpenable)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            if (isOpenable)
            {
                Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
                mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            }
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            mockMachine.SetupGet(x => x.Running).Returns(running);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.PauseCommand.Execute(null);

            // Verify
            mockPausableMachine.Verify(m => m.Stop(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && (!isOpenable || !requiresOpen) && running, model.PauseCommand.CanExecute(null));
        }

        [Test]
        public void Resume(
            [Values(false, true)] bool nullMachine,
            [Values(false, true)] bool requiresOpen,
            [Values(false, true)] bool running,
            [Values(false, true)] bool isOpenable)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            if (isOpenable)
            {
                Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
                mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            }
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            mockMachine.SetupGet(x => x.Running).Returns(running);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.ResumeCommand.Execute(null);

            // Verify
            mockPausableMachine.Verify(m => m.Start(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && (!isOpenable || !requiresOpen) && !running, model.ResumeCommand.CanExecute(null));
        }

        [TestCase(true, false, 0, false)]
        [TestCase(false, false, 0, false)]
        [TestCase(false, true, 0, true)]
        [TestCase(false, true, 1, true)]
        [TestCase(false, true, 2, true)]
        public void LoadDriveA(bool nullMachine, bool isZipped, int entryCount, bool selectFile)
        {
            TestLoadMedia(nullMachine, FileTypes.Disc, isZipped, entryCount, selectFile, machineViewModel => machineViewModel.DriveACommand, mediaImage => { return m => m.LoadDisc(0, mediaImage); });
        }

        [TestCase(true, false, 0, false)]
        [TestCase(false, false, 0, false)]
        [TestCase(false, true, 0, true)]
        [TestCase(false, true, 1, true)]
        [TestCase(false, true, 2, true)]
        public void LoadDriveB(bool nullMachine, bool isZipped, int entryCount, bool selectFile)
        {
            TestLoadMedia(nullMachine, FileTypes.Disc, isZipped, entryCount, selectFile, machineViewModel => machineViewModel.DriveBCommand, mediaImage => { return m => m.LoadDisc(1, mediaImage); });
        }

        [TestCase(true, false, 0, false)]
        [TestCase(false, false, 0, false)]
        [TestCase(false, true, 0, true)]
        [TestCase(false, true, 1, true)]
        [TestCase(false, true, 2, true)]
        public void LoadTape(bool nullMachine, bool isZipped, int entryCount, bool selectFile)
        {
            TestLoadMedia(nullMachine, FileTypes.Tape, isZipped, entryCount, selectFile, machineViewModel => machineViewModel.TapeCommand, mediaImage => { return m => m.LoadTape(mediaImage); });
        }

        [Test]
        public void Reset([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.ResetCommand, m => m.Reset());
        }

        [Test]
        public void ToggleRunning([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPausableMachine>(nullMachine, m => m.ToggleRunningCommand, m => m.ToggleRunning());
        }

        [Test]
        public void JumpToMostRecentBookmark([Values(false, true)] bool nullMachine)
        {
            TestCommand<IBookmarkableMachine>(nullMachine, m => m.JumpToMostRecentBookmarkCommand, m => m.JumpToMostRecentBookmark());
        }

        [Test]
        public void Compact([Values(false, true)] bool nullMachine)
        {
            TestCommand<ICompactableMachine>(nullMachine, m => m.CompactCommand, m => m.Compact(false));
        }

        [Test]
        public void SeekToNextBookmark([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPrerecordedMachine>(nullMachine, m => m.SeekToNextBookmarkCommand, m => m.SeekToNextBookmark());
        }

        [Test]
        public void SeekToPreviousBookmark([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPrerecordedMachine>(nullMachine, m => m.SeekToPrevBookmarkCommand, m => m.SeekToPreviousBookmark());
        }

        [Test]
        public void SeekToStart([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPrerecordedMachine>(nullMachine, m => m.SeekToStartCommand, m => m.SeekToStart());
        }
    }
}
