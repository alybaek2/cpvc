using CPvC.UI;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    // These tests need to run on the main thread due to the fact that the Canvas member of HistoryViewItem is a
    // UI component. Should probably figure out a way to separate the UI components. An ItemTemplate perhaps?
    [Apartment(ApartmentState.STA)]
    public class BookmarksViewModelTests
    {
        private Mock<IFileSystem> _mockFileSystem;

        private History _history;
        private BookmarksViewModel _viewModel;

        private HistoryViewItem _rootViewItem;
        private HistoryViewItem _bookmark2ViewItem;
        private HistoryViewItem _bookmark1ViewItem;
        private HistoryViewItem _bookmark3ViewItem;
        private HistoryViewItem _leaf1ViewItem;
        private HistoryViewItem _leaf2ViewItem;
        private HistoryViewItem _leaf3ViewItem;

        HistoryEvent _bookmark1Event;
        HistoryEvent _leaf1Event;
        HistoryEvent _bookmark2Event;
        HistoryEvent _leaf2Event;
        HistoryEvent _bookmark3Event;
        HistoryEvent _leaf3Event;

        [SetUp]
        public void Setup()
        {
            MockTextFile mockTextFile = new MockTextFile();
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(AnyString())).Returns(mockTextFile);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(ReadBytes()).Returns(new byte[1]);

            _history = new History();

            _bookmark1Event = _history.AddBookmark(100, new Bookmark(false, 0, null, null));
            _history.AddCoreAction(CoreAction.RunUntil(100, 400, null));
            _leaf1Event = _history.AddCoreAction(CoreAction.KeyPress(400, 42, true));
            _history.CurrentEvent = _bookmark1Event;
            _bookmark2Event = _history.AddBookmark(200, new Bookmark(false, 0, null, null));
            _history.AddCoreAction(CoreAction.RunUntil(200, 300, null));
            _leaf2Event = _history.AddCoreAction(CoreAction.KeyPress(300, 42, true));
            _history.CurrentEvent = _bookmark2Event;
            _history.AddCoreAction(CoreAction.KeyPress(300, 42, true));
            _history.AddCoreAction(CoreAction.KeyPress(400, 42, false));
            _bookmark3Event = _history.AddBookmark(500, new Bookmark(false, 0, null, null));
            _history.CurrentEvent = _history.RootEvent;
            _leaf3Event = _history.AddCoreAction(CoreAction.KeyPress(50, 42, true));
            _history.CurrentEvent = _bookmark3Event;


            // Diagram of this history...
            // 
            // 500: o
            // 400: |   |
            // 300: | | |
            // 200: o-/ |
            // 100: o---/
            //  50  |     |
            //   0: o-----/

            _viewModel = new BookmarksViewModel(_history);

            Assert.AreEqual(7, _viewModel.Items.Count);

            _bookmark3ViewItem = _viewModel.Items[0];
            _leaf1ViewItem = _viewModel.Items[1];
            _leaf2ViewItem = _viewModel.Items[2];
            _bookmark2ViewItem = _viewModel.Items[3];
            _bookmark1ViewItem = _viewModel.Items[4];
            _leaf3ViewItem = _viewModel.Items[5];
            _rootViewItem = _viewModel.Items[6];
        }

        [TearDown]
        public void Teardown()
        {
            _mockFileSystem = null;
        }

        // Checks that the items are as expected
        [Test]
        public void Items()
        {
            // Verify
            Assert.AreEqual(7, _viewModel.Items.Count);
            Assert.AreEqual(_history.RootEvent, _rootViewItem.HistoryEvent);
            Assert.AreEqual(_bookmark1Event, _bookmark1ViewItem.HistoryEvent);
            Assert.AreEqual(_bookmark2Event, _bookmark2ViewItem.HistoryEvent);
            Assert.AreEqual(_bookmark3Event, _bookmark3ViewItem.HistoryEvent);
            Assert.AreEqual(_leaf1Event, _leaf1ViewItem.HistoryEvent);
            Assert.AreEqual(_leaf2Event, _leaf2ViewItem.HistoryEvent);
            Assert.AreEqual(_leaf3Event, _leaf3ViewItem.HistoryEvent);
        }

        [Test]
        public void DeleteBookmarkNoSelection()
        {
            // Setup
            Mock<History.HistoryEventDelegate> mockAuditor = new Mock<History.HistoryEventDelegate>(MockBehavior.Loose);
            _history.Auditors += mockAuditor.Object;

            // Act
            _viewModel.DeleteBookmarksCommand.Execute(null);

            // Verify
            mockAuditor.VerifyNoOtherCalls();
        }

        [Test]
        public void DeleteBookmark()
        {
            // Setup
            _viewModel.AddSelectedItem(_bookmark2ViewItem);

            // Act
            _viewModel.DeleteBookmarksCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(_bookmark2ViewItem));
        }

        [Test]
        public void DeleteBookmarkMultiple()
        {
            // Setup
            _viewModel.AddSelectedItem(_bookmark1ViewItem);
            _viewModel.AddSelectedItem(_bookmark2ViewItem);

            // Act
            _viewModel.DeleteBookmarksCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(_bookmark1ViewItem));
            Assert.False(_viewModel.Items.Contains(_bookmark2ViewItem));
        }

        [Test]
        public void DeleteBookmarkMixedSelection()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_bookmark2ViewItem);

            // Act
            _viewModel.DeleteBookmarksCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(_bookmark2ViewItem));
        }

        [Test]
        public void CanDeleteNonBookmark()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.False(_viewModel.DeleteBookmarksCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteNonBookmarkMultipleSelected()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_leaf2ViewItem);

            // Verify
            Assert.False(_viewModel.DeleteBookmarksCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteNonBookmarkMixedSelection()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_bookmark2ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBookmarksCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBookmarkNothingSelected()
        {
            // Verify
            Assert.False(_viewModel.DeleteBookmarksCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBookmark()
        {
            // Setup
            _history.CurrentEvent = _leaf1Event;
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBookmarksCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBookmarkMultipleSelected()
        {
            // Setup
            _history.CurrentEvent = _leaf1Event;
            _viewModel.AddSelectedItem(_bookmark2ViewItem);
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBookmarksCommand.CanExecute(null));
        }

        [Test]
        public void DeleteBranchNoSelection()
        {
            // Setup
            Mock<History.HistoryEventDelegate> mockAuditor = new Mock<History.HistoryEventDelegate>(MockBehavior.Loose);
            _history.Auditors += mockAuditor.Object;

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            mockAuditor.VerifyNoOtherCalls();
        }

        [Test]
        public void DeleteBranch()
        {
            // Setup
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(_leaf1ViewItem));
        }

        [Test]
        public void DeleteBranchFromCurrent()
        {
            // Setup
            _history.CurrentEvent = _bookmark2Event;
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(_leaf1ViewItem));
        }

        [Test]
        public void DeleteBranchMultiple()
        {
            // Setup
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_leaf3ViewItem);

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            Assert.False(_viewModel.Items.Contains(_leaf1ViewItem));
            Assert.False(_viewModel.Items.Contains(_leaf3ViewItem));
        }

        [Test]
        public void DeleteBranchNonDeletable()
        {
            // Setup
            Mock<History.HistoryEventDelegate> mockAuditor = new Mock<History.HistoryEventDelegate>(MockBehavior.Loose);
            _history.Auditors += mockAuditor.Object;

            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_rootViewItem);
            _viewModel.AddSelectedItem(_bookmark2ViewItem);
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            mockAuditor.VerifyNoOtherCalls();
            Assert.True(_viewModel.Items.Contains(_rootViewItem));
            Assert.True(_viewModel.Items.Contains(_bookmark2ViewItem));
            Assert.True(_viewModel.Items.Contains(_bookmark3ViewItem));
        }

        [Test]
        public void CanDeleteBranch()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBranchMultipleSelected()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_leaf2ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBranchMixedSelection()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBranchDescendant()
        {
            // Setup
            _history.CurrentEvent = _bookmark1Event;
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.True(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBranchMultipleChildren()
        {
            // Setup
            _history.CurrentEvent = _bookmark1Event;
            _viewModel.AddSelectedItem(_bookmark2ViewItem);

            // Verify
            Assert.False(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBranchNoSelection()
        {
            // Verify
            Assert.False(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void CanDeleteBranchCurrent()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Verify
            Assert.False(_viewModel.DeleteBranchesCommand.CanExecute(null));
        }

        [Test]
        public void DeleteBranchCurrent()
        {
            // Setup
            _history.CurrentEvent = _bookmark3Event;
            _viewModel.AddSelectedItem(_bookmark3ViewItem);

            // Act
            _viewModel.DeleteBranchesCommand.Execute(null);

            // Verify
            Assert.True(_viewModel.Items.Contains(_bookmark3ViewItem));
        }

        [Test]
        public void AddSelectedItem()
        {
            // Act
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.AreEqual(1, _viewModel.SelectedItems.Count(i => i == _leaf1ViewItem));
        }

        [Test]
        public void AddSelectedItemTwice()
        {
            // Setup
            HistoryViewItem branchViewItem = _leaf1ViewItem;

            // Act
            _viewModel.AddSelectedItem(_leaf1ViewItem);
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.AreEqual(1, _viewModel.SelectedItems.Count(i => i == _leaf1ViewItem));
        }

        [Test]
        public void RemoveSelectedItem()
        {
            // Setup
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Act
            _viewModel.RemoveSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.AreEqual(0, _viewModel.SelectedItems.Count(i => i == _leaf1ViewItem));
        }

        [Test]
        public void RemoveSelectedItemTwice()
        {
            // Setup
            _viewModel.AddSelectedItem(_leaf1ViewItem);

            // Act
            _viewModel.RemoveSelectedItem(_leaf1ViewItem);
            _viewModel.RemoveSelectedItem(_leaf1ViewItem);

            // Verify
            Assert.AreEqual(0, _viewModel.SelectedItems.Count(i => i == _leaf1ViewItem));
        }
    }
}
