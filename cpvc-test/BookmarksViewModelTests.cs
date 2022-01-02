using CPvC.UI;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [Apartment(ApartmentState.STA)]
    public class BookmarksViewModelTests
    {
        private Mock<IFileSystem> _mockFileSystem;

        private LocalMachine _machine;
        private BookmarksViewModel _viewModel;

        private LocalMachine CreateMachineWithHistory()
        {
            LocalMachine machine = LocalMachine.New("test", null);
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

            _machine = CreateMachineWithHistory();
            _viewModel = new BookmarksViewModel(_machine);
        }

        [TearDown]
        public void Teardown()
        {
            _machine.Close();

            _mockFileSystem = null;
        }

        [Test]
        public void SetSelectedItemCurrent()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("test", null);
            RunForAWhile(machine);
            machine.AddBookmark(true);

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine);

            // The first item is the current event.
            viewModel.AddSelectedItem(viewModel.Items[0]);

            // Verify
            Assert.AreEqual(1, viewModel.SelectedItems.Count);
            Assert.AreEqual(machine.History.CurrentEvent, viewModel.SelectedItems[0].HistoryEvent);
            Assert.AreEqual(viewModel.Items[0], viewModel.SelectedItems[0]);
            Assert.IsTrue(viewModel.DeleteBookmarksCommand.CanExecute(null));
            Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void SetSelectedItemBookmark()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("test", null);
            RunForAWhile(machine);
            machine.AddBookmark(true);
            HistoryEvent bookmarkEvent = machine.History.CurrentEvent;
            RunForAWhile(machine);
            machine.AddBookmark(true);

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine);

            // The second item is the bookmark event.
            viewModel.AddSelectedItem(viewModel.Items[1]);

            // Verify
            Assert.AreEqual(1, viewModel.SelectedItems.Count);
            Assert.AreEqual(bookmarkEvent, viewModel.SelectedItems[0].HistoryEvent);
            Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItems[0]);
            Assert.IsTrue(viewModel.DeleteBookmarksCommand.CanExecute(null));
            Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void SetSelectedItemRoot()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("test", null);
            RunForAWhile(machine);
            machine.AddBookmark(true);

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine);

            // The last item is the root event.
            viewModel.AddSelectedItem(viewModel.Items[1]);

            // Verify
            Assert.AreEqual(1, viewModel.SelectedItems.Count);
            Assert.AreEqual(machine.History.RootEvent, viewModel.SelectedItems[0].HistoryEvent);
            Assert.AreEqual(viewModel.Items[1], viewModel.SelectedItems[0]);
            Assert.IsFalse(viewModel.DeleteBookmarksCommand.CanExecute(null));
            Assert.IsFalse(viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void SimpleHistory()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("test", null);
            RunForAWhile(machine);
            machine.Key(Keys.A, true);
            RunForAWhile(machine);
            machine.Key(Keys.A, false);
            RunForAWhile(machine);
            machine.AddBookmark(true);

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine);

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
            LocalMachine machine = LocalMachine.New("test", null);
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

            // Act
            BookmarksViewModel viewModel = new BookmarksViewModel(machine);

            // Verify
            Assert.AreEqual(6, viewModel.Items.Count);
            Assert.AreEqual(machine.History.CurrentEvent, viewModel.Items[0].HistoryEvent);
            Assert.AreEqual(event400, viewModel.Items[1].HistoryEvent);
            Assert.AreEqual(event300, viewModel.Items[2].HistoryEvent);
            Assert.AreEqual(event200, viewModel.Items[3].HistoryEvent);
            Assert.AreEqual(event100, viewModel.Items[4].HistoryEvent);
            Assert.AreEqual(machine.History.RootEvent, viewModel.Items[5].HistoryEvent);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeleteBookmark(bool hasSelectedItem)
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryViewItem bookmarkEventViewItem = viewModel.Items[3];
                Bookmark bookmark = (bookmarkEventViewItem.HistoryEvent as BookmarkHistoryEvent)?.Bookmark;

                if (hasSelectedItem)
                {
                    viewModel.AddSelectedItem(bookmarkEventViewItem);
                }

                HistoryEvent parentEvent = bookmarkEventViewItem.HistoryEvent.Parent;
                List<HistoryEvent> childEvents = new List<HistoryEvent>();
                childEvents.AddRange(bookmarkEventViewItem.HistoryEvent.Children);

                // Act
                viewModel.DeleteBookmarksCommand.Execute(null);

                // Verify
                if (!hasSelectedItem)
                {
                    Assert.AreEqual(bookmark, (bookmarkEventViewItem.HistoryEvent as BookmarkHistoryEvent)?.Bookmark);
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
        public void DeleteBranchNoSelection()
        {
            // Setup
            int historyEventsCount = 0;
            _machine.History.Auditors += (historyEvent, changeAction) =>
            {
                historyEventsCount++;
            };

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            Assert.Zero(historyEventsCount);
        }

        [Test]
        public void DeleteBranch()
        {
            // Setup
            HistoryViewItem branchViewItem = _viewModel.Items[1];
            HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;
            _viewModel.AddSelectedItem(branchViewItem);

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(branchViewItem));
            Assert.AreEqual(1, parentEvent.Children.Count);
        }

        [Test]
        public void CanDeleteBranch()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;
                viewModel.AddSelectedItem(branchViewItem);

                // Verify
                Assert.True(viewModel.DeleteBranchesCommand.CanExecute(null));
            }
        }

        [Test]
        public void DeleteCurrent()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryEvent historyEvent = machine.History.CurrentEvent;

                // Act
                viewModel.DeleteBranch(historyEvent);

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
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryEvent historyEvent = machine.History.CurrentEvent;
                machine.JumpToBookmark(machine.History.RootEvent.Children[0].Children[0]);

                // Act
                viewModel.DeleteBranch(historyEvent);

                // Verify
                Assert.AreEqual(1, machine.History.RootEvent.Children[0].Children[0].Children[1].Children[0].Children.Count);
            }
        }

        [Test]
        public void AddSelectedItem()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;

                // Act
                viewModel.AddSelectedItem(branchViewItem);

                // Verify
                Assert.AreEqual(1, viewModel.SelectedItems.Count(i => i == branchViewItem));
            }
        }

        [Test]
        public void AddSelectedItemTwice()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;

                // Act
                viewModel.AddSelectedItem(branchViewItem);
                viewModel.AddSelectedItem(branchViewItem);

                // Verify
                Assert.AreEqual(1, viewModel.SelectedItems.Count(i => i == branchViewItem));
            }
        }

        [Test]
        public void RemoveSelectedItem()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;
                viewModel.AddSelectedItem(branchViewItem);

                // Act
                viewModel.RemoveSelectedItem(branchViewItem);

                // Verify
                Assert.AreEqual(0, viewModel.SelectedItems.Count(i => i == branchViewItem));
            }
        }

        [Test]
        public void RemoveSelectedItemTwice()
        {
            // Setup
            using (LocalMachine machine = CreateMachineWithHistory())
            {
                BookmarksViewModel viewModel = new BookmarksViewModel(machine);
                HistoryViewItem branchViewItem = viewModel.Items[1];
                HistoryEvent parentEvent = branchViewItem.HistoryEvent.Parent;
                viewModel.AddSelectedItem(branchViewItem);

                // Act
                viewModel.RemoveSelectedItem(branchViewItem);
                viewModel.RemoveSelectedItem(branchViewItem);

                // Verify
                Assert.AreEqual(0, viewModel.SelectedItems.Count(i => i == branchViewItem));
            }
        }
    }
}
