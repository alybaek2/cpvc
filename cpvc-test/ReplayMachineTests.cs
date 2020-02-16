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
            HistoryEvent historyEvent = HistoryEvent.CreateCheckpoint(0, 0, DateTime.UtcNow, null);
            HistoryEvent finalHistoryEvent = HistoryEvent.CreateCheckpoint(1, 40000000, DateTime.UtcNow, null);
            historyEvent.AddChild(finalHistoryEvent);

            ReplayMachine replayMachine = new ReplayMachine(finalHistoryEvent);

            return replayMachine;
        }

        [Test]
        public void StartAndStop()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();
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

        [Test]
        public void Toggle()
        {
            // Setup
            using (ReplayMachine machine = CreateMachine())
            {
                machine.Start();

                // Act
                bool running1 = machine.Core.Running;
                machine.ToggleRunning();
                bool running2 = machine.Core.Running;
                machine.ToggleRunning();

                // Verify
                Assert.IsTrue(running1);
                Assert.IsFalse(running2);
                Assert.IsTrue(machine.Core.Running);
            }
        }
    }
}
