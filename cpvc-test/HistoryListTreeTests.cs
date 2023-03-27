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
            IEnumerable<HistoryEvent> horizontalEvents = historyListTree.HorizontalOrdering().Select(x => x.Data);
            IEnumerable<HistoryEvent> verticalEvents = historyListTree.VerticalOrdering().Select(x => x.Data);

            List<HistoryEvent> expectedOrdering = new List<HistoryEvent>
            {
                history.RootEvent,
                event1
            };

            Assert.AreEqual(expectedOrdering, horizontalEvents);
            Assert.AreEqual(expectedOrdering, verticalEvents);
        }
    }
}
