using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace CPvC.Test
{
    public class HistoryEventOrderingTests
    {
        [SetUp]
        public void Setup()
        {
            _history = new History();
            _historyListTree = new HistoryEventOrdering(_history);
        }

        [Test]
        public void AddHistoryEvent()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true));

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event1);
            VerifyVerticalOrderings(_history.RootEvent, event1);
        }

        [Test]
        public void AddHistoryEvents()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true));
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, false));

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event2);
            VerifyVerticalOrderings(_history.RootEvent, event2);
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
            VerifyHorizontalOrderings(_history.RootEvent, event2, event3, event4);
            VerifyVerticalOrderings(_history.RootEvent, event2, event4, event3);
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
            VerifyHorizontalOrderings(_history.RootEvent, event2, event4, event3);
            VerifyVerticalOrderings(_history.RootEvent, event2, event3, event4);
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
            VerifyHorizontalOrderings(_history.RootEvent, event2, event5, event3);
            VerifyVerticalOrderings(_history.RootEvent, event2, event3, event5);
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
            VerifyHorizontalOrderings(_history.RootEvent, event1, event4, event3);
            VerifyVerticalOrderings(_history.RootEvent, event1, event3, event4);
        }

        [Test]
        public void DeleteBookmarkParentBecomesInvisible()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true));
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, false));
            _history.CurrentEvent = event1;
            HistoryEvent event3 = _history.AddBookmark(200, null);
            _history.CurrentEvent = event2; // Switch to a different event since we can't delete event3 while it's the current event.

            _history.DeleteBookmark(event3);

            // Verify - note the Id is used in the event of a tie.
            VerifyHorizontalOrderings(_history.RootEvent, event2);
            VerifyVerticalOrderings(_history.RootEvent, event2);
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
            VerifyHorizontalOrderings(_history.RootEvent, event1, event4, event3);
            VerifyVerticalOrderings(_history.RootEvent, event1, event3, event4);
        }

        [Test]
        public void NonBookmarkIsVisible()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, true));
            _history.CurrentEvent = event1;
            HistoryEvent event3 = _history.AddCoreAction(new RunUntilAction(200, 400, null));

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event1, event3, event2);
            VerifyVerticalOrderings(_history.RootEvent, event1, event2, event3);
        }

        [Test]
        public void DeleteChildMakesParentInvisible()
        {
            // Act
            HistoryEvent event1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, true));
            _history.CurrentEvent = event1;
            HistoryEvent event3 = _history.AddCoreAction(new RunUntilAction(200, 400, null));
            _history.DeleteBranch(event2);

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event3);
            VerifyVerticalOrderings(_history.RootEvent, event3);
        }

        [Test]
        public void DeleteBookmark()
        {
            // Act
            HistoryEvent event1 = _history.AddBookmark(100, null);
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
            HistoryEvent event3 = _history.AddBookmark(300, null);
            HistoryEvent event4 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, false));
            _history.DeleteBookmark(event3);

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event1, event4);
            VerifyVerticalOrderings(_history.RootEvent, event1, event4);
        }

        [Test]
        public void DeleteBranchWithNoDescendents()
        {
            // Act
            HistoryEvent event1 = _history.AddBookmark(100, null);
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
            _history.CurrentEvent = event1;
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, false));
            _history.DeleteBranch(event2);

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event1, event3);
            VerifyVerticalOrderings(_history.RootEvent, event1, event3);
        }

        [Test]
        public void DeleteBranchWithOneDescendent()
        {
            // Act
            HistoryEvent event1 = _history.AddBookmark(100, null);
            HistoryEvent event2 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
            HistoryEvent event3 = _history.AddBookmark(300, null);

            _history.CurrentEvent = event1;
            HistoryEvent event4 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, false));
            _history.DeleteBranch(event2);

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event1, event4);
            VerifyVerticalOrderings(_history.RootEvent, event1, event4);
        }

        [Test]
        public void DeleteBranchWithMultipleDescendents()
        {
            // Act
            HistoryEvent event1 = _history.AddBookmark(100, null);
            HistoryEvent event2 = _history.AddBookmark(200, null);
            HistoryEvent event3 = _history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
            _history.CurrentEvent = event2;
            HistoryEvent event4 = _history.AddCoreAction(new KeyPressAction(300, Keys.A, false));

            _history.CurrentEvent = event1;
            HistoryEvent event5 = _history.AddCoreAction(new KeyPressAction(200, Keys.B, false));
            _history.DeleteBranch(event2);

            // Verify
            VerifyHorizontalOrderings(_history.RootEvent, event1, event5);
            VerifyVerticalOrderings(_history.RootEvent, event1, event5);
        }

        private void VerifyHorizontalOrderings(params object[] expectedOrdering)
        {
            List<InterestingEvent> horizontalInterestingEvents = _historyListTree.HorizontalInterestingEvents;
            Assert.AreEqual(expectedOrdering, horizontalInterestingEvents.Select(x => x.HistoryEvent));
        }

        private void VerifyVerticalOrderings(params object[] expectedOrdering)
        {
            InterestingEvent[] verticalEvents = new InterestingEvent[expectedOrdering.Length];

            foreach (InterestingEvent ie in _historyListTree.HorizontalInterestingEvents)
            {
                verticalEvents[ie.VerticalIndex] = ie;
            }

            Assert.AreEqual(expectedOrdering, verticalEvents.Select(x => x.HistoryEvent));
        }

        private History _history;
        private HistoryEventOrdering _historyListTree;
    }
}
