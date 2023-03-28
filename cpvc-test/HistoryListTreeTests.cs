using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class HistoryListTreeTests
    {
        [SetUp]
        public void Setup()
        {
            _history = new History();
            _historyListTree = new HistoryListTree(_history);
        }

        [Test]
        public void AddHistoryEvent()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true));

            // Verify
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event1);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event1);
        }

        [Test]
        public void AddHistoryEvents()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true));
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, false));

            // Verify
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event2);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event2);
        }

        [Test]
        public void AddBranchedEvent()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            HistoryEvent event2 = _history.AddBookmark(200, null);
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(400, Keys.A, true));
            _history.CurrentEvent = event2;
            HistoryEvent event4 = _history.AddCoreAction(new KeyPressAction(300, Keys.B, true));

            // Verify
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event2, event3, event4);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event2, event4, event3);
        }

        [Test]
        public void AddBranchedEventTie()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            HistoryEvent event2 = _history.AddBookmark(200, null);
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, true));
            _history.CurrentEvent = event2;
            HistoryEvent event4 = _history.AddCoreAction(new RunUntilAction(200, 300, null));

            // Verify - note the Id is used in the event of a tie.
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event2, event4, event3);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event2, event3, event4);
        }

        [Test]
        public void AddBranchedEventBreakTie1()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            HistoryEvent event2 = _history.AddBookmark(200, null);
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, true));
            _history.CurrentEvent = event2;
            HistoryEvent event4 = _history.AddCoreAction(new RunUntilAction(200, 300, null));
            HistoryEvent event5 = _history.AddCoreAction(new RunUntilAction(300, 304, null));

            // Verify - note the Id is used in the event of a tie.
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event2, event5, event3);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event2, event3, event5);
        }

        [Test]
        public void DeleteBookmarkParentBecomesVisible()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            HistoryEvent event2 = _history.AddBookmark(200, null);
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, true));
            _history.CurrentEvent = event2;
            HistoryEvent event4 = _history.AddCoreAction(new RunUntilAction(200, 400, null));
            _history.DeleteBookmark(event2);

            // Verify - note the Id is used in the event of a tie.
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event1, event4, event3);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event1, event3, event4);
        }

        [Test]
        public void DeleteBookmarkParentAlreadyVisible()
        {
            // Act
            HistoryEvent event1 = _history.AddBookmark(100, null);
            HistoryEvent event2 = _history.AddBookmark(200, null);
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, true));
            _history.CurrentEvent = event2;
            HistoryEvent event4 = _history.AddCoreAction(new RunUntilAction(200, 400, null));
            _history.DeleteBookmark(event2);

            // Verify - note the Id is used in the event of a tie.
            VerifyOrderings(_historyListTree.HorizontalOrdering(), _history.RootEvent, event1, event4, event3);
            VerifyOrderings(_historyListTree.VerticalOrdering(), _history.RootEvent, event1, event3, event4);
        }

        private void VerifyOrderings(List<ListTreeNode<HistoryEvent>> actualOrdering, params object[] expectedOrdering)
        {
            Assert.AreEqual(actualOrdering.Select(x => x.Data), expectedOrdering);
        }

        private History _history;
        private HistoryListTree _historyListTree;
    }
}
