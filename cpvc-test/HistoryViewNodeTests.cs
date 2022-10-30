using Moq;
using NUnit.Framework;


namespace CPvC.Test
{
    public class HistoryViewNodeTests
    {
        private History _history;

        [SetUp]
        public void Setup()
        {
            _history = new History();
            CoreActionHistoryEvent coreEvent1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            CoreActionHistoryEvent coreEvent2 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true)); //CoreAction(new RunUntilAction(200, 300, null));
            //BookmarkHistoryEvent coreEvent2 = _history.AddBookmark(200, new Bookmark(false, 1, (byte[])null, (byte[])null)); //CoreAction(new RunUntilAction(200, 300, null));
            _history.CurrentEvent = coreEvent1;
            CoreActionHistoryEvent coreEvent3 = _history.AddCoreAction(new RunUntilAction(200, 250, null));
        }

        [Test]
        public void AddRootAndChild()
        {
            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(_history.RootEvent);
            nodeList.Add(_history.RootEvent.Children[0]);
            nodeList.Add(_history.RootEvent.Children[0].Children[0]);


            // Verify
            Assert.AreEqual(2, nodeList.NodeList.Count);
            Assert.AreEqual(_history.RootEvent, nodeList.NodeList[0].HistoryEvent);
            Assert.AreEqual(_history.RootEvent.Children[0], nodeList.NodeList[1].HistoryEvent);
        }

        [Test]
        public void AddRootAndChildAndChild()
        {
            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(_history.RootEvent);
            nodeList.Add(_history.RootEvent.Children[0]);
            nodeList.Add(_history.RootEvent.Children[0].Children[0]);


            // Verify
            Assert.AreEqual(3, nodeList.NodeList.Count);
            Assert.AreEqual(_history.RootEvent, nodeList.NodeList[0].HistoryEvent);
            Assert.AreEqual(_history.RootEvent.Children[0].Children[0], nodeList.NodeList[1].HistoryEvent);
        }

        [Test]
        public void AddRootAndChildAndTwoChildren()
        {
            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(_history.RootEvent);
            nodeList.Add(_history.RootEvent.Children[0]);
            nodeList.Add(_history.RootEvent.Children[0].Children[0]);
            nodeList.Add(_history.RootEvent.Children[0].Children[1]);


            // Verify
            Assert.AreEqual(3, nodeList.NodeList.Count);
            Assert.AreEqual(_history.RootEvent, nodeList.NodeList[0].HistoryEvent);
            Assert.AreEqual(_history.RootEvent.Children[0].Children[0], nodeList.NodeList[1].HistoryEvent);
        }
    }
}
