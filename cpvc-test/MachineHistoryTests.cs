using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class MachineHistoryTests
    {
        [Test]
        public void AddEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            HistoryEvent event2 = HistoryEvent.CreateCheckpoint(2, 100, DateTime.UtcNow, null);

            // Act
            history.AddEvent(event1);
            history.AddEvent(event2);

            // Verify
            Assert.AreEqual(event1, history.RootEvent);
            Assert.AreEqual(1, event1.Children.Count);
        }

        [Test]
        public void AddParentCheckpoint()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            HistoryEvent event2 = HistoryEvent.CreateCheckpoint(2, 100, DateTime.UtcNow, null);
            HistoryEvent event3 = HistoryEvent.CreateCheckpoint(3, 50, DateTime.UtcNow, null);

            // Act
            history.AddEvent(event1);
            history.AddEvent(event2);
            history.AddEvent(event3);

            // Verify
            Assert.AreEqual(event1, history.RootEvent);
            Assert.AreEqual(1, event1.Children.Count);
            Assert.AreEqual(1, event3.Children.Count);
            Assert.AreEqual(0, event2.Children.Count);
            Assert.AreEqual(event3, event2.Parent);
            Assert.AreEqual(event1, event3.Parent);
            Assert.AreEqual(event3, event1.Children[0]);
            Assert.AreEqual(event2, event3.Children[0]);
        }

        [Test]
        public void DeleteEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            HistoryEvent event2 = HistoryEvent.CreateCheckpoint(2, 50, DateTime.UtcNow, null);
            HistoryEvent event3 = HistoryEvent.CreateCheckpoint(3, 100, DateTime.UtcNow, null);
            history.AddEvent(event1);
            event1.AddChild(event2);
            event2.AddChild(event3);

            // Act
            bool result = history.DeleteEvent(event2);

            // Verify
            Assert.True(result);
            Assert.AreEqual(0, event1.Children.Count);
        }

        [Test]
        public void DeleteRootEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            history.AddEvent(event1);

            // Act
            bool result = history.DeleteEvent(event1);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteRootEventById()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            history.AddEvent(event1);

            // Act
            bool result = history.DeleteEvent(1);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void SetCurrentEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            HistoryEvent event2 = HistoryEvent.CreateCheckpoint(2, 50, DateTime.UtcNow, null);
            history.AddEvent(event1);
            history.AddEvent(event2);

            // Act
            history.SetCurrentEvent(1);

            // Verify
            Assert.AreEqual(event1, history.CurrentEvent);
        }

        [Test]
        public void SetBookmark()
        {
            // Setup
            Bookmark bookmark = new Bookmark(false, 1, (byte[])null, (byte[])null);
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = HistoryEvent.CreateCheckpoint(1, 1, DateTime.UtcNow, null);
            history.AddEvent(event1);

            // Act
            history.SetBookmark(1, bookmark);

            // Verify
            Assert.AreEqual(bookmark, event1.Bookmark);
        }
    }
}
