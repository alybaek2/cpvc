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
        private ReplayMachine CreateMachine()
        {
            HistoryEvent historyEvent = HistoryEvent.CreateCoreAction(0, CoreAction.CoreVersion(0, 1));
            HistoryEvent historyEvent2 = HistoryEvent.CreateCoreAction(1, CoreAction.KeyPress(10000000, Keys.A, true));
            HistoryEvent historyEvent3 = HistoryEvent.CreateCoreAction(2, CoreAction.KeyPress(20000000, Keys.A, false));
            HistoryEvent finalHistoryEvent = HistoryEvent.CreateCheckpoint(3, 30000000, DateTime.UtcNow, null);

            historyEvent.AddChild(historyEvent2);
            historyEvent2.AddChild(historyEvent3);
            historyEvent3.AddChild(finalHistoryEvent);

            ReplayMachine replayMachine = new ReplayMachine(finalHistoryEvent);

            return replayMachine;
        }

        [Test]
        public void StartAndStop()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                bool runningState1 = replayMachine.Running;

                // Act
                replayMachine.Start();
                bool runningState2 = replayMachine.Running;
                replayMachine.Stop();
                bool runningState3 = replayMachine.Running;

                // Verify
                Assert.False(runningState1);
                Assert.True(runningState2);
                Assert.False(runningState3);
            }
        }

        [Test]
        public void Toggle()
        {
            // Setup
            using (ReplayMachine machine = CreateMachine())
            {
                machine.Start();

                // Act
                bool running1 = machine.Running;
                machine.ToggleRunning();
                bool running2 = machine.Running;
                machine.ToggleRunning();

                // Verify
                Assert.IsTrue(running1);
                Assert.IsFalse(running2);
                Assert.IsTrue(machine.Running);
            }
        }
    }
}
