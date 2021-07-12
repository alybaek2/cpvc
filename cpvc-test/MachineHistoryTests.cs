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
        public void Root()
        {
            // Setup
            MachineHistory history = new MachineHistory();

            // Verify
            Assert.AreEqual(HistoryEventType.None, history.RootEvent.Type);
        }

        [Test]
        public void AddEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();

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
        public void DeleteEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            HistoryEvent event2 = history.AddCoreAction(CoreAction.KeyPress(200, 12, true));
            HistoryEvent event3 = history.AddCoreAction(CoreAction.RunUntil(200, 300, null));

            // Act
            bool result = history.DeleteEventAndChildren(event2);

            // Verify
            Assert.True(result);
            Assert.AreEqual(0, event1.Children.Count);
        }

        [Test]
        public void DeleteRoot()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent root = history.RootEvent;

            // Act
            bool result = history.DeleteEventAndChildren(history.RootEvent);

            // Verify
            Assert.False(result);
            Assert.AreEqual(root, history.RootEvent);
        }

        [Test]
        public void CollapseRunUntilActions()
        {
            // Setup
            MachineHistory history = new MachineHistory();

            // Act
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            HistoryEvent event2 = history.AddCoreAction(CoreAction.RunUntil(200, 300, null));

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
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            HistoryEvent event2 = history.AddCoreAction(CoreAction.KeyPress(200, 12, true));
            HistoryEvent event3 = history.AddCoreAction(CoreAction.RunUntil(200, 300, null));

            // Act
            history.SetCurrent(event1);

            // Verify
            Assert.AreEqual(event1, history.CurrentEvent);
        }

        [Test]
        public void SetRootCurrentEvent()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));

            // Act
            history.SetCurrent(history.RootEvent);

            // Verify
            Assert.AreEqual(history.RootEvent, history.CurrentEvent);
        }

        [Test]
        public void SetBookmark()
        {
            // Setup
            MachineHistory history = new MachineHistory();
            HistoryEvent event1 = history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            HistoryEvent event2 = history.AddCoreAction(CoreAction.KeyPress(200, 12, true));

            // Act
            HistoryEvent event3 = history.AddBookmark(200, new Bookmark(false, Core.LatestVersion, new byte[] { }, new byte[] { }));

            // Verify
            Assert.AreEqual(event3, history.CurrentEvent);
            Assert.AreEqual(HistoryEventType.AddBookmark, event3.Type);
        }
    }
}
