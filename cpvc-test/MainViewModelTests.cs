using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MainViewModelTests
    {
        private Mock<ISettings> _mockSettings;
        private Mock<IFile> _mockFile;
        private Mock<IFileSystem> _mockFileSystem;

        private string[] _lines;
        private string _settingGet;

        [SetUp]
        public void Setup()
        {
            _mockSettings = new Mock<ISettings>(MockBehavior.Strict);
            _mockSettings.SetupGet(x => x.RecentlyOpened).Returns(() => _settingGet);
            _mockSettings.SetupSet(x => x.RecentlyOpened = It.IsAny<string>());

            _mockFile = new Mock<IFile>();
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines(AnyString())).Returns(() => _lines);
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(_mockFile.Object);

        }

        [Test]
        public void ThrowsWhenNewMachineFails()
        {
            // Setup
            string filepath = "test.cpvc";
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(filepath)).Throws(new Exception("File not found"));
            mockFileSystem.Setup(ReadBytes()).Throws(new Exception("File missing"));
            mockFileSystem.Setup(DeleteFile(filepath));

            Mock<ISettings> mockSettings = new Mock<ISettings>(MockBehavior.Loose);

            // Act and Verify
            Exception ex = Assert.Throws<Exception>(() =>
            {
                MainViewModel viewModel = new MainViewModel(mockSettings.Object, mockFileSystem.Object);
                viewModel.NewMachine(filepath, mockFileSystem.Object);
            });
            Assert.AreEqual(ex.Message, "File not found");
        }

        [Test]
        public void NewNull()
        {
            // Setup
            _settingGet = "";
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Act
            Machine machine = viewModel.NewMachine(null, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(machine);
        }

        [Test]
        public void OpenNull()
        {
            // Setup
            _settingGet = "";
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Act
            Machine machine = viewModel.OpenMachine(null, _mockFileSystem.Object);

            // Verify
            Assert.IsNull(machine);
        }

        [Test]
        public void Open()
        {
            // Setup
            _settingGet = "";
            _lines = new string[] { "name:Test", "checkpoint:0:0:0:0" };
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Act
            Machine machine = viewModel.OpenMachine("test.cpvc", _mockFileSystem.Object);

            // Verify
            Assert.IsNotNull(machine);
            Assert.Contains(machine, viewModel.Machines);
            _mockSettings.VerifySet(x => x.RecentlyOpened = "Test;test.cpvc", Times.Once);
        }

        [Test]
        public void OpenInvalid()
        {
            // Setup
            _settingGet = "";
            _lines = new string[] { "invalid" };
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Act and Verify
            Assert.Throws<Exception>(() => viewModel.OpenMachine("cpvc.test", _mockFileSystem.Object));
        }

        [Test]
        public void Remove()
        {
            // Setup
            _settingGet = "Test;test.cpvc";
            _lines = new string[] { "name:Test", "checkpoint:0:0:0:0" };
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            Machine machine = viewModel.Machines[0];

            // Act
            viewModel.Remove(machine);

            // Verify
            Assert.AreEqual(0, viewModel.Machines.Count);
        }

        [Test]
        public void OpenAndCloseMultiple()
        {
            // Setup
            _settingGet = "Test;test.cpvc,Test2;test2.cpvc";
            _lines = new string[] { "name:Test", "checkpoint:0:0:0:0" };
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
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
            _settingGet = "Test;test.cpvc";
            _lines = new string[] { "name:Test", "checkpoint:0:0:0:0" };
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);
            Machine machine = viewModel.Machines[0];
            machine.Open();

            // Act
            bool runningState1 = machine.Core.Running;
            viewModel.ToggleRunning(machine);
            bool runningState2 = machine.Core.Running;
            viewModel.ToggleRunning(machine);
            bool runningState3 = machine.Core.Running;

            // Verify
            Assert.AreEqual(runningState1, runningState3);
            Assert.AreNotEqual(runningState1, runningState2);
        }

        [Test]
        public void ToggleNull()
        {
            // Setup
            _settingGet = "";
            _lines = null;
            MainViewModel viewModel = new MainViewModel(_mockSettings.Object, _mockFileSystem.Object);

            // Act and Verify
            Assert.DoesNotThrow(() => viewModel.ToggleRunning(null));
        }
    }
}
