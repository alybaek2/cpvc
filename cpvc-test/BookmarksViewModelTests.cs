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
        public void SetSelectedItemNull()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine, 100);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                viewModel.SelectedItem = null;

                // Verify
                Assert.IsNull(viewModel.SelectedItem);
                Assert.IsNull(viewModel.Bitmap);
                Assert.IsFalse(viewModel.CanDeleteBookmark);
                Assert.IsFalse(viewModel.CanJumpToBookmark);
                Assert.IsFalse(viewModel.CanDeleteBranch);
            }
        }

        [Test]
        public void SetSelectedItemCurrent()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine, 100);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                // The first item is the current event.
                viewModel.SelectedItem = viewModel.Items[0];

                // Verify
                Assert.AreEqual(machine.CurrentEvent, viewModel.SelectedItem.HistoryEvent);
                Assert.AreEqual(viewModel.Items[0], viewModel.SelectedItem);
                Assert.IsNotNull(viewModel.Bitmap);
                Assert.IsTrue(viewModel.CanDeleteBookmark);
                Assert.IsTrue(viewModel.CanJumpToBookmark);
                Assert.IsTrue(viewModel.CanDeleteBranch);
            }
        }

        [Test]
        public void SetSelectedItemBookmark()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine, 100);
            machine.AddBookmark(true);
            HistoryEvent bookmarkEvent = machine.CurrentEvent;
            RunForAWhile(machine, 100);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                // The second item is the bookmark event.
                viewModel.SelectedItem = viewModel.Items[1];

                // Verify
                Assert.AreEqual(bookmarkEvent, viewModel.SelectedItem.HistoryEvent);
                Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItem);
                Assert.IsNotNull(viewModel.Bitmap);
                Assert.IsTrue(viewModel.CanDeleteBookmark);
                Assert.IsTrue(viewModel.CanJumpToBookmark);
                Assert.IsTrue(viewModel.CanDeleteBranch);
            }
        }

        [Test]
        public void SetSelectedItemRoot()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine, 100);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                // The last item is the root event.
                viewModel.SelectedItem = viewModel.Items[1];

                // Verify
                Assert.AreEqual(machine.RootEvent, viewModel.SelectedItem.HistoryEvent);
                Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItem);
                Assert.IsNull(viewModel.Bitmap);
                Assert.IsFalse(viewModel.CanDeleteBookmark);
                Assert.IsFalse(viewModel.CanJumpToBookmark);
                Assert.IsTrue(viewModel.CanDeleteBranch);
            }
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
                Assert.IsNotNull(viewModel.Items[0].CreateDate);
                Assert.IsNotNull(viewModel.Items[1].CreateDate);
            }
        }

        [Test]
        public void SimpleBranchedHistory()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            Run(machine, 100, true);
            machine.AddBookmark(false);
            HistoryEvent event100 = machine.CurrentEvent;
            Run(machine, 300, true);
            machine.SeekToLastBookmark();
            HistoryEvent event400 = machine.CurrentEvent.Children[0];
            Run(machine, 100, true);
            machine.AddBookmark(false);
            HistoryEvent event200 = machine.CurrentEvent;
            Run(machine, 100, true);
            machine.SeekToLastBookmark();
            HistoryEvent event300 = machine.CurrentEvent.Children[0];
            Run(machine, 300, true);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine))
            {
                // Verify
                Assert.AreEqual(6, viewModel.Items.Count);
                Assert.AreEqual(machine.CurrentEvent, viewModel.Items[0].HistoryEvent);
                Assert.AreEqual(event400, viewModel.Items[1].HistoryEvent);
                Assert.AreEqual(event300, viewModel.Items[2].HistoryEvent);
                Assert.AreEqual(event200, viewModel.Items[3].HistoryEvent);
                Assert.AreEqual(event100, viewModel.Items[4].HistoryEvent);
                Assert.AreEqual(machine.RootEvent, viewModel.Items[5].HistoryEvent);
            }
        }
    }
}
