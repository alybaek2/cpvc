using Moq;
using NUnit.Framework;

namespace CPvC.Test
{
    public class MainModelTests
    {
        Mock<IFileByteStream> _mockFileBytestream;
        Mock<IFileSystem> _mockFileSystem;
        LocalMachine _machine;
        Mock<ISettings> _mockSettings;
        MainModel _mainModel;


        [SetUp]
        public void Setup()
        {
            _mockFileBytestream = new Mock<IFileByteStream>();
            _mockFileSystem = new Mock<IFileSystem>();
            _mockFileSystem.Setup(fs => fs.OpenFileByteStream("test.cpvc")).Returns(_mockFileBytestream.Object);
            _machine = LocalMachine.New("test", null);
            _mockSettings = new Mock<ISettings>();
            _mainModel = new MainModel(_mockSettings.Object, null);
        }

        [Test]
        public void AddMachineUpdate()
        {
            // Act
            _mainModel.AddMachine(_machine);

            // Verify
            _mockSettings.VerifySet(s => s.RecentlyOpened = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void RemoveMachineUpdate()
        {
            // Setup
            _mainModel.AddMachine(_machine);
            _mockSettings.Invocations.Clear();

            // Act
            _mainModel.RemoveMachine(_machine);

            // Verify
            _mockSettings.VerifySet(s => s.RecentlyOpened = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ModifyMachineUpdate()
        {
            // Setup
            _mainModel.AddMachine(_machine);
            _mockSettings.Invocations.Clear();

            // Act
            _machine.Persist(_mockFileSystem.Object, "test.cpvc");

            // Verify
            _mockSettings.VerifySet(s => s.RecentlyOpened = It.IsAny<string>(), Times.Once);
        }

        [Test]
        public void ModifyRemovedMachine()
        {
            // Setup
            _mainModel.AddMachine(_machine);
            _mainModel.RemoveMachine(_machine);
            _mockSettings.Invocations.Clear();

            // Act
            _machine.Persist(_mockFileSystem.Object, "test.cpvc");

            // Verify
            _mockSettings.VerifySet(s => s.RecentlyOpened = It.IsAny<string>(), Times.Never);
        }
    }
}
