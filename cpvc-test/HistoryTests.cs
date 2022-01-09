using Moq;
using NUnit.Framework;
using System;

namespace CPvC.Test
{
    [TestFixture]
    public class HistoryTests
    {
        private History _history;
        private HistoryEvent _event0;
        private HistoryEvent _event00;
        private HistoryEvent _event01;
        private HistoryEvent _event010;

        [SetUp]
        public void SetUp()
        {
            _history = new History();
            _event0 = _history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            _event00 = _history.AddCoreAction(CoreAction.KeyPress(200, 12, true));
            _history.CurrentEvent = _event0;
            _event01 = _history.AddCoreAction(CoreAction.Reset(300));
            _event010 = _history.AddBookmark(400, new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 }));
        }

        [Test]
        public void Root()
        {
            // Setup
            History history = new History();

            // Verify
            Assert.IsInstanceOf<RootHistoryEvent>(history.RootEvent);
        }

        [Test]
        public void EndTicksReset()
        {
            // Setup
            HistoryEvent historyEvent = _history.AddCoreAction(CoreAction.Reset(100));

            // Verify
            Assert.AreEqual(100, historyEvent.Ticks);
            Assert.AreEqual(100, historyEvent.EndTicks);
        }

        [Test]
        public void EndTicksKeyPress()
        {
            // Setup
            HistoryEvent historyEvent = _history.AddCoreAction(CoreAction.KeyPress(100, 42, true));

            // Verify
            Assert.AreEqual(100, historyEvent.Ticks);
            Assert.AreEqual(100, historyEvent.EndTicks);
        }

        [Test]
        public void EndTicksRunUntil()
        {
            // Setup
            HistoryEvent historyEvent = _history.AddCoreAction(CoreAction.RunUntil(100, 200, null));

            // Verify
            Assert.AreEqual(100, historyEvent.Ticks);
            Assert.AreEqual(200, historyEvent.EndTicks);
        }

        [Test]
        public void AddEvent()
        {
            // Setup
            History history = new History();

            // Act
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            HistoryEvent event2 = history.AddCoreAction(CoreAction.KeyPress(200, 12, true));

            // Verify
            Assert.AreEqual(1, history.RootEvent.Children.Count);
            Assert.AreEqual(event1, history.RootEvent.Children[0]);
            Assert.AreEqual(1, event1.Children.Count);
            Assert.AreEqual(event2, event1.Children[0]);
            Assert.AreEqual(0, event2.Children.Count);
        }

        [Test]
        public void CollapseRunUntilActions()
        {
            // Setup
            History history = new History();

            // Act
            CoreActionHistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            CoreActionHistoryEvent event2 = history.AddCoreAction(CoreAction.RunUntil(200, 300, null));

            // Verify
            Assert.AreEqual(1, history.RootEvent.Children.Count);
            Assert.AreEqual(event1, history.RootEvent.Children[0]);
            Assert.AreEqual(0, event1.Children.Count);
            Assert.AreEqual(event1, event2);
            Assert.AreEqual(CoreAction.Types.RunUntil, event1.CoreAction.Type);
            Assert.AreEqual(100, event1.CoreAction.Ticks);
            Assert.AreEqual(300, event1.CoreAction.StopTicks);
        }

        [Test]
        public void SetCurrentEvent()
        {
            // Act
            _history.CurrentEvent = _event01;

            // Verify
            Assert.AreEqual(_event01, _history.CurrentEvent);
        }

        [Test]
        public void SetRootCurrentEvent()
        {
            // Act
            _history.CurrentEvent = _history.RootEvent;

            // Verify
            Assert.AreEqual(_history.RootEvent, _history.CurrentEvent);
        }

        [Test]
        public void SetCurrentEventToCurrentEvent()
        {
            // Setup
            Mock<History.HistoryEventDelegate> mockAuditor = new Mock<History.HistoryEventDelegate>(MockBehavior.Loose);
            _history.Auditors += mockAuditor.Object;

            // Act
            _history.CurrentEvent = _history.CurrentEvent;

            // Verify
            mockAuditor.VerifyNoOtherCalls();
        }

        [Test]
        public void SetBookmark()
        {
            // Setup
            History history = new History();
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            HistoryEvent event2 = history.AddCoreAction(CoreAction.KeyPress(200, 12, true));

            // Act
            HistoryEvent event3 = history.AddBookmark(200, new Bookmark(false, Core.LatestVersion, new byte[] { }, new byte[] { }));

            // Verify
            Assert.AreEqual(event3, history.CurrentEvent);
            Assert.IsInstanceOf<BookmarkHistoryEvent>(event3);
        }

        [Test]
        public void SetBookmarkBadTicks()
        {
            // Setup
            History history = new History();
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));

            // Act and Verify
            Assert.Throws<Exception>(() => history.AddBookmark(99, new Bookmark(false, Core.LatestVersion, new byte[] { }, new byte[] { })));
        }

        [Test]
        public void DeleteCurrentAndChildren()
        {
            // Act
            bool result = _history.DeleteBranch(_history.CurrentEvent);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteEventAndChildren()
        {
            // Setup
            _history.CurrentEvent = _event0;

            // Act
            bool result = _history.DeleteBranch(_event01);

            // Verify
            Assert.True(result);
            Assert.AreEqual(1, _event0.Children.Count);
        }

        [Test]
        public void DeleteBranchAncestorOfCurrent()
        {
            // Act
            bool result = _history.DeleteBranch(_history.CurrentEvent.Parent);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteBranchCurrent()
        {
            // Act
            bool result = _history.DeleteBranch(_history.CurrentEvent);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteRootAndChildren()
        {
            // Act and Verify
            Assert.Throws<Exception>(() => _history.DeleteBranch(_history.RootEvent));
        }

        [Test]
        public void DeleteBookmark()
        {
            // Act
            bool result = _history.DeleteBookmark(_event01);

            // Verify
            Assert.True(result);
            Assert.AreEqual(2, _event0.Children.Count);
            Assert.AreEqual(_event0.Children[1], _event010);
            Assert.AreEqual(_event0, _event010.Parent);
        }

        [Test]
        public void DeleteBookmarkCurrent()
        {
            // Act
            bool result = _history.DeleteBookmark(_history.CurrentEvent);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteBookmarkRoot()
        {
            // Act and Verify
            Assert.Throws<Exception>(() => _history.DeleteBookmark(_history.RootEvent));
        }

        [Test]
        public void DeleteEventNotOurNode()
        {
            // Setup
            History otherHistory = new History();

            // Act and Verify
            Assert.Throws<Exception>(() => _history.DeleteBookmark(otherHistory.CurrentEvent));
        }

        [Test]
        public void DeleteEventAndChildrenNotOurNode()
        {
            // Setup
            History otherHistory = new History();

            // Act and Verify
            Assert.Throws<Exception>(() => _history.DeleteBranch(otherHistory.CurrentEvent));
        }

        [Test]
        public void SetCurrentNotOurNode()
        {
            // Setup
            History otherHistory = new History();

            // Act and Verify
            Assert.Throws<Exception>(() => _history.CurrentEvent = otherHistory.CurrentEvent);
        }

        [Test]
        public void EqualOrAncestor()
        {
            // Verify
            Assert.True(_event01.IsEqualToOrAncestorOf(_event01));
            Assert.True(_event01.IsEqualToOrAncestorOf(_event010));
            Assert.False(_event01.IsEqualToOrAncestorOf(_event0));
            Assert.False(_event01.IsEqualToOrAncestorOf(null));
        }

        [Test]
        public void RootLine()
        {
            // Verify
            Assert.Throws<NotImplementedException>(() => _history.RootEvent.GetLine());
        }
    }
}
