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
    }
}
