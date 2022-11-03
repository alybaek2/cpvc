using Moq;
using NUnit.Framework;


namespace CPvC.Test
{
    public class HistoryViewNodeTests
    {
        //private History _history;

        [SetUp]
        public void Setup()
        {
            //_history = new History();
            //CoreActionHistoryEvent coreEvent1 = _history.AddCoreAction(new RunUntilAction(100, 200, null));
            //CoreActionHistoryEvent coreEvent2 = _history.AddCoreAction(new KeyPressAction(100, Keys.A, true)); //CoreAction(new RunUntilAction(200, 300, null));
            ////BookmarkHistoryEvent coreEvent2 = _history.AddBookmark(200, new Bookmark(false, 1, (byte[])null, (byte[])null)); //CoreAction(new RunUntilAction(200, 300, null));
            //_history.CurrentEvent = coreEvent1;
            //CoreActionHistoryEvent coreEvent3 = _history.AddCoreAction(new RunUntilAction(200, 250, null));
        }

        [Test]
        public void AddRootAndChild()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));

            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);

            // Verify
            Assert.AreEqual(2, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(history.RootEvent.Children[0], nodeList.NodeList[1]);
        }

        [Test]
        public void AddRootAndChildAndChild()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
            CoreActionHistoryEvent coreEvent2 = history.AddCoreAction(new KeyPressAction(100, Keys.A, true));
            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);


            // Verify
            Assert.AreEqual(2, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(history.RootEvent.Children[0].Children[0], nodeList.NodeList[1]);
        }

        [Test]
        public void AddRootAndChildAndTwoChildren()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
            CoreActionHistoryEvent coreEvent2 = history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
            history.CurrentEvent = coreEvent1;
            CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(300, 400, null));

            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);


            // Verify
            Assert.AreEqual(4, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(coreEvent1, nodeList.NodeList[1]);
            Assert.AreEqual(coreEvent2, nodeList.NodeList[2]);
            Assert.AreEqual(coreEvent3, nodeList.NodeList[3]);
        }

        [Test]
        public void CreateNodeThatWasPreviouslyUninteresting()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
            CoreActionHistoryEvent coreEvent2 = history.AddCoreAction(new KeyPressAction(200, Keys.A, true));

            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);

            history.CurrentEvent = coreEvent1;
            CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(300, 400, null));

            nodeList.Add(coreEvent3);

            // Verify
            Assert.AreEqual(4, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(coreEvent1, nodeList.NodeList[1]);
            Assert.AreEqual(coreEvent2, nodeList.NodeList[2]);
            Assert.AreEqual(coreEvent3, nodeList.NodeList[3]);
        }

        [Test]
        public void Update()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
            CoreActionHistoryEvent coreEvent2 = history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
            CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(200, 300, null));

            history.CurrentEvent = coreEvent1;
            CoreActionHistoryEvent coreEvent4 = history.AddCoreAction(new RunUntilAction(200, 250, null));

            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);

            CoreActionHistoryEvent coreEvent5 = history.AddCoreAction(new RunUntilAction(250, 350, null));

            nodeList.Update(coreEvent4);

            // Verify
            Assert.AreEqual(4, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(coreEvent1, nodeList.NodeList[1]);
            Assert.AreEqual(coreEvent3, nodeList.NodeList[2]);
            Assert.AreEqual(coreEvent5, nodeList.NodeList[3]);
        }

        [Test]
        public void DeleteNonRecursive()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
            BookmarkHistoryEvent coreEvent2 = history.AddBookmark(200, new Bookmark(true, 0, (byte[])null, (byte[])null));
            CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(200, 300, null));

            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);

            history.DeleteBookmark(coreEvent2);
            nodeList.Delete(coreEvent2, coreEvent1, false);

            // Verify
            Assert.AreEqual(2, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(coreEvent3, nodeList.NodeList[1]);
        }

        [Test]
        public void DeleteRecursive()
        {
            History history = new History();
            CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
            BookmarkHistoryEvent coreEvent2 = history.AddBookmark(200, new Bookmark(true, 0, (byte[])null, (byte[])null));
            CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(200, 300, null));
            history.CurrentEvent = coreEvent1;

            HistoryViewNodeList nodeList = new HistoryViewNodeList();

            nodeList.Add(history.RootEvent);

            history.DeleteBranch(coreEvent2);
            nodeList.Delete(coreEvent2, coreEvent1, true);

            // Verify
            Assert.AreEqual(2, nodeList.NodeList.Count);
            Assert.AreEqual(history.RootEvent, nodeList.NodeList[0]);
            Assert.AreEqual(coreEvent1, nodeList.NodeList[1]);
        }
    }
}
