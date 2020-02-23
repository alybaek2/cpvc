using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Windows.Input;
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

            ReplayMachine replayMachine = new ReplayMachine(historyEvent);
            viewModel.ReplayMachines.Add(replayMachine);

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
            string oldName = machine.Name;
            mockNamePrompt.Setup(x => x(oldName)).Returns(newName);

            if (active)
            {
                viewModel.ActiveMachine = machine;
            }

            // Act
            viewModel.RenameCommand.Execute(null);

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
            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, filename);

            MainViewModel viewModel = SetupViewModel(1, mockPrompt, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = machine;

            // Act
            if (fileType == FileTypes.Tape)
            {
                viewModel.TapeCommand.Execute(null);
            }
            else
            {
                ICommand command = (drive == 0) ? viewModel.DriveACommand : viewModel.DriveBCommand;
                command.Execute(null);
            }

            // Verify
            mockPrompt.Verify(x => x(fileType, true), Times.Once());
            mockPrompt.VerifyNoOtherCalls();
            _mockFileSystem.Verify(ReadBytes(filename), Times.Once());
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

            List<string> entries = new List<string>();
            for (int e = 0; e < entryCount; e++)
            {
                entries.Add(String.Format("test{0}.{1}", e, (drive == 2) ? "cdt" : "dsk"));
            }

            // Throw in a file with a different extension in order to verify it isn't shown
            // in the Select Item window.
            List<string> entriesWithExtraneousFile = new List<string>(entries)
            {
                "test.txt"
            };

            _mockFileSystem.Setup(GetZipFileEntryNames(zipFilename)).Returns(entriesWithExtraneousFile);

            Mock<MainViewModel.PromptForFileDelegate> mockPrompt = SetupPrompt(fileType, true, zipFilename);
            Mock<MainViewModel.SelectItemDelegate> mockSelect = new Mock<MainViewModel.SelectItemDelegate>();

            if (entryCount > 0)
            {
                _mockFileSystem.Setup(GetZipFileEntry(zipFilename, entries[0])).Returns(new byte[1]);

                if (entryCount > 1)
                {
                    mockSelect.Setup(x => x(entries)).Returns(selectFile ? entries[0] : null);
                }
            }

            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object, mockSelect.Object, mockPrompt.Object, null, null);
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            viewModel.ActiveMachine = machine;

            // Act
            if (drive == 2)
            {
                viewModel.TapeCommand.Execute(null);
            }
            else if (drive == 0)
            {
                viewModel.DriveACommand.Execute(null);
            }
            else if (drive == 1)
            {
                viewModel.DriveBCommand.Execute(null);
            }

            // Verify
            mockPrompt.Verify(x => x(fileType, true), Times.Once());

            if (entryCount == 0)
            {
                _mockFileSystem.Verify(GetZipFileEntry(zipFilename), Times.Never());
            }
            else if (entryCount == 1)
            {
                _mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), Times.Once());
            }
            else if (entryCount > 1)
            {
                _mockFileSystem.Verify(GetZipFileEntry(zipFilename, entries[0]), selectFile ? Times.Once() : Times.Never());
                mockSelect.Verify(x => x(entries), Times.Once());
            }

            if (zipFilename != null)
            {
                _mockFileSystem.Verify(GetZipFileEntryNames(zipFilename), Times.Once());
            }

            mockSelect.VerifyNoOtherCalls();
        }

        [Test]
        public void NullActiveMachine()
        {
            // Setup
            Mock<MainViewModel.PromptForFileDelegate> prompt = SetupPrompt(FileTypes.Machine, true, null);

            MainViewModel viewModel = SetupViewModel(1, prompt, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = null;

            // Act and Verify
            Assert.DoesNotThrow(() =>
            {
                viewModel.Key(Keys.A, true);
                viewModel.ResetCommand.Execute(null);
                viewModel.PauseCommand.Execute(null);
                viewModel.ResumeCommand.Execute(null);
                viewModel.DriveACommand.Execute(null);
                viewModel.TapeCommand.Execute(null);
                viewModel.DriveAEjectCommand.Execute(null);
                viewModel.TapeEjectCommand.Execute(null);
                viewModel.AddBookmarkCommand.Execute(null);
                viewModel.SeekToPreviousBookmarkCommand.Execute(null);
                viewModel.EnableTurbo(true);
                viewModel.CompactCommand.Execute(null);
                viewModel.Close(null);
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
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.BrowseBookmarksCommand.Execute(null);

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
            CoreMachine coreMachine = replayMachine ? (CoreMachine)viewModel.ReplayMachines[0] : (CoreMachine)machine;
            viewModel.ActiveMachine = active ? coreMachine : null;

            // Run the machine enough to fill up the audio buffer.
            Run(coreMachine, 4000000, true);

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
            Machine machine = viewModel.Machines[0];
            if (closed)
            {
                machine.Close();
            }
            else
            {
                machine.Open();
            }

            // Act
            viewModel.ActiveMachine = machine;

            // Verify
            Assert.IsFalse(machine.RequiresOpen);
            Assert.AreEqual(machine, viewModel.ActiveMachine);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveMachine"), Times.Once);
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
            Assert.IsNull(viewModel.ActiveMachine);
            Assert.AreEqual(nonMachine, viewModel.ActiveItem);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveItem"), Times.Once);
            propChanged.Verify(PropertyChanged(viewModel, "ActiveMachine"), Times.Once);
        }

        [Test]
        public void Remove()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();

            // Act
            viewModel.Remove(machine);

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
        public void Toggle()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();

            // Act
            bool runningState1 = machine.Core.Running;
            viewModel.ToggleRunningCommand.Execute(machine);
            bool runningState2 = machine.Core.Running;
            viewModel.ToggleRunningCommand.Execute(machine);
            bool runningState3 = machine.Core.Running;

            // Verify
            Assert.AreEqual(runningState1, runningState3);
            Assert.AreNotEqual(runningState1, runningState2);
        }

        [Test]
        public void ToggleNull()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(0, null, null, null);

            // Act and Verify
            Assert.DoesNotThrow(() => viewModel.ToggleRunningCommand.Execute(null));
        }

        [Test]
        public void EnableTurbo()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = machine;

            // Act - enable turbo mode and run for enough ticks that should cause 10 audio
            //       samples to be written while in turbo mode.
            viewModel.EnableTurbo(true);
            Run(machine, 8300, true);

            // Verify
            byte[] buffer = new byte[100];
            int samples = machine.ReadAudio(buffer, 0, buffer.Length);
            Assert.AreEqual(10, samples);
        }

        [Test]
        public void EjectTape()
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = machine;

            // Act
            viewModel.TapeEjectCommand.Execute(null);

            ProcessQueueAndStop(machine.Core);

            // Verify - need a better way of checking this; perhaps play the tape and check no tones are generated.
            Assert.False(machine.Core.Running);
            Assert.AreEqual("Ejected tape", machine.Status);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void EjectDisc(byte drive)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = machine;

            // Act
            ICommand command = (drive == 0) ? viewModel.DriveAEjectCommand : viewModel.DriveBEjectCommand;
            command.Execute(null);

            ProcessQueueAndStop(machine.Core);

            // Verify - need a better way of checking this; perhaps query the FDC main status register.
            Assert.False(machine.Core.Running);
            Assert.AreEqual("Ejected disc", machine.Status);
        }

        /// <summary>
        /// Ensures that Pause and Resume calls are passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        /// <param nam="nullMachine">Indicates whether Pause and Resume should be called with a null parameter instead of a machine.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void PauseAndResume(bool active)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            bool initialState = machine.Core.Running;
            viewModel.PauseCommand.Execute(null);
            bool pausedState = machine.Core.Running;
            viewModel.ResumeCommand.Execute(null);
            bool runningState = machine.Core.Running;

            // Verify
            Assert.IsFalse(initialState);
            Assert.AreEqual(active, runningState);
            Assert.IsFalse(pausedState);
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
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.ResetCommand.Execute(null);
            viewModel.Key(Keys.A, true);

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
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.AddBookmarkCommand.Execute(null);

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

        /// <summary>
        /// Ensures that a SeekToLastBookmark call is passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void SeekToLastBookmark(bool active)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            machine.AddBookmark(false);
            HistoryEvent bookmarkEvent = machine.CurrentEvent;

            for (byte k = 0; k < 80; k++)
            {
                machine.Key(k, true);
            }

            bool[] keys = new bool[80];
            Array.Clear(keys, 0, keys.Length);
            RequestProcessedDelegate auditor = (core, request, action) =>
            {
                if (request != null && request.Type == CoreRequest.Types.KeyPress)
                {
                    keys[request.KeyCode] = request.KeyDown;
                }
            };

            machine.Core.Auditors += auditor;
            RunForAWhile(machine);
            ProcessQueueAndStop(machine.Core);

            HistoryEvent lastEvent = machine.CurrentEvent;
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.SeekToPreviousBookmarkCommand.Execute(null);
            machine.Core.Auditors += auditor;
            ProcessQueueAndStop(machine.Core);

            // Verify
            if (active)
            {
                Assert.AreEqual(bookmarkEvent, machine.CurrentEvent);
                for (byte j = 0; j < 80; j++)
                {
                    Assert.IsFalse(keys[j]);
                }
            }
            else
            {
                Assert.AreEqual(lastEvent, machine.CurrentEvent);
                for (byte j = 0; j < 80; j++)
                {
                    Assert.IsTrue(keys[j]);
                }
            }
        }

        /// <summary>
        /// Ensures that a CompactFile call is passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void CompactMachineFile(bool active)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.CompactCommand.Execute(null);

            // Verify
            if (active)
            {
                string expectedStatus = "Compacted";
                Assert.AreEqual(expectedStatus, machine.Status.Substring(0, expectedStatus.Length));
            }
            else
            {
                Assert.IsNull(machine.Status);
            }
        }

        /// <summary>
        /// Ensures that a Close call is passed through from the view model to the machine.
        /// </summary>
        /// <param name="active">Indicates whether the machine should be set as the view model's active machine.</param>
        /// <param name="nullMachine">Indicates whether Close should be called with a null parameter instead of a machine.</param>
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Close(bool active, bool nullMachine)
        {
            // Setup
            MainViewModel viewModel = SetupViewModel(1, null, null, null);
            Machine machine = viewModel.Machines[0];
            machine.Open();
            viewModel.ActiveMachine = active ? machine : null;

            // Act
            viewModel.Close(nullMachine ? null : machine);

            // Verify - a successfully closed machine should have its RequireOpen property set to true.
            Assert.AreEqual(active || !nullMachine, machine.RequiresOpen);
        }
    }
}
