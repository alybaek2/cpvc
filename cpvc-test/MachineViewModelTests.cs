using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace CPvC.Test
{
    public class MachineViewModelTests
    {
        static private void TestLoadMedia(
            bool nullMachine,
            FileTypes fileType,
            bool nullFilename,
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
            string filename = nullFilename ? null : (isZipped ? "test.zip" : String.Format("test.{0}", ext));

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

            MachineViewModel machineViewModel = new MachineViewModel(nullMachine ? null : mockMachine.Object, mockFileSystem.Object, mockPrompt.Object, null, null, mockSelect.Object, null, null);

            ICommand command = getCommand(machineViewModel);

            // Act
            command.Execute(null);

            // Verify
            if (!nullMachine)
            {
                mockInteractiveMachine.Verify(m => m.AutoPause());
            }

            if (selectFile && isZipped && !nullMachine && !nullFilename)
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
            else if (!isZipped && !nullMachine && !nullFilename)
            {
                mockInteractiveMachine.Verify(verifyLoad(new byte[1] { 0x02 }), Times.Once());
            }

            mockMachine.VerifyNoOtherCalls();

            Assert.AreEqual(!nullMachine, command.CanExecute(null));
        }

        static private void TestCommand<T>(bool nullMachine, Func<MachineViewModel, ICommand> getCommand, object parameter, System.Linq.Expressions.Expression<Action<T>> verifyCall) where T : class
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<T> mockOpenableMachine = mockMachine.As<T>();
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null, null, null);

            // Act
            getCommand(model).Execute(parameter);

            // Verify
            mockOpenableMachine.Verify(verifyCall, nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, getCommand(model).CanExecute(null));
        }

        [Test]
        public void Open([Values(false, true)] bool nullMachine)
        {
            // Setup
            Mock<IPersistableMachine> mockPersistableMachine = new Mock<IPersistableMachine>();
            mockPersistableMachine.Setup(x => x.OpenFromFile(It.IsAny<IFileSystem>()));
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockPersistableMachine.As<ICoreMachine>().Object, null, null, null, null, null, null, null);

            // Act
            model.OpenCommand.Execute(null);

            // Verify
            mockPersistableMachine.Verify(m => m.OpenFromFile(It.IsAny<IFileSystem>()), nullMachine ? Times.Never() : Times.Once());
        }

        //[TestCase(false, false)]
        //[TestCase(true, false)]
        //[TestCase(false, true)]
        //[TestCase(true, true)]
        //public void Close(bool nullMachine, bool canClose)
        //{
        //    // Setup
        //    Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
        //    mockMachine.Setup(x => x.CanClose()).Returns(canClose);
        //    MachineViewModel model = new MachineViewModel(null, null, nullMachine ? null : mockMachine.Object, null, null, null, null, null);

        //    // Act
        //    model.CloseCommand.Execute(null);

        //    // Verify
        //    mockMachine.Verify(m => m.Close(), nullMachine ? Times.Never() : Times.Once());
        //    Assert.AreEqual(!nullMachine && canClose, model.CloseCommand.CanExecute(null));
        //}

        [TestCase(false)]
        [TestCase(true)]
        public void ReverseStart(bool nullMachine)
        {
            // Setup
            Mock<IReversibleMachine> mockMachine = new Mock<IReversibleMachine>();
            Mock<ICoreMachine> mockCoreMachine = mockMachine.As<ICoreMachine>();
            mockMachine.Setup(x => x.Reverse());
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockCoreMachine.Object, null, null, null, null, null, null, null);

            // Act
            model.ReverseStartCommand.Execute(null);

            // Verify
            mockMachine.Verify(m => m.Reverse(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, model.ReverseStartCommand.CanExecute(null));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReverseStop(bool nullMachine)
        {
            // Setup
            Mock<IReversibleMachine> mockMachine = new Mock<IReversibleMachine>();
            Mock<ICoreMachine> mockCoreMachine = mockMachine.As<ICoreMachine>();
            mockMachine.Setup(x => x.ReverseStop());
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockCoreMachine.Object, null, null, null, null, null, null, null);

            // Act
            model.ReverseStopCommand.Execute(null);

            // Verify
            mockMachine.Verify(m => m.ReverseStop(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, model.ReverseStopCommand.CanExecute(null));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EnableTurbo(bool nullMachine)
        {
            // Setup
            Mock<ITurboableMachine> mockMachine = new Mock<ITurboableMachine>();
            Mock<ICoreMachine> mockCoreMachine = mockMachine.As<ICoreMachine>();
            mockMachine.Setup(x => x.EnableTurbo(true));
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockCoreMachine.Object, null, null, null, null, null, null, null);

            // Act
            model.TurboCommand.Execute(true);

            // Verify
            mockMachine.Verify(m => m.EnableTurbo(true), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, model.TurboCommand.CanExecute(null));
        }

        [Test]
        public void Pause(
            [Values(false, true)] bool nullMachine,
            [Values(false, true)] bool canStop,
            [Values(false, true)] bool isPausable)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IPausableMachine> mockPausableMachine = null;
            if (isPausable)
            {
                mockPausableMachine = mockMachine.As<IPausableMachine>();
                mockPausableMachine.SetupGet(x => x.CanStop).Returns(canStop);
            }
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null, null, null);

            // Act
            model.PauseCommand.Execute(null);

            // Verify
            mockPausableMachine?.Verify(m => m.Stop(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && canStop && isPausable, model.PauseCommand.CanExecute(null));
        }

        [Test]
        public void Resume(
            [Values(false, true)] bool nullMachine,
            [Values(false, true)] bool canStart,
            [Values(false, true)] bool isPausable)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IPausableMachine> mockPausableMachine = null;
            if (isPausable)
            {
                mockPausableMachine = mockMachine.As<IPausableMachine>();
                mockPausableMachine.SetupGet(x => x.CanStart).Returns(canStart);
            }
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null, null, null);

            // Act
            model.ResumeCommand.Execute(null);

            // Verify
            mockPausableMachine?.Verify(m => m.Start(), (nullMachine || !isPausable) ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && canStart && isPausable, model.ResumeCommand.CanExecute(null));
        }

        [TestCase(false, true, false, 0, false)]
        [TestCase(true, false, false, 0, false)]
        [TestCase(false, false, false, 0, false)]
        [TestCase(false, false, true, 2, false)]
        [TestCase(false, false, true, 0, true)]
        [TestCase(false, false, true, 1, true)]
        [TestCase(false, false, true, 2, true)]
        public void LoadDriveA(bool nullMachine, bool nullFilename, bool isZipped, int entryCount, bool selectFile)
        {
            TestLoadMedia(nullMachine, FileTypes.Disc, nullFilename, isZipped, entryCount, selectFile, machineViewModel => machineViewModel.DriveACommand, mediaImage => { return m => m.LoadDisc(0, mediaImage); });
        }

        [Test]
        public void EjectDriveA([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.DriveAEjectCommand, null, m => m.LoadDisc(0, null));
        }

        [Test]
        public void EjectDriveB([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.DriveBEjectCommand, null, m => m.LoadDisc(1, null));
        }

        [Test]
        public void EjectTape([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.TapeEjectCommand, null, m => m.LoadTape(null));
        }

        [TestCase(false, true, false, 0, false)]
        [TestCase(true, false, false, 0, false)]
        [TestCase(false, false, false, 0, false)]
        [TestCase(false, false, true, 2, false)]
        [TestCase(false, false, true, 0, true)]
        [TestCase(false, false, true, 1, true)]
        [TestCase(false, false, true, 2, true)]
        public void LoadDriveB(bool nullMachine, bool nullFilename, bool isZipped, int entryCount, bool selectFile)
        {
            TestLoadMedia(nullMachine, FileTypes.Disc, nullFilename, isZipped, entryCount, selectFile, machineViewModel => machineViewModel.DriveBCommand, mediaImage => { return m => m.LoadDisc(1, mediaImage); });
        }

        [TestCase(false, true, false, 0, false)]
        [TestCase(true, false, false, 0, false)]
        [TestCase(false, false, false, 0, false)]
        [TestCase(false, false, true, 2, false)]
        [TestCase(false, false, true, 0, true)]
        [TestCase(false, false, true, 1, true)]
        [TestCase(false, false, true, 2, true)]
        public void LoadTape(bool nullMachine, bool nullFilename, bool isZipped, int entryCount, bool selectFile)
        {
            TestLoadMedia(nullMachine, FileTypes.Tape, nullFilename, isZipped, entryCount, selectFile, machineViewModel => machineViewModel.TapeCommand, mediaImage => { return m => m.LoadTape(mediaImage); });
        }

        [Test]
        public void Reset([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.ResetCommand, null, m => m.Reset());
        }

        [Test]
        public void ToggleRunning([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPausableMachine>(nullMachine, m => m.ToggleRunningCommand, null, m => m.ToggleRunning());
        }

        [Test]
        public void JumpToMostRecentBookmark([Values(false, true)] bool nullMachine)
        {
            TestCommand<IJumpableMachine>(nullMachine, m => m.JumpToMostRecentBookmarkCommand, null, m => m.JumpToMostRecentBookmark());
        }

        [Test]
        public void Compact([Values(false, true)] bool nullMachine)
        {
            TestCommand<ICompactableMachine>(nullMachine, m => m.CompactCommand, null, m => m.Compact(null, false));
        }

        [Test]
        public void SeekToNextBookmark([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPrerecordedMachine>(nullMachine, m => m.SeekToNextBookmarkCommand, null, m => m.SeekToNextBookmark());
        }

        [Test]
        public void SeekToPreviousBookmark([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPrerecordedMachine>(nullMachine, m => m.SeekToPrevBookmarkCommand, null, m => m.SeekToPreviousBookmark());
        }

        [Test]
        public void SeekToStart([Values(false, true)] bool nullMachine)
        {
            TestCommand<IPrerecordedMachine>(nullMachine, m => m.SeekToStartCommand, null, m => m.SeekToStart());
        }

        [Test]
        public void KeyDown([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.KeyDownCommand, Keys.A, m => m.Key(Keys.A, true));
        }

        [Test]
        public void KeyUp([Values(false, true)] bool nullMachine)
        {
            TestCommand<IInteractiveMachine>(nullMachine, m => m.KeyUpCommand, Keys.A, m => m.Key(Keys.A, false));
        }

        //[Test]
        //public void RemoveCommand()
        //{
        //    // Setup
        //    MachineViewModel viewModel = new MachineViewModel(null, null, null, null, null, null, null, null);
        //    Command command = new Command(x => { }, y => true);

        //    // Act
        //    viewModel.RemoveCommand = command;

        //    // Verify
        //    Assert.AreEqual(command, viewModel.RemoveCommand);
        //}
    }
}
