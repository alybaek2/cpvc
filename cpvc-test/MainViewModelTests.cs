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
            _mainViewModel = new MainViewModel(null, _mockFileSystem.Object);

        }

        [TearDown]
        public void Teardown()
        {
            _mockSettings = null;
            _mockFileSystem = null;

            _settingGet = null;
        }

        private MainViewModel SetupViewModel(int machineCount, PromptForFileEventHandler mockPromptForFile, PromptForBookmarkEventHandler mockPromptForBookmark, PromptForNameEventHandler mockPromptForName)
        {
            _settingGet = String.Join(",", Enumerable.Range(0, machineCount).Select(x => String.Format("test{0}.cpvc", x)));

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object);
            viewModel.PromptForFile += mockPromptForFile;
            viewModel.PromptForBookmark += mockPromptForBookmark;
            viewModel.PromptForName += mockPromptForName;

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

        private void TestInterfacePassthrough<T>(ICommand command, Expression<Action<T>> expr) where T : class
        {
            // Setup
            Mock<T> mockMachine = new Mock<T>(MockBehavior.Strict);
            mockMachine.Setup(expr);

            // Act
            command.Execute(mockMachine.Object);

            // Verify
            mockMachine.Verify(expr, Times.Once());
        }

        private void TestNoInterfacePassthrough<T>(ICommand command) where T : class
        {
            // Setup
            Mock<T> mockMachine = new Mock<T>(MockBehavior.Strict);

            // Act
            command.Execute(mockMachine.Object);

            // Verify
            mockMachine.VerifyNoOtherCalls();
        }

        //static private Mock<MainViewModel.PromptForFileDelegate> SetupPrompt(FileTypes fileType, bool existing, string filepath)
        //{
        //    Mock<MainViewModel.PromptForFileDelegate> mockPrompt = new Mock<MainViewModel.PromptForFileDelegate>();
        //    mockPrompt.Setup(x => x(fileType, existing)).Returns(filepath);

        //    return mockPrompt;
        //}

        //[Test]
        //public void OpenNull()
        //{
        //    // Setup
        //    Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, null);
        //    MainViewModel viewModel = SetupViewModel(0, prompt, null, null);
        //    int machineViewModelCount = viewModel.MachineViewModels.Count;

        //    // Act
        //    viewModel.OpenMachineCommand.Execute(null);

        //    // Verify
        //    Assert.AreEqual(machineViewModelCount, viewModel.MachineViewModels.Count);
        //}

        [Test]
        public void OpenInvalid()
        {
            // Setup
            PromptForFileEventHandler mockPrompt = (object sender, PromptForFileEventArgs args) => args.Filepath = "test.cpvc";

            Mock<ISocket> mockSocket = new Mock<ISocket>();
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            viewModel.PromptForFile += mockPrompt;
            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile("test.cpvc")).Throws(new Exception());

            // Act and Verify
            Assert.Throws<Exception>(() => viewModel.OpenMachine("test.cpvc", _mockFileSystem.Object));
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

        /// <summary>
        /// Ensures that if we call MainViewModel.OpenMachine twice with the same filepath, the second call should return the same machine.
        /// </summary>
        //[Test]
        //public void OpenMachineTwice()
        //{
        //    // Setup
        //    string filepath = "test.cpvc";
        //    Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);
        //    MainViewModel viewModel = SetupViewModel(0, prompt, null, null);
        //    _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(false);
        //    viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

        //    // Act
        //    viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

        //    // Verify
        //    Assert.AreEqual(1, viewModel.MachinePreviews.Count);
        //}

        //[TestCase(false, null)]
        //[TestCase(true, null)]
        //[TestCase(true, "test2")]
        //public void RenameMachine(bool active, string newName)
        //{
        //    // Setup
        //    Mock<MainViewModel.PromptForNameDelegate> mockNamePrompt = new Mock<MainViewModel.PromptForNameDelegate>(MockBehavior.Loose);
        //    MainViewModel viewModel = SetupViewModel(1, null, null, mockNamePrompt);
        //    ICoreMachine machine = viewModel.Machines[0];
        //    MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
        //    string oldName = machine.Name;
        //    mockNamePrompt.Setup(x => x(oldName)).Returns(newName);

        //    if (active)
        //    {
        //        viewModel.ActiveMachineViewModel = machineViewModel;
        //    }
        //    else
        //    {
        //        viewModel.ActiveMachineViewModel = null;
        //    }

        //    // Act
        //    viewModel.ActiveMachineViewModel.RenameCommand.Execute(null);

        //    // Verify
        //    if (active && newName != null)
        //    {
        //        Assert.AreEqual(newName, machine.Name);
        //    }
        //    else
        //    {
        //        Assert.AreEqual(oldName, machine.Name);
        //    }
        //}

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

        //[TestCase(false, true)]
        //[TestCase(true, false)]
        //[TestCase(true, true)]
        //public void SelectBookmark(bool active, bool selectEvent)
        //{
        //    // Setup
        //    MachineHistory history = new MachineHistory();

        //    HistoryEvent event2 = null;
        //    Mock<MainViewModel.PromptForBookmarkDelegate> prompt = new Mock<MainViewModel.PromptForBookmarkDelegate>(MockBehavior.Strict);
        //    MainViewModel viewModel = SetupViewModel(1, null, prompt, null);
        //    MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
        //    Machine machine = machineViewModel.Machine as Machine;
        //    machine.Open();

        //    machine.AddBookmark(false);
        //    event2 = machine.History.CurrentEvent;
        //    prompt.Setup(p => p()).Returns(selectEvent ? event2 : null);
        //    TestHelpers.Run(machine, 1000);

        //    machine.AddBookmark(false);

        //    viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

        //    // Act
        //    viewModel.ActiveMachineViewModel.BrowseBookmarksCommand.Execute(null);

        //    // Verify
        //    if (active)
        //    {
        //        prompt.Verify(p => p(), Times.Once());
        //        if (selectEvent)
        //        {
        //            Assert.AreEqual(event2, machine.History.CurrentEvent);
        //        }
        //        else
        //        {
        //            Assert.AreNotEqual(event2, machine.History.CurrentEvent);
        //        }
        //    }
        //    else
        //    {
        //        prompt.Verify(p => p(), Times.Never());
        //        Assert.AreNotEqual(event2, machine.History.CurrentEvent);
        //    }
        //}

        //[TestCase(false)]
        //[TestCase(true)]
        //public void ReadAudio(bool active)
        //{
        //    // Setup
        //    MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object, null, null, null, null, null, _mockSelectRemoveMachine.Object, _mockSelectServerPort.Object, () => _mockSocket.Object, null);

        //    Mock<ICoreMachine> coreMachine = new Mock<ICoreMachine>();
        //    MachineViewModel machineViewModel = new MachineViewModel(coreMachine.Object, null, null, null, null, null, null, null);

        //    Mock<ICoreMachine> coreMachine2 = new Mock<ICoreMachine>();
        //    MachineViewModel machineViewModel2 = new MachineViewModel(coreMachine2.Object, null, null, null, null, null, null, null);

        //    viewModel.MachineViewModels.Add(machineViewModel);
        //    viewModel.MachineViewModels.Add(machineViewModel2);
        //    viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

        //    // Act
        //    byte[] buffer = new byte[4];
        //    int samples = viewModel.ReadAudio(buffer, 0, 1);

        //    // Verify
        //    if (active)
        //    {
        //        coreMachine.Verify(m => m.ReadAudio(buffer, 0, 1));
        //        coreMachine2.Verify(m => m.AdvancePlayback(1));
        //    }
        //    else
        //    {
        //        coreMachine.Verify(m => m.AdvancePlayback(1));
        //        coreMachine2.Verify(m => m.AdvancePlayback(1));
        //    }
        //}

        //[TestCase(false)]
        //[TestCase(true)]
        //public void SetActiveMachine(bool closed)
        //{
        //    // Setup
        //    Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
        //    MainViewModel viewModel = SetupViewModel(1, null, null, null);
        //    viewModel.PropertyChanged += propChanged.Object;
        //    MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
        //    Machine machine = machineViewModel.Machine as Machine;
        //    if (closed)
        //    {
        //        machineViewModel.CloseCommand.Execute(null);
        //    }
        //    else
        //    {
        //        machineViewModel.OpenCommand.Execute(null);
        //    }

        //    // Act
        //    viewModel.ActiveMachineViewModel = machineViewModel;

        //    // Verify
        //    Assert.IsFalse(machine.RequiresOpen);
        //    Assert.AreEqual(machineViewModel, viewModel.ActiveMachineViewModel);
        //    propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
        //    propChanged.Verify(PropertyChanged(viewModel, "ActiveMachineViewModel"), Times.Once);
        //}

        //[Test]
        //public void SetActiveNonMachine()
        //{
        //    // Setup
        //    Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
        //    MainViewModel viewModel = SetupViewModel(0, null, null, null);
        //    viewModel.PropertyChanged += propChanged.Object;
        //    object nonMachine = new object();

        //    // Act
        //    viewModel.ActiveItem = nonMachine;

        //    // Verify
        //    Assert.IsNull(viewModel.ActiveMachineViewModel.Machine);
        //    Assert.AreEqual(nonMachine, viewModel.ActiveItem);
        //    propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
        //    propChanged.Verify(PropertyChanged(viewModel, "ActiveMachineViewModel"), Times.Once);
        //}

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
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.ResetCommand);
        }

        [Test]
        public void DriveAEject()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.DriveAEjectCommand, m => m.LoadDisc(0, null));
        }

        [Test]
        public void DriveAEjectNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.DriveAEjectCommand);
        }

        [Test]
        public void DriveBEject()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.DriveBEjectCommand, m => m.LoadDisc(1, null));
        }

        [Test]
        public void DriveBEjectNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.DriveBEjectCommand);
        }

        [Test]
        public void TapeEject()
        {
            TestInterfacePassthrough<IInteractiveMachine>(_mainViewModel.TapeEjectCommand, m => m.LoadTape(null));
        }

        [Test]
        public void TapeEjectNonInteractiveMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.TapeEjectCommand);
        }

        [Test]
        public void ToggleRunning()
        {
            TestInterfacePassthrough<IPausableMachine>(_mainViewModel.ToggleRunningCommand, m => m.ToggleRunning());
        }

        [Test]
        public void ToggleRunningNonPausableMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.ToggleRunningCommand);
        }

        [Test]
        public void AddBookmark()
        {
            TestInterfacePassthrough<IBookmarkableMachine>(_mainViewModel.AddBookmarkCommand, m => m.AddBookmark(false));
        }

        [Test]
        public void AddBookmarkToNonBookmarkableMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.AddBookmarkCommand);
        }

        [Test]
        public void JumpToMostRecentBookmark()
        {
            TestInterfacePassthrough<IJumpableMachine>(_mainViewModel.JumpToMostRecentBookmarkCommand, m => m.JumpToMostRecentBookmark());
        }

        [Test]
        public void JumpToMostRecentBookmarkForNonJumpableMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.JumpToMostRecentBookmarkCommand);
        }

        [Test]
        public void SeekToNextBookmark()
        {
            TestInterfacePassthrough<IPrerecordedMachine>(_mainViewModel.SeekToNextBookmarkCommand, m => m.SeekToNextBookmark());
        }

        [Test]
        public void SeekToNextBookmarkForNonPrerecordedMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.SeekToNextBookmarkCommand);
        }

        [Test]
        public void SeekToPrevBookmark()
        {
            TestInterfacePassthrough<IPrerecordedMachine>(_mainViewModel.SeekToPrevBookmarkCommand, m => m.SeekToPreviousBookmark());
        }

        [Test]
        public void SeekToPrevBookmarkForNonPrerecordedMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.SeekToPrevBookmarkCommand);
        }

        [Test]
        public void SeekToStart()
        {
            TestInterfacePassthrough<IPrerecordedMachine>(_mainViewModel.SeekToStartCommand, m => m.SeekToStart());
        }

        [Test]
        public void SeekToStartForNonPrerecordedMachine()
        {
            TestNoInterfacePassthrough<IJumpableMachine>(_mainViewModel.SeekToStartCommand);
        }

        [Test]
        public void Reverse()
        {
            TestInterfacePassthrough<IReversibleMachine>(_mainViewModel.ReverseStartCommand, m => m.Reverse());
        }

        [Test]
        public void ReverseForNonReversibleMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.ReverseStartCommand);
        }

        [Test]
        public void ReverseStop()
        {
            TestInterfacePassthrough<IReversibleMachine>(_mainViewModel.ReverseStopCommand, m => m.ReverseStop());
        }

        [Test]
        public void ReverseStopForNonReversibleMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.ReverseStopCommand);
        }

        [Test]
        public void ToggleReversibility()
        {
            TestInterfacePassthrough<IReversibleMachine>(_mainViewModel.ReverseStopCommand, m => m.ReverseStop());
        }

        [Test]
        public void ToggleReversibilityForNonReversibleMachine()
        {
            TestNoInterfacePassthrough<ITurboableMachine>(_mainViewModel.ReverseStopCommand);
        }

        [Test]
        public void OpenReplayMachine()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);

            // Verify
            IEnumerable<ReplayMachine> replayMachines = viewModel.Machines.Where(m => m is ReplayMachine).Select(m => m as ReplayMachine);
            Assert.AreEqual(1, replayMachines.Count());
            Assert.AreEqual("Test Replay", replayMachines.ElementAt(0).Name);
        }

        [Test]
        public void StartServerSelectCancel()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
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
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
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
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
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
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
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
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
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
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
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
