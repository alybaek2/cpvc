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
        [Test]
        public void AddHistoryEvent()
        {
            // Setup
            History history = new History();
            HistoryListTree historyListTree = new HistoryListTree(history);

            // Act
            HistoryEvent event1 = history.AddCoreAction(new KeyPressAction(100, Keys.A, true));

            // Verify
            List<HistoryEvent> expectedOrdering = new List<HistoryEvent>
            {
                history.RootEvent,
                event1
            };

            VerifyOrderings(expectedOrdering, expectedOrdering, historyListTree);
        }

        [Test]
        public void AddHistoryEvents()
        {
            // Setup
            History history = new History();
            HistoryListTree historyListTree = new HistoryListTree(history);

            // Act
            HistoryEvent event1 = history.AddCoreAction(new KeyPressAction(100, Keys.A, true));
            HistoryEvent event2 = history.AddCoreAction(new KeyPressAction(200, Keys.A, false));

            // Verify
            List<HistoryEvent> expectedOrdering = new List<HistoryEvent>
            {
                history.RootEvent,
                event2
            };

            VerifyOrderings(expectedOrdering, expectedOrdering, historyListTree);
        }

        private void VerifyOrderings(IEnumerable<HistoryEvent> expectedHorizontalOrdering, IEnumerable<HistoryEvent> expectedVerticalOrdering, HistoryListTree historyListTree)
        {
            IEnumerable<HistoryEvent> horizontalEvents = historyListTree.HorizontalOrdering().Select(x => x.Data);
            IEnumerable<HistoryEvent> verticalEvents = historyListTree.VerticalOrdering().Select(x => x.Data);

            Assert.AreEqual(expectedHorizontalOrdering, horizontalEvents);
            Assert.AreEqual(expectedVerticalOrdering, verticalEvents);
        }
    }
}
