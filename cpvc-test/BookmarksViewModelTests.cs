using CPvC.UI;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [Apartment(ApartmentState.STA)]
    public class BookmarksViewModelTests
    {
        private Mock<IFileSystem> _mockFileSystem;

        private LocalMachine CreateMachineWithHistory()
        {
            LocalMachine machine = LocalMachine.New("test", null, null);
            Run(machine, 100);
            machine.AddBookmark(false);
            Run(machine, 300);
            machine.JumpToMostRecentBookmark();
            Run(machine, 100);
            machine.AddBookmark(false);
            Run(machine, 100);
            machine.JumpToMostRecentBookmark();
            Run(machine, 300);
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
            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(AnyString())).Returns(mockTextFile);
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
            LocalMachine machine = LocalMachine.New("test", null, null);
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
            LocalMachine machine = LocalMachine.New("test", null, null);
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
            Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
            Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
        }

        [Test]
        public void SetSelectedItemBookmark()
        {
            // Setup
            Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();

            LocalMachine machine = LocalMachine.New("test", null, null);
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
            Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
            Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
        }

        [Test]
        public void SetSelectedItemRoot()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("test", null, null);
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
            Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
            Assert.IsTrue(viewModel.ReplayTimelineCommand.CanExecute(null));
        }

        [Test]
        public void SimpleHistory()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("test", null, null);
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
            LocalMachine machine = LocalMachine.New("test", null, null);
            Run(machine, 100);
            machine.AddBookmark(false);
            HistoryEvent event100 = machine.History.CurrentEvent;
            Run(machine, 300);
            machine.JumpToMostRecentBookmark();
            HistoryEvent event400 = machine.History.CurrentEvent.Children[0];
            Run(machine, 100);
            machine.AddBookmark(false);
            HistoryEvent event200 = machine.History.CurrentEvent;
            Run(machine, 100);
            machine.JumpToMostRecentBookmark();
            HistoryEvent event300 = machine.History.CurrentEvent.Children[0];
            Run(machine, 300);
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
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                Mock<BookmarksViewModel.ItemSelectedDelegate> mockItemSelected = new Mock<BookmarksViewModel.ItemSelectedDelegate>();
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, mockItemSelected.Object);
                HistoryViewItem bookmarkEventViewItem = viewModel.Items[3];
                Bookmark bookmark = bookmarkEventViewItem.HistoryEvent.Bookmark;
                viewModel.SelectedItem = nullSelectedItem ? null : bookmarkEventViewItem;
                HistoryEvent parentEvent = bookmarkEventViewItem.HistoryEvent.Parent;
                List<HistoryEvent> childEvents = new List<HistoryEvent>();
                childEvents.AddRange(bookmarkEventViewItem.HistoryEvent.Children);

                // Act
                viewModel.DeleteBookmarksCommand.Execute(null);

                // Verify
                if (nullSelectedItem)
                {
                    Assert.AreEqual(bookmark, bookmarkEventViewItem.HistoryEvent.Bookmark);
                }
                else
                {
                    foreach (HistoryEvent child in childEvents)
                    {
                        Assert.AreEqual(parentEvent, child.Parent);
                        Assert.True(parentEvent.Children.Contains(child));
                    }
                }
            }
        }

        [Test]
        public void DeleteBranch([Values(false, true)] bool nullSelectedItem)
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
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
            using (LocalMachine machine = CreateMachineWithHistory())
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
            using (LocalMachine machine = CreateMachineWithHistory())
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

        [Test]
        public void DeleteCurrent()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, null);
                HistoryEvent historyEvent = machine.History.CurrentEvent;

                // Act
                viewModel.TrimTimeline(historyEvent);

                // Verify
                Assert.AreEqual(historyEvent, machine.History.CurrentEvent);
            }
        }

        // This test (and possibly all of these tests) should be re-written to make
        // it clear what they're testing!
        [Test]
        public void DeleteNonCurrent()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine, null);
                HistoryEvent historyEvent = machine.History.CurrentEvent;
                machine.JumpToBookmark(machine.History.RootEvent.Children[0].Children[0]);

                // Act
                viewModel.TrimTimeline(historyEvent);

                // Verify
                Assert.AreEqual(1, machine.History.RootEvent.Children[0].Children[0].Children[1].Children[0].Children.Count);
            }
        }
    }
}
