using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class ReplayMachineTests
    {
        [Test]
        public void StartAndStop()
        {
            // Setup
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 0, DateTime.UtcNow, null);
            HistoryEvent finalHistoryEvent = HistoryEvent.CreateCheckpoint(1, 40000000, DateTime.UtcNow, null);
            historyEvent.AddChild(finalHistoryEvent);

            ReplayMachine replayMachine = new ReplayMachine(finalHistoryEvent);
            bool runningState1 = replayMachine.Core.Running;

            // Act
            replayMachine.Start();
            bool runningState2 = replayMachine.Core.Running;
            replayMachine.Stop();
            bool runningState3 = replayMachine.Core.Running;

            // Verify
            Assert.False(runningState1);
            Assert.True(runningState2);
            Assert.False(runningState3);
        }
    }
}
