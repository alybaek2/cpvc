using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Input;
using static CPvC.MainViewModel;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MainViewModelTests
    {
        private Mock<ISettings> _mockSettings;
        private Mock<IFileSystem> _mockFileSystem;
        private Mock<ISocket> _mockSocket;
        private string _settingGet;
        private string _remoteServersSetting;

        private MainViewModel _mainViewModel;
        private LocalMachine _machine;

        [SetUp]
        public void Setup()
        {
            _remoteServersSetting = String.Empty;

            _mockSettings = new Mock<ISettings>(MockBehavior.Strict);
            _mockSettings.SetupGet(x => x.RecentlyOpened).Returns(() => _settingGet);
            _mockSettings.SetupSet(x => x.RecentlyOpened = It.IsAny<string>());
            _mockSettings.SetupGet(x => x.RemoteServers).Returns(() => _remoteServersSetting);
            _mockSettings.SetupSet(x => x.RemoteServers = It.IsAny<string>());

            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.FileLength(AnyString())).Returns(100);
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(true);
            _mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);

            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(AnyString())).Returns(mockTextFile);

            _mockSocket = new Mock<ISocket>();

            _machine = LocalMachine.New("test", null, null);
            _mainViewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            _mainViewModel.Model.AddMachine(_machine);

        }

        [TearDown]
        public void Teardown()
        {
            _mockSettings = null;
            _mockFileSystem = null;

            _settingGet = null;
        }

        private MainViewModel SetupViewModel(int machineCount)
        {
            _settingGet = String.Join(",", Enumerable.Range(0, machineCount).Select(x => String.Format("test{0}.cpvc", x)));

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object);

            // Create a Replay machine.
            HistoryEvent historyEvent = null;
            using (LocalMachine machine = CreateTestMachine())
            {
                historyEvent = machine.History.CurrentEvent;
            }

            viewModel.OpenReplayMachine("Test Replay", historyEvent);

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

        static private void TestLoadMedia(
            bool nullMachine,
            FileTypes fileType,
            bool nullFilename,
            bool isZipped,
            int entryCount,
            bool selectFile,
            Func<MainViewModel, ICommand> getCommand,
            Func<byte[], Expression<Action<IInteractiveMachine>>> verifyLoad)
        {
            // Setup
            Mock<IMachine> mockMachine = new Mock<IMachine>();
            Mock<IInteractiveMachine> mockInteractiveMachine = mockMachine.As<IInteractiveMachine>();
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>();
            Mock<ISettings> mockSettings = new Mock<ISettings>();

            string ext = (fileType == FileTypes.Tape) ? "cdt" : "dsk";
            string filename = nullFilename ? null : (isZipped ? "test.zip" : String.Format("test.{0}", ext));

            MainViewModel mainViewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);
            mainViewModel.Model.AddMachine(mockMachine.Object);
            mainViewModel.PromptForFile += (sender, args) =>
            {
                if (args.Existing && args.FileType == fileType)
                {
                    args.Filepath = filename;
                }
            };

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
                        mainViewModel.SelectItem += (sender, args) =>
                        {
                            args.SelectedItem = selectFile ? args.Items[0] : null;
                        };
                    }
                }
            }
            else
            {
                mockFileSystem.Setup(fileSystem => fileSystem.ReadBytes(filename)).Returns(new byte[1] { 0x02 });
            }

            ICommand command = getCommand(mainViewModel);

            // Act
            command.Execute(nullMachine ? null : mockInteractiveMachine.Object);

            // Verify
            if (!nullMachine)
            {
                mockPausableMachine.Verify(m => m.AutoPause());
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

            Assert.AreEqual(!nullMachine, command.CanExecute(nullMachine ? null : mockInteractiveMachine.Object));
        }

        static private void TestInterfacePassthrough<T>(ICommand command, Expression<Action<T>> expr) where T : class
        {
            // Setup
            Mock<T> mockMachine = new Mock<T>(MockBehavior.Strict);
            mockMachine.Setup(expr);

            // Act
            command.Execute(mockMachine.Object);

            // Verify
            mockMachine.Verify(expr, Times.Once());
        }

        private void TestNoInterfacePassthrough<T>(ICommand command, bool canExecute) where T : class
        {
            // Setup
            Mock<T> mockMachine = new Mock<T>(MockBehavior.Strict);

            // Act
            command.Execute(mockMachine.Object);

            // Verify
            mockMachine.VerifyNoOtherCalls();
            Assert.False(command.CanExecute(mockMachine));
        }

        //static private Mock<MainViewModel.PromptForFileDelegate> SetupPrompt(FileTypes fileType, bool existing, string filepath)
        //{
        //    Mock<MainViewModel.PromptForFileDelegate> mockPrompt = new Mock<MainViewModel.PromptForFileDelegate>();
        //    mockPrompt.Setup(x => x(fileType, existing)).Returns(filepath);

        //    return mockPrompt;
        //}

        [Test]
        public void OpenNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0);
            viewModel.PromptForFile += (sender, args) =>
            {
                args.Filepath = null;
            };
            int machineViewModelCount = viewModel.Machines.Count;

            // Act
            viewModel.OpenMachineCommand.Execute(null);

            // Verify
            Assert.AreEqual(machineViewModelCount, viewModel.Machines.Count);
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
        public void OpenInvalid()
        {
            // Setup
            PromptForFileEventHandler mockPrompt = (object sender, PromptForFileEventArgs args) => args.Filepath = "test.cpvc";

            Mock<ISocket> mockSocket = new Mock<ISocket>();
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            viewModel.PromptForFile += mockPrompt;
            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(It.IsAny<string>())).Throws(new Exception());

            // Act and Verify
            Assert.Throws<Exception>(() => viewModel.OpenMachine(_mockFileSystem.Object));
        }

        [Test]
        public void OpenNonExistentFile()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            _settingGet = "test.cpvc";
            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(AnyString())).Throws(new Exception());

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Verify
            Assert.AreEqual(0, viewModel.Machines.Count);
        }

        [Test]
        public void NewMachine()
        {
            // Setup
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object);

            // Act
            viewModel.NewMachine(_mockFileSystem.Object);

            // Verify
            Assert.AreEqual(1, viewModel.Machines.Count);
            Assert.AreEqual(viewModel.Machines[0], viewModel.ActiveMachine);
            Assert.AreEqual(RunningState.Running, viewModel.Machines[0].RunningState);
        }


        /// <summary>
        /// Ensures that if we call MainViewModel.OpenMachine twice with the same filepath, the second call should return the same machine.
        /// </summary>
        [Test]
        public void OpenMachineTwice()
        {
            // Setup
            string filepath = "test.cpvc";
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object);
            viewModel.PromptForFile += (sender, args) =>
            {
                if (args.FileType == FileTypes.Machine && args.Existing)
                {
                    args.Filepath = filepath;
                }
            };

            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(filepath)).Returns(mockTextFile);
            viewModel.OpenMachine(_mockFileSystem.Object);

            // Act
            viewModel.OpenMachine(_mockFileSystem.Object);

            // Verify
            Assert.AreEqual(1, viewModel.Machines.Count);
        }

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(true, "test2")]
        public void RenameMachine(bool active, string newName)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            viewModel.PromptForName += (sender, args) =>
            {
                args.SelectedName = newName;
            };

            IMachine machine = viewModel.Machines[0];
            string oldName = machine.Name;

            // Act
            viewModel.RenameCommand.Execute(active ? machine : null);

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

        //[Test]
        //public void NullActiveMachine()
        //{
        //    // Setup
        //    Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, null);

        //    MainViewModel viewModel = SetupViewModel(1, prompt, null, null);
        //    Machine machine = viewModel.Machines[0];
        //    machine.Open();
        //    viewModel.ActiveMachineViewModel = null;

        //    // Act and Verify
        //    Assert.DoesNotThrow(() =>
        //    {
        //        viewModel.ActiveMachineViewModel.KeyDownCommand.Execute(Keys.A);
        //        viewModel.ActiveMachineViewModel.ResetCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.PauseCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.ResumeCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.DriveACommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.TapeCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.DriveAEjectCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.TapeEjectCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.AddBookmarkCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.JumpToMostRecentBookmarkCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.TurboCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.CompactCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.SeekToNextBookmarkCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.SeekToPrevBookmarkCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.SeekToStartCommand.Execute(null);
        //        viewModel.ActiveMachineViewModel.CloseCommand.Execute(null);
        //    });
        //}

        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SelectBookmark(bool nullMachine, bool selectEvent)
        {
            // Setup
            HistoryEvent historyEvent = null;
            MainViewModel viewModel = SetupViewModel(1);
            _machine.OpenFromFile(_mockFileSystem.Object);

            _machine.AddBookmark(false);
            historyEvent = _machine.History.CurrentEvent;
            viewModel.PromptForBookmark += (sender, args) =>
            {
                args.SelectedBookmark = selectEvent ? historyEvent : null;
            };

            TestHelpers.Run(_machine, 1000);

            _machine.AddBookmark(false);

            // Act
            viewModel.BrowseBookmarksCommand.Execute(nullMachine ? null : _machine);

            // Verify
            if (!nullMachine && selectEvent)
            {
                Assert.AreEqual(historyEvent, _machine.History.CurrentEvent);
            }
            else
            {
                Assert.AreNotEqual(historyEvent, _machine.History.CurrentEvent);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReadAudio(bool active)
        {
            // Setup
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object);

            Mock<IMachine> coreMachine = new Mock<IMachine>();
            Mock<IMachine> coreMachine2 = new Mock<IMachine>();

            viewModel.Model.AddMachine(coreMachine.Object);
            viewModel.Model.AddMachine(coreMachine2.Object);

            viewModel.ActiveMachine = active ? coreMachine.Object : null;

            // Act
            byte[] buffer = new byte[4];
            int samples = viewModel.ReadAudio(buffer, 0, 1);

            // Verify
            if (active)
            {
                coreMachine.Verify(m => m.ReadAudio(buffer, 0, 1));
                coreMachine2.Verify(m => m.AdvancePlayback(1));
            }
            else
            {
                coreMachine.Verify(m => m.AdvancePlayback(1));
                coreMachine2.Verify(m => m.AdvancePlayback(1));
            }
        }

        [Test]
        public void SetActiveMachine()
        {
            // Setup
            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            MainViewModel viewModel = SetupViewModel(1);
            viewModel.PropertyChanged += propChanged.Object;

            // Act
            viewModel.ActiveMachine = _machine;

            // Verify
            Assert.AreEqual(_machine, viewModel.ActiveMachine);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveMachine"), Times.Once);
        }

        [Test]
        public void SetActiveNonMachine()
        {
            // Setup
            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            MainViewModel viewModel = SetupViewModel(0);
            viewModel.PropertyChanged += propChanged.Object;
            object nonMachine = new object();

            // Act
            viewModel.ActiveItem = nonMachine;

            // Verify
            Assert.IsNull(viewModel.ActiveMachine);
            Assert.AreEqual(nonMachine, viewModel.ActiveItem);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveMachine"), Times.Once);
        }

        //[Test]
        //public void Remove()
        //{
        //    // Setup
        //    MainViewModel viewModel = SetupViewModel(1, null, null, null);
        //    MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
        //    (machineViewModel.Machine as IOpenableMachine)?.Open();

        //    // Act
        //    viewModel.Remove(machineViewModel);

        //    // Verify
        //    Assert.IsEmpty(viewModel.Machines);
        //    _mockSettings.VerifySet(x => x.RecentlyOpened = "", Times.Once);
        //}

        //[Test]
        //public void OpenAndCloseMultiple()
        //{
        //    // Setup
        //    MainViewModel viewModel = SetupViewModel(2, null, null, null);
        //    foreach (Machine machine in viewModel.Machines)
        //    {
        //        machine.Open();
        //    }

        //    // Act
        //    viewModel.CloseAll();

        //    // Verify
        //    Assert.AreEqual(2, viewModel.Machines.Count);
        //    Assert.IsTrue(viewModel.Machines[0].RequiresOpen);
        //    Assert.IsTrue(viewModel.Machines[1].RequiresOpen);
        //}

        //[Test]
        //public void ToggleNull()
        //{
        //    // Setup
        //    MainViewModel viewModel = SetupViewModel(0, null, null, null);

        //    // Act and Verify
        //    Assert.DoesNotThrow(() => viewModel.ActiveMachineViewModel.ToggleRunningCommand.Execute(null));
        //}

        ///// <summary>
        ///// Ensures that a Reset call is passed through from the view model to the machine.
        ///// </summary>
        //[TestCase]
        //public void Reset()
        //{
        //    // Setup
        //    Mock<RequestProcessedDelegate> mockAuditor = new Mock<RequestProcessedDelegate>();
        //    MainViewModel viewModel = SetupViewModel(1, null, null, null);
        //    MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
        //    Machine machine = viewModel.Machines[0];

        //    bool resetCalled = false;
        //    bool[] keys = new bool[80];
        //    Array.Clear(keys, 0, keys.Length);
        //    RequestProcessedDelegate auditor = (core, request, action) =>
        //    {
        //        if (core == machine.Core && action != null)
        //        {
        //            if (action.Type == CoreRequest.Types.KeyPress)
        //            {
        //                keys[action.KeyCode] = action.KeyDown;
        //            }
        //            else if (action.Type == CoreRequest.Types.Reset)
        //            {
        //                resetCalled = true;
        //            }
        //        }
        //    };

        //    machine.Open();
        //    machine.Core.Auditors += auditor;
        //    viewModel.ActiveMachineViewModel = machineViewModel;
        //    machine.Start();

        //    // Act
        //    machineViewModel.ResetCommand.Execute(null);
        //    machineViewModel.KeyDownCommand.Execute(Keys.A);
        //    WaitForQueueToProcess(machine.Core);

        //    // Verify
        //    Assert.True(resetCalled);
        //    Times expectedResetTimes = Times.Once();
        //    Times expectedKeyTimes = Times.Once();

        //    for (byte k = 0; k < 80; k++)
        //    {
        //        if (k == Keys.A)
        //        {
        //            Assert.True(keys[k]);
        //        }
        //        else
        //        {
        //            Assert.False(keys[k]);
        //        }
        //    }
        //}


        [Test]
        public void Reset()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.ResetCommand, m => m.Reset());
        }

        [Test]
        public void ResetNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.ResetCommand, false);
        }

        [Test]
        public void DriveAEject()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.DriveAEjectCommand, m => m.LoadDisc(0, null));
        }

        [Test]
        public void DriveAEjectNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.DriveAEjectCommand, false);
        }

        [Test]
        public void DriveBEject()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.DriveBEjectCommand, m => m.LoadDisc(1, null));
        }

        [Test]
        public void DriveBEjectNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.DriveBEjectCommand, false);
        }

        [Test]
        public void TapeEject()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.TapeEjectCommand, m => m.LoadTape(null));
        }

        [Test]
        public void TapeEjectNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.TapeEjectCommand, false);
        }

        [Test]
        public void ToggleRunning()
        {
            TestInterfacePassthrough<IPausableMachine>(_mainViewModel.ToggleRunningCommand, m => m.ToggleRunning());
        }

        [Test]
        public void ToggleRunningNonPausableMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.ToggleRunningCommand, false);
        }

        [Test]
        public void AddBookmark()
        {
            TestInterfacePassthrough<IBookmarkableMachine>(_mainViewModel.AddBookmarkCommand, m => m.AddBookmark(false));
        }

        [Test]
        public void AddBookmarkToNonBookmarkableMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.AddBookmarkCommand, false);
        }

        [Test]
        public void JumpToMostRecentBookmark()
        {
            TestInterfacePassthrough<IJumpableMachine>(_mainViewModel.JumpToMostRecentBookmarkCommand, m => m.JumpToMostRecentBookmark());
        }

        [Test]
        public void JumpToMostRecentBookmarkForNonJumpableMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.JumpToMostRecentBookmarkCommand, false);
        }

        [Test]
        public void KeyPress()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Mock<IInteractiveMachine> mockMachine = new Mock<IInteractiveMachine>();
            mockMachine.Setup(m => m.Key(It.IsAny<byte>(), It.IsAny<bool>()));

            // Act
            viewModel.KeyPress(mockMachine.Object, 42, true);

            // Verify
            mockMachine.Verify(m => m.Key(42, true), Times.Once());
        }

        [Test]
        public void EnableTurbo()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Mock<ITurboableMachine> mockMachine = new Mock<ITurboableMachine>();
            mockMachine.Setup(m => m.EnableTurbo(It.IsAny<bool>()));

            // Act
            viewModel.EnableTurbo(mockMachine.Object, true);

            // Verify
            mockMachine.Verify(m => m.EnableTurbo(true), Times.Once());
        }

        [Test]
        public void SeekToNextBookmark()
        {
            TestInterfacePassthrough<IPrerecordedMachine>(_mainViewModel.SeekToNextBookmarkCommand, m => m.SeekToNextBookmark());
        }

        [Test]
        public void SeekToNextBookmarkForNonPrerecordedMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.SeekToNextBookmarkCommand, false);
        }

        [Test]
        public void SeekToPrevBookmark()
        {
            TestInterfacePassthrough<IPrerecordedMachine>(_mainViewModel.SeekToPrevBookmarkCommand, m => m.SeekToPreviousBookmark());
        }

        [Test]
        public void SeekToPrevBookmarkForNonPrerecordedMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.SeekToPrevBookmarkCommand, false);
        }

        [Test]
        public void SeekToStart()
        {
            TestInterfacePassthrough<IPrerecordedMachine>(_mainViewModel.SeekToStartCommand, m => m.SeekToStart());
        }

        [Test]
        public void SeekToStartForNonPrerecordedMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.SeekToStartCommand, false);
        }

        [Test]
        public void Reverse()
        {
            TestInterfacePassthrough<IReversibleMachine>(_mainViewModel.ReverseStartCommand, m => m.Reverse());
        }

        [Test]
        public void ReverseForNonReversibleMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.ReverseStartCommand, false);
        }

        [Test]
        public void ReverseStop()
        {
            TestInterfacePassthrough<IReversibleMachine>(_mainViewModel.ReverseStopCommand, m => m.ReverseStop());
        }

        [Test]
        public void ToggleReversibility()
        {
            TestInterfacePassthrough<IReversibleMachine>(_mainViewModel.ToggleReversibility, m => m.ToggleReversibilityEnabled());
        }

        [Test]
        public void Compact()
        {
            TestInterfacePassthrough<ICompactableMachine>(_mainViewModel.CompactCommand, m => m.Compact(_mockFileSystem.Object));
        }

        [Test]
        public void Persist()
        {
            // Setup
            Mock<IPersistableMachine> mockMachine = new Mock<IPersistableMachine>(MockBehavior.Strict);
            mockMachine.SetupGet(m => m.PersistantFilepath).Returns(String.Empty);
            mockMachine.Setup(m => m.Persist(_mockFileSystem.Object, "test.cpvc"));
            _mainViewModel.PromptForFile += (sender, args) =>
            {
                args.Filepath = "test.cpvc";
            };

            // Act
            _mainViewModel.PersistCommand.Execute(mockMachine.Object);

            // Verify
            mockMachine.Verify(m => m.Persist(_mockFileSystem.Object, "test.cpvc"), Times.Once());
        }

        [Test]
        public void PersistNonPersistableMachine()
        {
            // Setup
            Mock<IJumpableMachine> mockMachine = new Mock<IJumpableMachine>();
            Mock<ReportErrorEventHandler> mockReport = new Mock<ReportErrorEventHandler>();
            _mainViewModel.ReportError += mockReport.Object;

            // Act
            _mainViewModel.PersistCommand.Execute(mockMachine.Object);

            // Verify
            mockReport.Verify(m => m(It.IsAny<object>(), It.IsAny<ReportErrorEventArgs>()), Times.Once());
        }

        [Test]
        public void PersistAlreadyPersistedMachine()
        {
            // Setup
            Mock<IPersistableMachine> mockMachine = new Mock<IPersistableMachine>();
            mockMachine.SetupGet(m => m.PersistantFilepath).Returns("test.cpvc");
            Mock<PromptForFileEventHandler> mockPrompt = new Mock<PromptForFileEventHandler>();
            _mainViewModel.PromptForFile += mockPrompt.Object;

            // Act
            _mainViewModel.PersistCommand.Execute(mockMachine.Object);

            // Verify
            mockPrompt.VerifyNoOtherCalls();
        }

        [Test]
        public void Pause()
        {
            TestInterfacePassthrough<IPausableMachine>(_mainViewModel.PauseCommand, m => m.Stop());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanPause(bool canStop)
        {
            // Setup
            Mock<IPausableMachine> mockMachine = new Mock<IPausableMachine>();
            mockMachine.SetupGet(m => m.CanStop).Returns(canStop);

            // Verify
            Assert.AreEqual(canStop, _mainViewModel.PauseCommand.CanExecute(mockMachine.Object));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CanResume(bool canStart)
        {
            // Setup
            Mock<IPausableMachine> mockMachine = new Mock<IPausableMachine>();
            mockMachine.SetupGet(m => m.CanStart).Returns(canStart);

            // Verify
            Assert.AreEqual(canStart, _mainViewModel.ResumeCommand.CanExecute(mockMachine.Object));
        }

        [Test]
        public void CanPauseNonPausableMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.PauseCommand, false);
        }


        [Test]
        public void CanResumeNonPausableMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.ResumeCommand, false);
        }

        [Test]
        public void Resume()
        {
            TestInterfacePassthrough<IPausableMachine>(_mainViewModel.ResumeCommand, m => m.Start());
        }

        [Test]
        public void Remove()
        {
            // Setup
            _mainViewModel.ConfirmClose += (sender, args) =>
            {
                args.Result = true;
            };
            int machineCount = _mainViewModel.Machines.Count;
            _mainViewModel.NewMachineCommand.Execute(null);
            IMachine machine = _mainViewModel.Machines[0];

            // Act
            _mainViewModel.RemoveCommand.Execute(machine);

            // Verify
            Assert.AreEqual(machineCount, _mainViewModel.Machines.Count);
        }

        [TestCase(false, false, true, false, true)]
        [TestCase(false, true, false, false, true)]
        [TestCase(true, false, false, false, false)]
        [TestCase(true, false, false, true, true)]
        public void CloseAll(bool newMachine, bool persistedMachine, bool nonPersistableMachine, bool confirmClose, bool expectedResult)
        {
            // Setup
            MainViewModel mainViewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            if (newMachine)
            {
                mainViewModel.NewMachine(_mockFileSystem.Object);
            }

            if (persistedMachine)
            {
                mainViewModel.PromptForFile += (sender, args) =>
                {
                    args.Filepath = "test.cpvc";
                };

                MockTextFile mockTextFile = new MockTextFile();
                mockTextFile.WriteLine("name:Test");
                _mockFileSystem.Setup(fs => fs.OpenTextFile("test.cpvc")).Returns(mockTextFile);

                mainViewModel.OpenMachine(_mockFileSystem.Object);
            }

            if (nonPersistableMachine)
            {
                MachineHistory history = new MachineHistory();

                mainViewModel.OpenReplayMachine("Test (Replay)", history.RootEvent);
            }

            mainViewModel.ConfirmClose += (sender, args) =>
            {
                args.Result = confirmClose;
            };

            // Act
            bool result = mainViewModel.CloseAll();

            // Verify
            Assert.AreEqual(expectedResult, result);
        }

        [Test]
        public void Close()
        {
            TestInterfacePassthrough<IMachine>(_mainViewModel.CloseCommand, m => m.Close());
        }

        [Test]
        public void CloseAndCancel()
        {
            // Setup
            int machineCount = _mainViewModel.Machines.Count;
            _mainViewModel.ConfirmClose += (sender, args) =>
            {
                args.Result = false;
            };

            // Act
            _mainViewModel.CloseCommand.Execute(_machine);

            // Verify
            Assert.True(_machine.IsOpen);
            Assert.AreEqual(machineCount, _mainViewModel.Machines.Count);
        }

        [Test]
        public void CloseNoConfirmHandler()
        {
            // Act
            int machineCount = _mainViewModel.Machines.Count;
            _mainViewModel.CloseCommand.Execute(_machine);

            // Verify
            Assert.True(_machine.IsOpen);
            Assert.AreEqual(machineCount, _mainViewModel.Machines.Count);
        }

        [Test]
        public void ReverseStopForNonReversibleMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.ReverseStopCommand, false);
        }

        [Test]
        public void ToggleReversibilityForNonReversibleMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.ReverseStopCommand, false);
        }

        [Test]
        public void OpenReplayMachine()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);

            // Verify
            IEnumerable<ReplayMachine> replayMachines = viewModel.Machines.Where(m => m is ReplayMachine).Select(m => m as ReplayMachine);
            Assert.AreEqual(1, replayMachines.Count());
            Assert.AreEqual("Test Replay", replayMachines.ElementAt(0).Name);
        }

        [Test]
        public void StartServerSelectCancel()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            viewModel.SelectServerPort += (object o, SelectServerPortEventArgs args) => args.SelectedPort = null;

            // Act
            viewModel.StartServerCommand.Execute(null);

            // Verify
            _mockSocket.VerifyNoOtherCalls();
        }

        [TestCase(6128)]
        [TestCase(9999)]
        public void StartServerSelectOk(int port)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            viewModel.SelectServerPort += (sender, args) => args.SelectedPort = (ushort)port;
            viewModel.CreateSocket += (sender, args) => args.CreatedSocket = _mockSocket.Object;

            // Act
            viewModel.StartServerCommand.Execute(null);

            // Verify
            _mockSocket.Verify(s => s.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, (ushort)port)), Times.Once());
            _mockSocket.Verify(s => s.Listen(1), Times.Once());
            _mockSocket.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null), Times.Once());
        }

        [Test]
        public void StopServer()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            viewModel.SelectServerPort += (sender, args) => args.SelectedPort = 6128;
            viewModel.CreateSocket += (sender, args) => args.CreatedSocket = _mockSocket.Object;
            viewModel.StartServerCommand.Execute(null);

            // Act
            viewModel.StopServerCommand.Execute(null);

            // Verify
            _mockSocket.Verify(s => s.Close(), Times.Once());
        }

        [Test]
        public void Connect()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Mock<IRemote> mockRemote = new Mock<IRemote>();
            RemoteMachine machine = new RemoteMachine(mockRemote.Object);
            viewModel.SelectRemoteMachine += (sender, e) =>
            {
                e.SelectedMachine = machine;
                viewModel.RecentServers.Add(new ServerInfo("localhost", 6128));
            };

            // Act
            viewModel.ConnectCommand.Execute(null);

            // Verify
            Assert.AreEqual(machine, viewModel.ActiveMachine);
            _mockSettings.VerifySet(s => s.RemoteServers = "localhost:6128");
        }

        [Test]
        public void EmptyRemoteServers()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Mock<IRemote> mockRemote = new Mock<IRemote>();
            RemoteMachine machine = new RemoteMachine(mockRemote.Object);
            viewModel.SelectRemoteMachine += (sender, e) => { e.SelectedMachine = machine; };

            // Act
            viewModel.ConnectCommand.Execute(null);

            // Verify
            Assert.AreEqual(machine, viewModel.ActiveMachine);
            _mockSettings.VerifySet(s => s.RemoteServers = "");
        }

        [Test]
        public void ConnectCancel()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1);
            Mock<IRemote> mockRemote = new Mock<IRemote>();
            viewModel.SelectRemoteMachine += (sender, e) => { e.SelectedMachine = null; };

            // Act
            viewModel.ConnectCommand.Execute(null);

            // Verify
            Assert.Null(viewModel.ActiveMachine as RemoteMachine);
        }

        [Test]
        public void LoadRemoteServer()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            _remoteServersSetting = "localhost:6128";

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Verify
            Assert.AreEqual(1, viewModel.RecentServers.Count);
            Assert.AreEqual("localhost", viewModel.RecentServers[0].ServerName);
            Assert.AreEqual(6128, viewModel.RecentServers[0].Port);
        }

        [Test]
        public void LoadRemoteServers()
        {
            // Setup
            _remoteServersSetting = "localhost:6128;host2:3333";

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Verify
            Assert.AreEqual(2, viewModel.RecentServers.Count);
            Assert.AreEqual("localhost", viewModel.RecentServers[0].ServerName);
            Assert.AreEqual(6128, viewModel.RecentServers[0].Port);
            Assert.AreEqual("host2", viewModel.RecentServers[1].ServerName);
            Assert.AreEqual(3333, viewModel.RecentServers[1].Port);
        }

        [Test]
        public void LoadNullRemoteServers()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            _remoteServersSetting = null;

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(viewModel.RecentServers);
        }

        [Test]
        public void OpenPersistableMachine()
        {
            // Setup
            Mock<IPersistableMachine> mockPersistableMachine = new Mock<IPersistableMachine>();
            mockPersistableMachine.Setup(x => x.OpenFromFile(It.IsAny<IFileSystem>()));

            // Act
            _mainViewModel.OpenCommand.Execute(mockPersistableMachine.Object);

            // Verify
            mockPersistableMachine.Verify(m => m.OpenFromFile(_mockFileSystem.Object), Times.Once());
            Assert.True(_mainViewModel.OpenCommand.CanExecute(mockPersistableMachine.Object));
        }

        [Test]
        public void OpenNonPersistableMachine()
        {
            // Setup
            Mock<IMachine> mockCoreMachine = new Mock<IMachine>();

            // Act
            _mainViewModel.OpenCommand.Execute(mockCoreMachine.Object);

            // Verify
            mockCoreMachine.VerifyNoOtherCalls();
            Assert.False(_mainViewModel.OpenCommand.CanExecute(mockCoreMachine.Object));
        }

        [Test]
        public void OpenNullPersistableMachine()
        {
            // Act and Verify
            Assert.DoesNotThrow(() => _mainViewModel.OpenCommand.Execute(null));
            Assert.False(_mainViewModel.OpenCommand.CanExecute(null));
        }
    }
}
