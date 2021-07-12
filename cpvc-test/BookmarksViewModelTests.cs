using CPvC.UI;
using Moq;
using NUnit.Framework;
using System.ComponentModel;
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
            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);

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

        [Test]
        public void SetSelectedItemCurrent()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);

            // The first item is the current event.
            viewModel.SelectedItem = viewModel.Items[0];

            // Verify
            Assert.AreEqual(machine.History.CurrentEvent, viewModel.SelectedItem.HistoryEvent);
            Assert.AreEqual(viewModel.Items[0], viewModel.SelectedItem);
            Assert.IsNotNull(viewModel.Bitmap);
            Assert.IsTrue(viewModel.DeleteBookmarksCommand.CanExecute(null));
            Assert.IsTrue(viewModel.JumpToBookmarkCommand.CanExecute(null));
            Assert.IsTrue(viewModel.DeleteBranchesCommand.CanExecute(null));
            Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
        }

        [Test]
        public void SetSelectedItemBookmark()
        {
            // Setup
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            HistoryEvent bookmarkEvent = machine.History.CurrentEvent;
            RunForAWhile(machine);
            machine.AddBookmark(true);

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);

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

        [Test]
        public void SetSelectedItemRoot()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);

            // The last item is the root event.
            viewModel.SelectedItem = viewModel.Items[1];

            // Verify
            Assert.AreEqual(machine.History.RootEvent, viewModel.SelectedItem.HistoryEvent);
            Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItem);
            Assert.IsNull(viewModel.Bitmap);
            Assert.IsFalse(viewModel.DeleteBookmarksCommand.CanExecute(null));
            Assert.IsFalse(viewModel.JumpToBookmarkCommand.CanExecute(null));
            Assert.IsTrue(viewModel.DeleteBranchesCommand.CanExecute(null));
            Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
        }

        [Test]
        public void SimpleHistory()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            RunForAWhile(machine);
            machine.Key(Keys.A, true);
            RunForAWhile(machine);
            machine.Key(Keys.A, false);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);

            // Verify
            Assert.AreEqual(2, viewModel.Items.Count);
            Assert.AreEqual(machine.History.CurrentEvent, viewModel.Items[0].HistoryEvent);
            Assert.AreEqual(machine.History.RootEvent, viewModel.Items[1].HistoryEvent);
            Assert.IsNotNull(viewModel.Items[0].CreateDate);
            Assert.IsNotNull(viewModel.Items[1].CreateDate);
        }

        [Test]
        public void SimpleBranchedHistory()
        {
            // Setup
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            Run(machine, 100, true);
            machine.AddBookmark(false);
            HistoryEvent event100 = machine.History.CurrentEvent;
            Run(machine, 300, true);
            machine.JumpToMostRecentBookmark();
            HistoryEvent event400 = machine.History.CurrentEvent.Children[0];
            Run(machine, 100, true);
            machine.AddBookmark(false);
            HistoryEvent event200 = machine.History.CurrentEvent;
            Run(machine, 100, true);
            machine.JumpToMostRecentBookmark();
            HistoryEvent event300 = machine.History.CurrentEvent.Children[0];
            Run(machine, 300, true);
            machine.AddBookmark(true);
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);

            // Verify
            Assert.AreEqual(6, viewModel.Items.Count);
            Assert.AreEqual(machine.History.CurrentEvent, viewModel.Items[0].HistoryEvent);
            Assert.AreEqual(event400, viewModel.Items[1].HistoryEvent);
            Assert.AreEqual(event300, viewModel.Items[2].HistoryEvent);
            Assert.AreEqual(event200, viewModel.Items[3].HistoryEvent);
            Assert.AreEqual(event100, viewModel.Items[4].HistoryEvent);
            Assert.AreEqual(machine.History.RootEvent, viewModel.Items[5].HistoryEvent);
        }

        [Test]
        public void DeleteBookmark([Values(false, true)] bool nullSelectedItem)
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem bookmarkEventViewItem = viewModel.Items[3];
                viewModel.SelectedItem = nullSelectedItem ? null : bookmarkEventViewItem;

                // Act
                viewModel.DeleteBookmarksCommand.Execute(null);

                // Verify
                Assert.That(bookmarkEventViewItem.HistoryEvent.Bookmark, nullSelectedItem ? Is.Not.Null : Is.Null);
            }
        }

        [Test]
        public void DeleteBranch([Values(false, true)] bool nullSelectedItem)
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;
                viewModel.SelectedItem = nullSelectedItem ? null : branchViewItem;

                // Act
                viewModel.DeleteBranchesCommand.Execute(null);

                // Verify
                bool eventFound = false;
                for (int i = 0; i < viewModel.Items.Count; i++)
                {
                    if (viewModel.Items[i].HistoryEvent == branchViewItem.HistoryEvent)
                    {
                        eventFound = true;
                    }
                }

                Assert.AreEqual(nullSelectedItem, eventFound);
                Assert.AreEqual(nullSelectedItem ? 2 : 1, parentEvent.Children.Count);
            }
        }

        [Test]
        public void JumpToBookmark([Values(false, true)] bool nullSelectedItem)
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem bookmarkEventViewItem = viewModel.Items[3];
                viewModel.SelectedItem = nullSelectedItem ? null : bookmarkEventViewItem;

                // Act
                viewModel.JumpToBookmarkCommand.Execute(null);

                // Verify
                Assert.That(viewModel.SelectedJumpEvent, Is.EqualTo(nullSelectedItem ? null : viewModel.SelectedJumpEvent));
                mockItemSelected.Verify(s => s(), nullSelectedItem ? Times.Never() : Times.Once());
            }
        }

        [Test]
        public void ReplayTimeline([Values(false, true)] bool nullSelectedItem)
        {
            // Setup
            using (Machine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem historyEventViewItem = viewModel.Items[1];
                viewModel.SelectedItem = nullSelectedItem ? null : historyEventViewItem;

                // Act
                viewModel.ReplayTimelineCommand.Execute(null);

                // Verify
                Assert.That(viewModel.SelectedReplayEvent, Is.EqualTo(nullSelectedItem ? null : viewModel.SelectedReplayEvent));
                mockItemSelected.Verify(s => s(), nullSelectedItem ? Times.Never() : Times.Once());
            }
        }

        //[Test]
        //public void PropertyChanged([Values(false, true)] bool subscribeToCanExecuteChanged)
        //{
        //    // Setup
        //    using (Machine machine = CreateMachineWithHistory())
        //    {
        //        Mock<System.EventHandler> mockDeleteBookmarksHandler = new Mock<System.EventHandler>();
        //        Mock<System.EventHandler> mockDeleteBranchesHandler = new Mock<System.EventHandler>();
        //        Mock<System.EventHandler> mockJumpToBookmarkHandler = new Mock<System.EventHandler>();
        //        Mock<System.EventHandler> mockReplayTimelineHandler = new Mock<System.EventHandler>();

        //        BookmarksViewModel viewModel = new BookmarksViewModel(machine, null);
        //        Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
        //        viewModel.PropertyChanged += propChanged.Object;

        //        if (subscribeToCanExecuteChanged)
        //        {
        //            viewModel.DeleteBookmarksCommand.CanExecuteChanged += mockDeleteBookmarksHandler.Object;
        //            viewModel.DeleteBranchesCommand.CanExecuteChanged += mockDeleteBranchesHandler.Object;
        //            viewModel.JumpToBookmarkCommand.CanExecuteChanged += mockJumpToBookmarkHandler.Object;
        //            viewModel.ReplayTimelineCommand.CanExecuteChanged += mockReplayTimelineHandler.Object;
        //        }

        //        // Act
        //        viewModel.SelectedItem = new HistoryViewItem(HistoryEvent.CreateCheckpoint(0, 100, System.DateTime.UtcNow, null));

        //        // Verify
        //        propChanged.Verify(p => p(viewModel, It.Is<PropertyChangedEventArgs>(e => e.PropertyName == "SelectedItem")));
        //        propChanged.Verify(p => p(viewModel, It.Is<PropertyChangedEventArgs>(e => e.PropertyName == "Bitmap")));

        //        Times canExecuteTimes = subscribeToCanExecuteChanged ? Times.Once() : Times.Never();
        //        mockDeleteBookmarksHandler.Verify(p => p(viewModel, It.IsAny<System.EventArgs>()), canExecuteTimes);
        //        mockDeleteBranchesHandler.Verify(p => p(viewModel, It.IsAny<System.EventArgs>()), canExecuteTimes);
        //        mockJumpToBookmarkHandler.Verify(p => p(viewModel, It.IsAny<System.EventArgs>()), canExecuteTimes);
        //        mockReplayTimelineHandler.Verify(p => p(viewModel, It.IsAny<System.EventArgs>()), canExecuteTimes);
        //    }
        //}
    }
}
