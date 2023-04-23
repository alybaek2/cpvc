namespace CPvC.Test
{
    public class HistoryViewNodeTests
    {
        // This probably needs to be changed over to HistoryEventOrderings

        //[Test]
        //public void AddRootAndChild()
        //{
        //    // Setup
        //    HistoryControl historyControl = new HistoryControl();
        //    History history = new History();
        //    CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));

        //    // Act
        //    HistoryEventOrderings orderings = new HistoryEventOrderings(historyControl.Items, history);

        //    // Verify
        //    List<HistoryEvent> verticallySorted = orderings.GetVerticallySorted().ToList();
        //    Assert.AreEqual(2, orderings.Count());
        //    Assert.AreEqual(history.RootEvent, verticallySorted[0]);
        //    Assert.AreEqual(coreEvent1, verticallySorted[1]);
        //}

        //[Test]
        //public void AddRootAndChildAndChild()
        //{
        //    // Setup
        //    History history = new History();
        //    CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
        //    CoreActionHistoryEvent coreEvent2 = history.AddCoreAction(new KeyPressAction(100, Keys.A, true));

        //    // Act
        //    HistoryEventOrderings orderings = new HistoryEventOrderings(history);

        //    // Verify
        //    List<HistoryEvent> verticallySorted = orderings.GetVerticallySorted().ToList();
        //    Assert.AreEqual(2, orderings.Count());
        //    Assert.AreEqual(history.RootEvent, verticallySorted[0]);
        //    Assert.AreEqual(coreEvent2, verticallySorted[1]);
        //}

        //[Test]
        //public void AddRootAndChildAndTwoChildren()
        //{
        //    // Setup
        //    History history = new History();
        //    CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
        //    CoreActionHistoryEvent coreEvent2 = history.AddCoreAction(new KeyPressAction(200, Keys.A, true));
        //    history.CurrentEvent = coreEvent1;
        //    CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(300, 400, null));

        //    // Act
        //    HistoryEventOrderings orderings = new HistoryEventOrderings(history);

        //    // Verify
        //    List<HistoryEvent> verticallySorted = orderings.GetVerticallySorted().ToList();
        //    Assert.AreEqual(4, orderings.Count());
        //    Assert.AreEqual(history.RootEvent, verticallySorted[0]);
        //    Assert.AreEqual(coreEvent1, verticallySorted[1]);
        //    Assert.AreEqual(coreEvent2, verticallySorted[2]);
        //    Assert.AreEqual(coreEvent3, verticallySorted[3]);
        //}

        //[Test]
        //public void DeleteNonRecursive()
        //{
        //    // Setup
        //    History history = new History();
        //    CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
        //    BookmarkHistoryEvent coreEvent2 = history.AddBookmark(200, new Bookmark(true, 0, (byte[])null, (byte[])null));
        //    CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(200, 300, null));
        //    history.DeleteBookmark(coreEvent2);

        //    // Act
        //    HistoryEventOrderings orderings = new HistoryEventOrderings(history);

        //    // Verify
        //    List<HistoryEvent> verticallySorted = orderings.GetVerticallySorted().ToList();
        //    Assert.AreEqual(2, orderings.Count());
        //    Assert.AreEqual(history.RootEvent, verticallySorted[0]);
        //    Assert.AreEqual(coreEvent3, verticallySorted[1]);
        //}

        //[Test]
        //public void DeleteRecursive()
        //{
        //    // Setup
        //    History history = new History();
        //    CoreActionHistoryEvent coreEvent1 = history.AddCoreAction(new RunUntilAction(100, 200, null));
        //    BookmarkHistoryEvent coreEvent2 = history.AddBookmark(200, new Bookmark(true, 0, (byte[])null, (byte[])null));
        //    CoreActionHistoryEvent coreEvent3 = history.AddCoreAction(new RunUntilAction(200, 300, null));
        //    history.CurrentEvent = coreEvent1;
        //    history.DeleteBranch(coreEvent2);

        //    // Act
        //    HistoryEventOrderings orderings = new HistoryEventOrderings(history);

        //    // Verify
        //    List<HistoryEvent> verticallySorted = orderings.GetVerticallySorted().ToList();
        //    Assert.AreEqual(2, orderings.Count());
        //    Assert.AreEqual(history.RootEvent, verticallySorted[0]);
        //    Assert.AreEqual(coreEvent1, verticallySorted[1]);
        //}
    }
}
