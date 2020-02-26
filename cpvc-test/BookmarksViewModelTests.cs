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

        private Machine CreateMachineWithHistory()
        {
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            Run(machine, 100, true);
            machine.AddBookmark(false);
            Run(machine, 300, true);
            machine.JumpToMostRecentBookmark();
            Run(machine, 100, true);
            machine.AddBookmark(false);
            Run(machine, 100, true);
            machine.JumpToMostRecentBookmark();
            Run(machine, 300, true);
            machine.AddBookmark(true);

            // Diagram of this history...
            // 
            // 600: x
            //      |
            // 400: | o
            // 300: o |
            // 200: o |
            // 100: o-/
            //      |
            //   0: o

            return machine;
        }

        [SetUp]
        public void Setup()
        {
            MockFileByteStream mockBinaryFile = new MockFileByteStream();
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream(AnyString())).Returns(mockBinaryFile.Object);
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
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object))
            {
                // Act
                viewModel.SelectedItem = null;

                // Verify
                Assert.IsNull(viewModel.SelectedItem);
                Assert.IsNull(viewModel.Bitmap);
                Assert.IsFalse(viewModel.DeleteBookmarksCommand.CanExecute(null));
                Assert.IsFalse(viewModel.JumpToBookmarkCommand.CanExecute(null));
                Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
                Assert.IsFalse(viewModel.ReplayTimelineCommand.CanExecute(null));
            }
        }

        [Test]
        public void SetSelectedItemCurrent()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object))
            {
                // The first item is the current event.
                viewModel.SelectedItem = viewModel.Items[0];

                // Verify
                Assert.AreEqual(machine.CurrentEvent, viewModel.SelectedItem.HistoryEvent);
                Assert.AreEqual(viewModel.Items[0], viewModel.SelectedItem);
                Assert.IsNotNull(viewModel.Bitmap);
                Assert.IsTrue(viewModel.DeleteBookmarksCommand.CanExecute(null));
                Assert.IsTrue(viewModel.JumpToBookmarkCommand.CanExecute(null));
                Assert.IsTrue(viewModel.DeleteBranchesCommand.CanExecute(null));
                Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
            }
        }

        [Test]
        public void SetSelectedItemBookmark()
        {
            // Setup
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            HistoryEvent bookmarkEvent = machine.CurrentEvent;
            RunForAWhile(machine);
            machine.AddBookmark(true);

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object))
            {
                // The second item is the bookmark event.
                viewModel.SelectedItem = viewModel.Items[1];

                // Verify
                Assert.AreEqual(bookmarkEvent, viewModel.SelectedItem.HistoryEvent);
                Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItem);
                Assert.IsNotNull(viewModel.Bitmap);
                Assert.IsTrue(viewModel.DeleteBookmarksCommand.CanExecute(null));
                Assert.IsTrue(viewModel.JumpToBookmarkCommand.CanExecute(null));
                Assert.IsTrue(viewModel.DeleteBranchesCommand.CanExecute(null));
                Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
            }
        }

        [Test]
        public void SetSelectedItemRoot()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object))
            {
                // The last item is the root event.
                viewModel.SelectedItem = viewModel.Items[1];

                // Verify
                Assert.AreEqual(machine.RootEvent, viewModel.SelectedItem.HistoryEvent);
                Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItem);
                Assert.IsNull(viewModel.Bitmap);
                Assert.IsFalse(viewModel.DeleteBookmarksCommand.CanExecute(null));
                Assert.IsFalse(viewModel.JumpToBookmarkCommand.CanExecute(null));
                Assert.IsTrue(viewModel.DeleteBranchesCommand.CanExecute(null));
                Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
            }
        }

        [Test]
        public void SimpleHistory()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            RunForAWhile(machine);
            machine.Key(Keys.A, true);
            RunForAWhile(machine);
            machine.Key(Keys.A, false);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object))
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
            machine.JumpToMostRecentBookmark();
            HistoryEvent event400 = machine.CurrentEvent.Children[0];
            Run(machine, 100, true);
            machine.AddBookmark(false);
            HistoryEvent event200 = machine.CurrentEvent;
            Run(machine, 100, true);
            machine.JumpToMostRecentBookmark();
            HistoryEvent event300 = machine.CurrentEvent.Children[0];
            Run(machine, 300, true);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            using (BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object))
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

        [Test]
        public void DisposeTwice()
        {
            // Setup
            using (Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object))
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                viewModel.Dispose();

                // Act and Verify
                Assert.DoesNotThrow(() => viewModel.Dispose());
            }
        }

        [Test]
        public void DeleteBookmark()
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem bookmarkEventViewItem = viewModel.Items[3];
                viewModel.SelectedItem = bookmarkEventViewItem;

                // Act
                viewModel.DeleteBookmarksCommand.Execute(null);

                // Verify
                Assert.IsNull(bookmarkEventViewItem.HistoryEvent.Bookmark);
            }
        }

        [Test]
        public void DeleteBranch()
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;
                viewModel.SelectedItem = branchViewItem;
                int viewItemsCount = viewModel.Items.Count;

                // Act
                viewModel.DeleteBranchesCommand.Execute(null);

                // Verify
                Assert.AreEqual(1, parentEvent.Children.Count);
            }
        }

        [Test]
        public void JumpToBookmark()
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem bookmarkEventViewItem = viewModel.Items[3];
                viewModel.SelectedItem = bookmarkEventViewItem;

                // Act
                viewModel.JumpToBookmarkCommand.Execute(null);

                // Verify
                Assert.AreEqual(bookmarkEventViewItem.HistoryEvent, viewModel.SelectedJumpEvent);
                mockItemSelected.Verify(s => s(), Times.Once());
            }
        }

        [Test]
        public void ReplayTimeline()
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem historyEventViewItem = viewModel.Items[1];
                viewModel.SelectedItem = historyEventViewItem;

                // Act
                viewModel.ReplayTimelineCommand.Execute(null);

                // Verify
                Assert.AreEqual(historyEventViewItem.HistoryEvent, viewModel.SelectedReplayEvent);
                mockItemSelected.Verify(s => s(), Times.Once());
            }
        }
    }
}
