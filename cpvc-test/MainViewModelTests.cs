using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MainViewModelTests
    {
        private Mock<ISettings> _mockSettings;
        private Mock<IFileSystem> _mockFileSystem;

        private string _settingGet;

        private MockFileByteStream _mockBinaryWriter;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISettings>(MockBehavior.Strict);
            _mockSettings.SetupGet(x => x.RecentlyOpened).Returns(() => _settingGet);
            _mockSettings.SetupSet(x => x.RecentlyOpened = It.IsAny<string>());

            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.FileLength(AnyString())).Returns(100);
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(true);
            _mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);

            _mockBinaryWriter = new MockFileByteStream();

            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream(AnyString())).Returns(_mockBinaryWriter.Object);
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
            _settingGet = String.Join(",", Enumerable.Range(0, machineCount).Select(x => String.Format("Test{0};test{0}.cpvc", x)));
            _mockBinaryWriter.Content = new List<byte>
            {
                0x05,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00
            };

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem?.Object, null, mockPromptForFile?.Object, mockPromptForBookmark?.Object, mockPromptForName?.Object);

            // Create a Replay machine.
            HistoryEvent historyEvent = null;
            using (Machine machine = CreateTestMachine())
            {
                historyEvent = machine.CurrentEvent;
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
        public void ThrowsWhenNewMachineFails()
        {
            // Setup
            string filepath = "test.cpvc";
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream(filepath)).Throws(new Exception("File not found"));
            mockFileSystem.Setup(ReadBytes()).Throws(new Exception("File missing"));
            mockFileSystem.Setup(DeleteFile(filepath));
            mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(false);

            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act and Verify
            Exception ex = Assert.Throws<Exception>(() =>
            {
                MainViewModel viewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object, null, null, null, null);
                viewModel.NewMachine(prompt.Object, mockFileSystem.Object);
            });
            Assert.AreEqual(ex.Message, "File not found");
        }

        [Test]
        public void NewNull()
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, null);
            MainViewModel viewModel = SetupViewModel(0, prompt, null, null);

            // Act
            Machine machine = viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(machine);
        }

        [Test]
        public void OpenNull()
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, null);
            MainViewModel viewModel = SetupViewModel(0, prompt, null, null);

            // Act
            Machine machine = viewModel.OpenMachine(prompt.Object, null, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(machine);
        }

        [TestCase(null, null, "")]
        [TestCase(null, "test.cpvc", "test")]
        [TestCase("test.cpvc", null, "test")]
        public void OpenMachine(string filepath, string promptedFilepath, string expectedMachineName)
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, promptedFilepath);
            MainViewModel viewModel = SetupViewModel(0, prompt, null, null);
            _mockBinaryWriter.Content = new List<byte>
            {
                0x00,
                      (byte)expectedMachineName.Length, 0x00, 0x00, 0x00
            };

            foreach (char c in expectedMachineName)
            {
                _mockBinaryWriter.Content.Add((byte)c);
            }

            _mockBinaryWriter.Content.AddRange(new byte[] {
                0x05,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00
            });

            // Act
            Machine machine = viewModel.OpenMachine(prompt.Object, filepath, _mockFileSystem.Object);

            // Verify
            prompt.Verify(x => x(FileTypes.Machine, true), (filepath != null) ? Times.Never() : Times.Once());
            prompt.VerifyNoOtherCalls();

            if (expectedMachineName != String.Empty)
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
                _mockBinaryWriter.Verify(bw => bw.ReadByte(), Times.Never);
            }
        }

        [Test]
        public void OpenInvalid()
        {
            // Setup
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null);
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, "test.cpvc");
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(true);

            // Act and Verify
            Assert.Throws<Exception>(() => viewModel.OpenMachine(prompt.Object, "test.cpvc", _mockFileSystem.Object));
        }

        [Test]
        public void OpenNonExistantFile()
        {
            // Setup
            _settingGet = "Test;test.cpvc";

            // Act
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, null, null, null, null);

            // Verify
            Assert.AreEqual(0, viewModel.Machines.Count);
        }

        [TestCase(null, null)]
        [TestCase("test.cpvc", "test")]
        public void NewMachine(string filepath, string expectedMachineName)
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);
            MainViewModel viewModel = SetupViewModel(0, prompt, null, null);
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(false);

            // Act
            viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

            // Verify
            prompt.Verify(x => x(FileTypes.Machine, false), Times.Once());
            prompt.VerifyNoOtherCalls();

            if (expectedMachineName != null)
            {
                // Stop the machine as a newly created machine will be in a running state.
                viewModel.Machines[0].Stop();

                Assert.AreEqual(1, viewModel.Machines.Count);
                Assert.AreEqual(expectedMachineName, viewModel.Machines[0].Name);
                _mockFileSystem.Verify(fileSystem => fileSystem.DeleteFile(filepath), Times.Once());
                _mockFileSystem.Verify(fileSystem => fileSystem.OpenFileByteStream(filepath), Times.Once());
            }
            else
            {
                Assert.IsEmpty(viewModel.Machines);
                _mockFileSystem.VerifyNoOtherCalls();
            }
        }

        /// <summary>
        /// Ensures that if we call MainViewModel.OpenMachine twice with the same filepath, the second call should return the same machine.
        /// </summary>
        [Test]
        public void OpenMachineTwice()
        {
            // Setup
            string filepath = "test.cpvc";
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, false, filepath);
            MainViewModel viewModel = SetupViewModel(0, prompt, null, null);
            _mockFileSystem.Setup(fileSystem => fileSystem.Exists(AnyString())).Returns(false);
            Machine machine = viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

            // Act
            Machine machine2 = viewModel.NewMachine(prompt.Object, _mockFileSystem.Object);

            // Verify
            Assert.AreEqual(machine, machine2);
        }

        [TestCase(false, null)]
        [TestCase(true, null)]
        [TestCase(true, "test2")]
        public void RenameMachine(bool active, string newName)
        {
            // Setup
            Mock<MainViewModel.PromptForNameDelegate> mockNamePrompt = new Mock<MainViewModel.PromptForNameDelegate>(MockBehavior.Loose);
            MainViewModel viewModel = SetupViewModel(1, null, null, mockNamePrompt);
            Machine machine = viewModel.Machines[0];
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

        [Test]
        public void NullActiveMachine()
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, null);

            MainViewModel viewModel = SetupViewModel(1, prompt, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachineViewModel = null;

            // Act and Verify
            Assert.DoesNotThrow(() =>
            {
                viewModel.ActiveMachineViewModel.KeyDownCommand.Execute(Keys.A);
                viewModel.ActiveMachineViewModel.ResetCommand.Execute(null);
                viewModel.ActiveMachineViewModel.PauseCommand.Execute(null);
                viewModel.ActiveMachineViewModel.ResumeCommand.Execute(null);
                viewModel.ActiveMachineViewModel.DriveACommand.Execute(null);
                viewModel.ActiveMachineViewModel.TapeCommand.Execute(null);
                viewModel.ActiveMachineViewModel.DriveAEjectCommand.Execute(null);
                viewModel.ActiveMachineViewModel.TapeEjectCommand.Execute(null);
                viewModel.ActiveMachineViewModel.AddBookmarkCommand.Execute(null);
                viewModel.ActiveMachineViewModel.JumpToMostRecentBookmarkCommand.Execute(null);
                viewModel.ActiveMachineViewModel.TurboCommand.Execute(null);
                viewModel.ActiveMachineViewModel.CompactCommand.Execute(null);
                viewModel.ActiveMachineViewModel.SeekToNextBookmarkCommand.Execute(null);
                viewModel.ActiveMachineViewModel.SeekToPrevBookmarkCommand.Execute(null);
                viewModel.ActiveMachineViewModel.SeekToStartCommand.Execute(null);
                viewModel.ActiveMachineViewModel.CloseCommand.Execute(null);
            });
        }

        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void SelectBookmark(bool active, bool selectEvent)
        {
            // Setup
            Mock<MainViewModel.PromptForBookmarkDelegate> prompt = new Mock<MainViewModel.PromptForBookmarkDelegate>(MockBehavior.Strict);
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 0, DateTime.Now, null);
            prompt.Setup(p => p()).Returns(selectEvent ? historyEvent : null);
            MainViewModel viewModel = SetupViewModel(1, null, prompt, null);
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            Machine machine = machineViewModel.Machine as Machine;
            machine.Open();
            viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

            // Act
            viewModel.ActiveMachineViewModel.BrowseBookmarksCommand.Execute(null);

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

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void ReadAudio(bool active, bool replayMachine)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            MachineViewModel machineViewModel = replayMachine ? viewModel.MachineViewModels[1] : viewModel.MachineViewModels[0];

            viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

            // Run the machine enough to fill up the audio buffer.
            Run(machineViewModel.Machine, 200, true);

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
            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            viewModel.PropertyChanged += propChanged.Object;
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            Machine machine = machineViewModel.Machine as Machine;
            if (closed)
            {
                machineViewModel.CloseCommand.Execute(null);
            }
            else
            {
                machineViewModel.OpenCommand.Execute(null);
            }

            // Act
            viewModel.ActiveMachineViewModel = machineViewModel;

            // Verify
            Assert.IsFalse(machine.RequiresOpen);
            Assert.AreEqual(machineViewModel, viewModel.ActiveMachineViewModel);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveMachineViewModel"), Times.Once);
        }

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

        [Test]
        public void Remove()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            (machineViewModel.Machine as IOpenableMachine)?.Open();

            // Act
            viewModel.Remove(machineViewModel);

            // Verify
            Assert.IsEmpty(viewModel.Machines);
            _mockSettings.VerifySet(x => x.RecentlyOpened = "", Times.Once);
        }

        [Test]
        public void OpenAndCloseMultiple()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(2, null, null, null);
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
        public void ToggleNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0, null, null, null);

            // Act and Verify
            Assert.DoesNotThrow(() => viewModel.ActiveMachineViewModel.ToggleRunningCommand.Execute(null));
        }

        [Test]
        public void EnableTurbo()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            machine.Open();
            viewModel.ActiveMachineViewModel = machineViewModel;

            // Act - enable turbo mode and run for enough ticks that should cause 10 audio
            //       samples to be written while in turbo mode.
            viewModel.ActiveMachineViewModel.TurboCommand.Execute(true);
            Run(machine, 8300, true);

            // Verify
            byte[] buffer = new byte[100];
            int samples = machine.ReadAudio(buffer, 0, buffer.Length);
            Assert.AreEqual(10, samples);
        }

        /// <summary>
        /// Ensures that a Reset call is passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        /// <param name="nullMachine">Indicates whether Reset should be called with a null parameter instead of a machine.</param>
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Reset(bool active, bool nullMachine)
        {
            // Setup
            Mock<RequestProcessedDelegate> mockAuditor = new Mock<RequestProcessedDelegate>();
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            Machine machine = viewModel.Machines[0];

            bool resetCalled = false;
            bool[] keys = new bool[80];
            Array.Clear(keys, 0, keys.Length);
            RequestProcessedDelegate auditor = (core, request, action) =>
            {
                if (core == machine.Core && action != null)
                {
                    if (action.Type == CoreRequest.Types.KeyPress)
                    {
                        keys[action.KeyCode] = action.KeyDown;
                    }
                    else if (action.Type == CoreRequest.Types.Reset)
                    {
                        resetCalled = true;
                    }
                }
            };

            machine.Open();
            machine.Core.Auditors += auditor;
            viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

            // Act
            machineViewModel.ResetCommand.Execute(null);
            machineViewModel.KeyDownCommand.Execute(Keys.A);

            ProcessQueueAndStop(machine.Core);

            // Verify
            Assert.False(machine.Core.Running);

            Assert.AreEqual(active || !nullMachine, resetCalled);
            Times expectedResetTimes = (active || !nullMachine) ? Times.Once() : Times.Never();
            Times expectedKeyTimes = active ? Times.Once() : Times.Never();

            for (byte k = 0; k < 80; k++)
            {
                if (k == Keys.A)
                {
                    Assert.AreEqual(active, keys[k]);
                }
                else
                {
                    Assert.False(keys[k]);
                }
            }
        }

        /// <summary>
        /// Ensures that a AddBookmark call is passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void AddBookmark(bool active)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            MachineViewModel machineViewModel = viewModel.MachineViewModels[0];
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachineViewModel = active ? machineViewModel : null;

            // Act
            viewModel.ActiveMachineViewModel.AddBookmarkCommand.Execute(null);

            // Verify
            if (active)
            {
                Assert.IsNotNull(machine.CurrentEvent.Bookmark);
            }
            else
            {
                Assert.IsNull(machine.CurrentEvent.Bookmark);
            }
        }

        [Test]
        public void OpenReplayMachine()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);

            // Verify
            Assert.AreEqual(1, viewModel.ReplayMachines.Count);
            Assert.AreEqual("Test Replay", viewModel.ReplayMachines[0].Name);
        }
    }
}
