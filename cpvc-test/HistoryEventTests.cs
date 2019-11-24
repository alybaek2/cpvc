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
            Assert.AreEqual(0, parent.Children.Count);
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
    }
}
