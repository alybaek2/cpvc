using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
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
        private Mock<SelectRemoteMachineDelegate> _mockSelectRemoveMachine;
        private Mock<SelectServerPortDelegate> _mockSelectServerPort;
        private string _settingGet;
        private string _remoteServersSetting;

        private MockFileByteStream _mockBinaryWriter;

        private MainViewModel _mainViewModel;
        private MachineViewModel _machineViewModel;
        private Machine _machine;

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

            _mockBinaryWriter = new MockFileByteStream();

            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream(AnyString())).Returns(_mockBinaryWriter.Object);

            _mockSocket = new Mock<ISocket>();
            _mockSelectServerPort = new Mock<SelectServerPortDelegate>();
            _mockSelectRemoveMachine = new Mock<SelectRemoteMachineDelegate>();

            _machine = Machine.Create("test", null);
            _machineViewModel = new MachineViewModel(_machine, null, null, null, null, null, null, null);
            _mainViewModel = new MainViewModel(null, null, null, null, null, null, null, null, null, null, null);
            _mainViewModel.MachineViewModels.Add(_machineViewModel);
        }

        [TearDown]
        public void Teardown()
        {
            _mockSettings = null;
            _mockFileSystem = null;

            _settingGet = null;
        }

        private MainViewModel SetupViewModel(int machineCount, Mock<MainViewModel.PromptForFileDelegate> mockPromptForFile, Mock<MainViewModel.PromptForBookmarkDelegate> mockPromptForBookmark, Mock<MainViewModel.PromptForNameDelegate> mockPromptForName)
        {
            _settingGet = String.Join(",", Enumerable.Range(0, machineCount).Select(x => String.Format("test{0}.cpvc", x)));
            _mockBinaryWriter.Content = new List<byte>
            {
                0x01,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00
            };

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object, null, mockPromptForFile?.Object, mockPromptForBookmark?.Object, mockPromptForName?.Object, null, _mockSelectRemoveMachine.Object, _mockSelectServerPort.Object, () => _mockSocket.Object, null);

            // Create a Replay machine.
            HistoryEvent historyEvent = null;
            using (Machine machine = CreateTestMachine())
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

        static private Mock<MainViewModel.PromptForFileDelegate> SetupPrompt(FileTypes fileType, bool existing, string filepath)
        {
            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = new Mock<MainViewModel.PromptForFileDelegate>();
            mockPrompt.Setup(x => x(fileType, existing)).Returns(filepath);

            return mockPrompt;
        }

        [Test]
        public void OpenNull()
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, null);
            MainViewModel viewModel = SetupViewModel(0, prompt, null, null);
            int machineViewModelCount = viewModel.MachineViewModels.Count;

            // Act
            viewModel.OpenMachineCommand.Execute(null);

            // Verify
            Assert.AreEqual(machineViewModelCount, viewModel.MachineViewModels.Count);
        }

        [Test]
        public void OpenInvalid()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null, null, null, null, () => mockSocket.Object, null);
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, "test.cpvc");
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream(AnyString())).Throws(new Exception());

            // Act and Verify
            Assert.Throws<Exception>(() => viewModel.OpenMachine(prompt.Object, "test.cpvc", _mockFileSystem.Object));
        }

        [Test]
        public void OpenNonExistentFile()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            _settingGet = "test.cpvc";
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream(AnyString())).Throws(new Exception());

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null, null, null, null, () => mockSocket.Object, null);

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

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(true, "test2")]
        public void RenameMachine(bool active, string newName)
        {
            // Setup
            Mock<MainViewModel.PromptForNameDelegate> mockNamePrompt = new Mock<MainViewModel.PromptForNameDelegate>(MockBehavior.Loose);
            MainViewModel viewModel = SetupViewModel(1, null, null, mockNamePrompt);
            ICoreMachine machine = viewModel.Machines[0];
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            string oldName = machine.Name;
            mockNamePrompt.Setup(x => x(oldName)).Returns(newName);

            if (active)
            {
                viewModel.ActiveMachineViewModel = machineViewModel;
            }
            else
            {
                viewModel.ActiveMachineViewModel = null;
            }

            // Act
            viewModel.ActiveMachineViewModel.RenameCommand.Execute(null);

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

        [TestCase(false)]
        [TestCase(true)]
        public void ReadAudio(bool active)
        {
            // Setup
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object, null, null, null, null, null, _mockSelectRemoveMachine.Object, _mockSelectServerPort.Object, () => _mockSocket.Object, null);

            Mock<ICoreMachine> coreMachine = new Mock<ICoreMachine>();
            MachineViewModel machineViewModel = new MachineViewModel(coreMachine.Object, null, null, null, null, null, null, null);

            Mock<ICoreMachine> coreMachine2 = new Mock<ICoreMachine>();
            MachineViewModel machineViewModel2 = new MachineViewModel(coreMachine2.Object, null, null, null, null, null, null, null);

            viewModel.MachineViewModels.Add(machineViewModel);
            viewModel.MachineViewModels.Add(machineViewModel2);
            viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

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

        [Test]
        public void SetActiveNonMachine()
        {
            // Setup
            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            MainViewModel viewModel = SetupViewModel(0, null, null, null);
            viewModel.PropertyChanged += propChanged.Object;
            object nonMachine = new object();

            // Act
            viewModel.ActiveItem = nonMachine;

            // Verify
            Assert.IsNull(viewModel.ActiveMachineViewModel.Machine);
            Assert.AreEqual(nonMachine, viewModel.ActiveItem);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveMachineViewModel"), Times.Once);
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

        [Test]
        public void ToggleNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0, null, null, null);

            // Act and Verify
            Assert.DoesNotThrow(() => viewModel.ActiveMachineViewModel.ToggleRunningCommand.Execute(null));
        }

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

        /// <summary>
        /// Ensures that a AddBookmark call is passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void AddBookmark(bool active)
        {
            // Setup
            //Machine machine = Machine.Create("test", null);
            //MachineViewModel machineViewModel = new MachineViewModel(null, null, machine, null, null, null, null, null);
            //MainViewModel mainViewModel = new MainViewModel(null, null, null, null, null, null, null, null, null, null, null);
            //mainViewModel.MachineViewModels.Add(machineViewModel);

            _mainViewModel.ActiveMachineViewModel = active ? _machineViewModel : null;

            // Act
            _mainViewModel.ActiveMachineViewModel.AddBookmarkCommand.Execute(null);

            // Verify
            if (active)
            {
                Assert.IsNotNull(_machine.History.CurrentEvent.Bookmark);
            }
            else
            {
                Assert.IsNull(_machine.History.CurrentEvent.Bookmark);
            }
        }

        [Test]
        public void OpenReplayMachine()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);

            // Verify
            IEnumerable<MachineViewModel> replayMachines = viewModel.MachineViewModels.Where(m => m.Machine is ReplayMachine);
            Assert.AreEqual(1, replayMachines.Count());
            Assert.AreEqual("Test Replay", replayMachines.ElementAt(0).Machine.Name);
        }

        [Test]
        public void StartServerSelectCancel()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            _mockSelectServerPort.Setup(s => s(It.IsAny<ushort>())).Returns(() => null);

            // Act
            viewModel.StartServerCommand.Execute(null);

            // Verify
            _mockSelectServerPort.Verify(s => s(6128), Times.Once());
            _mockSocket.VerifyNoOtherCalls();
        }

        [TestCase(6128)]
        [TestCase(9999)]
        public void StartServerSelectOk(int port)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            _mockSelectServerPort.Setup(s => s(It.IsAny<ushort>())).Returns(() => (ushort)port);

            // Act
            viewModel.StartServerCommand.Execute(null);

            // Verify
            _mockSelectServerPort.Verify(s => s(6128), Times.Once());
            _mockSocket.Verify(s => s.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Any, (ushort)port)), Times.Once());
            _mockSocket.Verify(s => s.Listen(1), Times.Once());
            _mockSocket.Verify(s => s.BeginAccept(It.IsAny<AsyncCallback>(), null), Times.Once());
        }

        [Test]
        public void StopServer()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            _mockSelectServerPort.Setup(s => s(It.IsAny<ushort>())).Returns(() => 6128);
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
            _mockSelectRemoveMachine.Setup(s => s(It.IsAny<ServerInfo>())).Returns(() => machine).Callback<ServerInfo>(s => viewModel.RecentServers.Add(new ServerInfo("localhost", 6128)));

            // Act
            viewModel.ConnectCommand.Execute(null);

            // Verify
            _mockSelectRemoveMachine.Verify(s => s(It.IsAny<ServerInfo>()), Times.Once());
            Assert.AreEqual(machine, viewModel.ActiveMachineViewModel.Machine);
            _mockSettings.VerifySet(s => s.RemoteServers = "localhost:6128");
        }

        [Test]
        public void EmptyRemoteServers()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Mock<IRemote> mockRemote = new Mock<IRemote>();
            RemoteMachine machine = new RemoteMachine(mockRemote.Object);
            _mockSelectRemoveMachine.Setup(s => s(It.IsAny<ServerInfo>())).Returns(() => machine);

            // Act
            viewModel.ConnectCommand.Execute(null);

            // Verify
            _mockSelectRemoveMachine.Verify(s => s(It.IsAny<ServerInfo>()), Times.Once());
            Assert.AreEqual(machine, viewModel.ActiveMachineViewModel.Machine);
            _mockSettings.VerifySet(s => s.RemoteServers = "");
        }

        [Test]
        public void ConnectCancel()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Mock<IRemote> mockRemote = new Mock<IRemote>();
            RemoteMachine machine = new RemoteMachine(mockRemote.Object);
            _mockSelectRemoveMachine.Setup(s => s(It.IsAny<ServerInfo>())).Returns(() => null);

            // Act
            viewModel.ConnectCommand.Execute(null);

            // Verify
            _mockSelectRemoveMachine.Verify(s => s(It.IsAny<ServerInfo>()), Times.Once());
            Assert.AreNotEqual(machine, viewModel.ActiveMachineViewModel.Machine);
        }

        [Test]
        public void LoadRemoteServer()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            _remoteServersSetting = "localhost:6128";

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null, null, null, null, () => mockSocket.Object, null);

            // Verify
            Assert.AreEqual(1, viewModel.RecentServers.Count);
            Assert.AreEqual("localhost", viewModel.RecentServers[0].ServerName);
            Assert.AreEqual(6128, viewModel.RecentServers[0].Port);
        }

        [Test]
        public void LoadRemoteServers()
        {
            // Setup
            Mock<ISocket> mockSocket = new Mock<ISocket>();
            _remoteServersSetting = "localhost:6128;host2:3333";

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null, null, null, null, () => mockSocket.Object, null);

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
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null, null, null, null, () => mockSocket.Object, null);

            // Verify
            Assert.IsNull(viewModel.RecentServers);
        }
    }
}
