using CPvC.UI;
using Moq;
using NUnit.Framework;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [Apartment(ApartmentState.STA)]
    public class BookmarksViewModelTests
    {
        private Mock<IFileSystem> _mockFileSystem;

        [SetUp]
        public void Setup()
        {
            Mock<IFile> mockFile = new Mock<IFile>();
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(mockFile.Object);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);
        }

        [TearDown]
        public void Teardown()
        {
            _mockFileSystem = null;
        }

        [Test]
        public void SimpleHistory()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine, 100);
            machine.Key(Keys.A, true);
            RunForAWhile(machine, 100);
            machine.Key(Keys.A, false);
            RunForAWhile(machine, 100);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                // Verify
                Assert.AreEqual(2, viewModel.Items.Count);
                Assert.AreEqual(machine.CurrentEvent, viewModel.Items[0].HistoryEvent);
                Assert.AreEqual(machine.RootEvent, viewModel.Items[1].HistoryEvent);
            }
        }

        [Test]
        public void SimpleBranchedHistory()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            Run(machine, 100, true);
            machine.AddBookmark(false);
            HistoryEvent bookmarkEvent = machine.CurrentEvent;
            Run(machine, 200, true);
            machine.SeekToLastBookmark();
            HistoryEvent branchEvent = machine.CurrentEvent.Children[0];
            Run(machine, 300, true);
            machine.AddBookmark(true);
            HistoryEvent bookmarkEvent2 = machine.CurrentEvent;
            Run(machine, 100, true);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                // Verify
                Assert.AreEqual(5, viewModel.Items.Count);
                Assert.AreEqual(machine.CurrentEvent, viewModel.Items[0].HistoryEvent);
                Assert.AreEqual(bookmarkEvent2, viewModel.Items[1].HistoryEvent);
                Assert.AreEqual(branchEvent, viewModel.Items[2].HistoryEvent);
                Assert.AreEqual(bookmarkEvent, viewModel.Items[3].HistoryEvent);
                Assert.AreEqual(machine.RootEvent, viewModel.Items[4].HistoryEvent);
            }
        }
    }
}
