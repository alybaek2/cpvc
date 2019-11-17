using CPvC.UI;
using Moq;
using NUnit.Framework;
using System;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class MainViewModelTests
    {
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
    }
}
