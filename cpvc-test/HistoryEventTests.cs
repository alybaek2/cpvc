using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class HistoryEventTests
    {
        [Test]
        public void AddSameChildTwice()
        {
            // Setup
            HistoryEvent parent = HistoryEvent.CreateCheckpoint(1, 100, DateTime.Now, null);
            HistoryEvent child = HistoryEvent.CreateCheckpoint(2, 100, DateTime.Now, null);

            // Act
            parent.AddChild(child);
            parent.AddChild(child);

            // Verify
            Assert.AreEqual(1, parent.Children.Count);
        }

        [Test]
        public void RemoveSameChildTwice()
        {
            // Setup
            HistoryEvent parent = HistoryEvent.CreateCheckpoint(1, 100, DateTime.Now, null);
            HistoryEvent parent2 = HistoryEvent.CreateCheckpoint(1, 100, DateTime.Now, null);
            HistoryEvent child = HistoryEvent.CreateCheckpoint(2, 100, DateTime.Now, null);
            parent.AddChild(child);

            // Act
            parent.RemoveChild(child);
            parent2.AddChild(child);
            parent.RemoveChild(child);

            // Verify - check removing the child the second time didn't affect the child's link to its new parent.
            Assert.IsEmpty(parent.Children);
            Assert.AreEqual(parent2, child.Parent);
        }

        [Test]
        public void CheckAncestry()
        {
            // Setup
            HistoryEvent grandparent = HistoryEvent.CreateCheckpoint(1, 100, DateTime.Now, null);
            HistoryEvent parent = HistoryEvent.CreateCheckpoint(2, 100, DateTime.Now, null);
            HistoryEvent child = HistoryEvent.CreateCheckpoint(3, 100, DateTime.Now, null);
            HistoryEvent child2 = HistoryEvent.CreateCheckpoint(4, 100, DateTime.Now, null);
            parent.AddChild(child);
            parent.AddChild(child2);
            grandparent.AddChild(parent);

            // Verify
            Assert.IsFalse(child.IsEqualToOrAncestorOf(parent));
            Assert.IsFalse(child.IsEqualToOrAncestorOf(grandparent));
            Assert.IsFalse(parent.IsEqualToOrAncestorOf(grandparent));
            Assert.IsTrue(parent.IsEqualToOrAncestorOf(child));
            Assert.IsTrue(grandparent.IsEqualToOrAncestorOf(parent));
            Assert.IsTrue(grandparent.IsEqualToOrAncestorOf(child));
            Assert.IsTrue(parent.IsEqualToOrAncestorOf(parent));
            Assert.IsFalse(parent.IsEqualToOrAncestorOf(null));
            Assert.IsFalse(child.IsEqualToOrAncestorOf(child2));
        }

        [Test]
        public void CloneCoreAction()
        {
            // Setup
            CoreAction coreAction = CoreAction.Reset(100);
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(0, coreAction);

            // Act
            HistoryEvent clone = historyEvent.CloneWithoutChildren();

            // Verify
            Assert.IsNotNull(clone);
            Assert.AreEqual(100, clone.Ticks);
            Assert.Zero(clone.Children.Count);
            Assert.AreEqual(coreAction.Type, clone.CoreAction.Type);
        }

        [Test]
        public void CloneCheckpoint()
        {
            // Setup
            Bookmark bookmark = new Bookmark(true, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 100, DateTime.UtcNow, bookmark);

            // Act
            HistoryEvent clone = historyEvent.CloneWithoutChildren();

            // Verify
            Assert.IsNotNull(clone);
            Assert.AreEqual(100, clone.Ticks);
            Assert.Zero(clone.Children.Count);
            Assert.AreEqual(bookmark.Screen.GetBytes(), clone.Bookmark.Screen.GetBytes());
            Assert.AreEqual(bookmark.State.GetBytes(), clone.Bookmark.State.GetBytes());
            Assert.AreEqual(bookmark.System, clone.Bookmark.System);
            Assert.AreEqual(bookmark.Version, clone.Bookmark.Version);
        }

        [Test]
        public void CloneCheckpointWithoutBookmark()
        {
            // Setup
            Bookmark bookmark = new Bookmark(true, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 100, DateTime.UtcNow, null);

            // Act
            HistoryEvent clone = historyEvent.CloneWithoutChildren();

            // Verify
            Assert.IsNotNull(clone);
            Assert.AreEqual(100, clone.Ticks);
            Assert.Zero(clone.Children.Count);
            Assert.Null(clone.Bookmark);
        }

        [Test]
        public void CloneInvalidType()
        {
            // Setup
            HistoryEvent historyEvent = new HistoryEvent(0, (HistoryEvent.Types)99, 100);

            // Act
            HistoryEvent clone = historyEvent.CloneWithoutChildren();

            // Verify
            Assert.Null(clone);
        }
    }
}
