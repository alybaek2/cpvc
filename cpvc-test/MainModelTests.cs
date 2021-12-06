using Moq;
using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class MainModelTests
    {
        Mock<IFileSystem> _mockFileSystem;
        LocalMachine _machine;
        Mock<ISettings> _mockSettings;
        MainModel _mainModel;


        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new Mock<IFileSystem>();
            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(TestHelpers.AnyString())).Returns(mockTextFile);
            _machine = LocalMachine.New("test", null, null);
            _mockSettings = new Mock<ISettings>();
            _mainModel = new MainModel(_mockSettings.Object, null);
        }

        [Test]
        public void AddNullMachine()
        {
            // Act and Verify
            Assert.Throws<ArgumentException>(() => _mainModel.AddMachine(null));
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
