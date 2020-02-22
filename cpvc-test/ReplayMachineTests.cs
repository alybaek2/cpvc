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
            HistoryEvent historyEvent4 = HistoryEvent.CreateCoreAction(3, CoreAction.LoadDisc(30000000, 0, new MemoryBlob(new byte[] { 0x01, 0x02 })));
            HistoryEvent historyEvent5 = HistoryEvent.CreateCoreAction(4, CoreAction.LoadDisc(40000000, 0, null));
            HistoryEvent historyEvent6 = HistoryEvent.CreateCoreAction(5, CoreAction.LoadTape(50000000, new MemoryBlob(new byte[] { 0x01, 0x02 })));
            HistoryEvent historyEvent7 = HistoryEvent.CreateCoreAction(6, CoreAction.LoadTape(60000000, null));
            HistoryEvent finalHistoryEvent = HistoryEvent.CreateCheckpoint(7, 70000000, DateTime.UtcNow, null);

            historyEvent.AddChild(historyEvent2);
            historyEvent2.AddChild(historyEvent3);
            historyEvent3.AddChild(historyEvent4);
            historyEvent4.AddChild(historyEvent5);
            historyEvent5.AddChild(historyEvent6);
            historyEvent6.AddChild(historyEvent7);
            historyEvent7.AddChild(finalHistoryEvent);

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

        [Test]
        public void Name()
        {
            // Setup
            using (ReplayMachine machine = CreateMachine())
            {
                // Act
                machine.Name = "Test";

                // Verify
                Assert.AreEqual("Test", machine.Name);
            }
        }


        [Test]
        public void CloseTwice()
        {
            // Setup
            using (ReplayMachine machine = CreateMachine())
            {
                machine.Close();

                // Act and Verify
                Assert.DoesNotThrow(() => {
                    machine.Close();
                });
            }
        }

        [Test]
        public void DisposeTwice()
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            machine.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => {
                machine.Dispose();
            });
        }
    }
}
