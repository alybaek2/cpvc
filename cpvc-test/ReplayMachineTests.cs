using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class ReplayMachineTests
    {
        private HistoryEvent _finalHistoryEvent;
        private List<UInt64> _bookmarkTicks;

        [SetUp]
        public void Setup()
        {
            _bookmarkTicks = new List<UInt64>();
            MachineTests machineTests = new MachineTests();
            machineTests.Setup();
            using (Machine machine = machineTests.CreateMachine())
            {
                RunForAWhile(machine);
                machine.Key(Keys.A, true);
                RunForAWhile(machine);
                machine.Key(Keys.A, false);
                RunForAWhile(machine);
                machine.LoadDisc(0, null);
                RunForAWhile(machine);
                machine.LoadTape(null);
                RunForAWhile(machine);
                machine.AddBookmark(false);
                _bookmarkTicks.Add(machine.Ticks);
                RunForAWhile(machine);
                machine.AddBookmark(false);
                _bookmarkTicks.Add(machine.Ticks);
                Run(machine, 4000000, false);

                _finalHistoryEvent = machine.CurrentEvent;
            }
        }

        private ReplayMachine CreateMachine()
        {
            ReplayMachine replayMachine = new ReplayMachine(_finalHistoryEvent);

            return replayMachine;
        }

        [Test]
        public void StartAndStop()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                RunningState runningState1 = replayMachine.RunningState;

                // Act
                replayMachine.Start();
                RunningState runningState2 = replayMachine.RunningState;
                replayMachine.Stop();
                RunningState runningState3 = replayMachine.RunningState;

                // Verify
                Assert.AreEqual(RunningState.Paused, runningState1);
                Assert.AreEqual(RunningState.Running, runningState2);
                Assert.AreEqual(RunningState.Paused, runningState3);
            }
        }

        [Test]
        public void EndTicks()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                // Verify
                Assert.AreEqual(_finalHistoryEvent.Ticks, replayMachine.EndTicks);
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
                RunningState runningState1 = machine.RunningState;
                machine.ToggleRunning();
                RunningState runningState2 = machine.RunningState;
                machine.ToggleRunning();

                // Verify
                Assert.AreEqual(RunningState.Running, runningState1);
                Assert.AreEqual(RunningState.Paused, runningState2);
                Assert.AreEqual(RunningState.Running, machine.RunningState);
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
        public void CanClose()
        {
            // Setup
            using (ReplayMachine machine = CreateMachine())
            {
                // Verify
                Assert.True(machine.CanClose());
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
                Assert.DoesNotThrow(() =>
                {
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
            Assert.DoesNotThrow(() =>
            {
                machine.Dispose();
            });
        }

        [Test]
        public void SeekToStart()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                RunForAWhile(replayMachine);

                // Act
                replayMachine.SeekToStart();

                // Verify
                Assert.AreEqual(0, replayMachine.Ticks);
            }
        }

        [Test]
        public void StartAtEnd()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                replayMachine.SeekToNextBookmark();
                replayMachine.SeekToNextBookmark();

                replayMachine.Start();
                while (replayMachine.RunningState == RunningState.Running)
                {
                    Thread.Sleep(10);
                }

                // Act
                Mock<MachineAuditorDelegate> auditor = new Mock<MachineAuditorDelegate>();
                replayMachine.Auditors += auditor.Object;
                replayMachine.Start();
                while (replayMachine.RunningState == RunningState.Running)
                {
                    Thread.Sleep(10);
                }

                // Verify
                auditor.VerifyNoOtherCalls();
                if (replayMachine.RunningState == RunningState.Running)
                {
                    replayMachine.Stop();
                    Assert.Fail();
                }

                Assert.AreEqual(replayMachine.EndTicks, replayMachine.Ticks);
            }
        }

        [Test]
        public void SeekToPrevAndNextBookmark()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                // Act and Verify
                foreach (UInt64 bookmarkTick in _bookmarkTicks)
                {
                    replayMachine.SeekToNextBookmark();
                    Assert.AreEqual(bookmarkTick, replayMachine.Ticks);
                }

                UInt64 finalTicks = replayMachine.Ticks;
                replayMachine.SeekToNextBookmark();
                Assert.AreEqual(finalTicks, replayMachine.Ticks);

                _bookmarkTicks.Reverse();
                foreach (UInt64 bookmarkTick in _bookmarkTicks)
                {
                    Assert.AreEqual(bookmarkTick, replayMachine.Ticks);
                    replayMachine.SeekToPreviousBookmark();
                }

                Assert.AreEqual(0, replayMachine.Ticks);
                replayMachine.SeekToPreviousBookmark();
                Assert.AreEqual(0, replayMachine.Ticks);
            }
        }

        [TestCase(RunningState.Paused)]
        [TestCase(RunningState.Running)]
        public void SetRunning(RunningState runningState)
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                if (runningState == RunningState.Running)
                {
                    replayMachine.Start();
                }

                // Act
                replayMachine.SeekToNextBookmark();

                // Verify
                Assert.AreEqual(runningState, replayMachine.RunningState);
            }
        }

        /// <summary>
        /// Tests that the machine doesn't throw an exception when there are no PropertyChanged
        /// handlers registered and a property is changed.
        /// </summary>
        [Test]
        public void NoPropertyChangedHandlers()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                // Act and Verify
                Assert.DoesNotThrow(() => replayMachine.Status = "Status");
            }
        }

        [Test]
        public void StatusChanged()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
                replayMachine.PropertyChanged += propChanged.Object;

                // Act
                replayMachine.Status = "Status";

                // Verify
                propChanged.Verify(p => p(replayMachine, It.Is<PropertyChangedEventArgs>(e => e.PropertyName == "Status")));
            }
        }

        [Test]
        public void Auditor()
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                Mock<MachineAuditorDelegate> auditor = new Mock<MachineAuditorDelegate>();
                replayMachine.Auditors += auditor.Object;

                // Act
                replayMachine.Start();
                Thread.Sleep(10);
                replayMachine.Stop();

                // Verify
                auditor.Verify(a => a(It.Is<CoreAction>(coreAction => coreAction != null && coreAction.Type == CoreRequest.Types.RunUntilForce)), Times.AtLeastOnce());
            }
        }
    }
}
