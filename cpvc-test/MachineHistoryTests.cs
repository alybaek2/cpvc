using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    [TestFixture]
    public class MachineHistoryTests
    {
        private MachineHistory _history;
        private HistoryEvent _event0;
        private HistoryEvent _event00;
        private HistoryEvent _event01;
        private HistoryEvent _event010;

        //private MachineHistory _historyCopy;

        [SetUp]
        public void SetUp()
        {
            _history = new MachineHistory();
            _event0 = _history.AddCoreAction(CoreAction.RunUntil(100, 200, null));
            _event00 = _history.AddCoreAction(CoreAction.KeyPress(200, 12, true));
            _history.SetCurrent(_event0);
            _event01 = _history.AddCoreAction(CoreAction.Reset(300));
            _event010 = _history.AddBookmark(400, new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 }));

            //_historyCopy = new MachineHistory();
            //_history.CopyTo(_historyCopy);
        }

        [Test]
        public void Root()
        {
            // Setup
            MachineHistory history = new MachineHistory();

            // Verify
            Assert.AreEqual(HistoryEventType.Root, history.RootEvent.Type);
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
            // Act
            _history.SetCurrent(_event01);

            // Verify
            Assert.AreEqual(_event01, _history.CurrentEvent);
        }

        [Test]
        public void SetRootCurrentEvent()
        {
            // Act
            _history.SetCurrent(_history.RootEvent);

            // Verify
            Assert.AreEqual(_history.RootEvent, _history.CurrentEvent);
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
            Assert.AreEqual(HistoryEventType.Bookmark, event3.Type);
        }

        //[Test]
        //public void Copy()
        //{
        //    // Verify
        //    Assert.True(TestHelpers.HistoriesEqual(_history, _historyCopy));
        //}

        [Test]
        public void DeleteCurrentAndChildren()
        {
            // Act
            bool result = _history.DeleteEventAndChildren(_history.CurrentEvent);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteEventAndChildren()
        {
            // Setup
            _history.SetCurrent(_event0);

            // Act
            bool result = _history.DeleteEventAndChildren(_event01);

            // Verify
            Assert.True(result);
            Assert.AreEqual(1, _event0.Children.Count);
        }

        [Test]
        public void DeleteRootAndChildren()
        {
            // Act and Verify
            Assert.Throws<Exception>(() => _history.DeleteEventAndChildren(_history.RootEvent));
        }

        [Test]
        public void DeleteEvent()
        {
            // Act
            bool result = _history.DeleteEvent(_event01);

            // Verify
            Assert.True(result);
            Assert.AreEqual(2, _event0.Children.Count);
            Assert.AreEqual(_event0.Children[1], _event010);
            Assert.AreEqual(_event0, _event010.Parent);
        }

        [Test]
        public void DeleteCurrent()
        {
            // Act
            bool result = _history.DeleteEvent(_history.CurrentEvent);

            // Verify
            Assert.False(result);
        }

        [Test]
        public void DeleteRoot()
        {
            // Act and Verify
            Assert.Throws<Exception>(() => _history.DeleteEvent(_history.RootEvent));
        }

        //[Test]
        //public void DeleteEventNotOurNode()
        //{
        //    // Act and Verify
        //    Assert.Throws<Exception>(() => _history.DeleteEvent(_historyCopy.CurrentEvent));
        //}

        //[Test]
        //public void DeleteEventAndChildrenNotOurNode()
        //{
        //    // Act and Verify
        //    Assert.Throws<Exception>(() => _history.DeleteEventAndChildren(_historyCopy.CurrentEvent));
        //}

        //[Test]
        //public void SetCurrentNotOurNode()
        //{
        //    // Act and Verify
        //    Assert.Throws<Exception>(() => _history.SetCurrent(_historyCopy.CurrentEvent));
        //}
    }
}
