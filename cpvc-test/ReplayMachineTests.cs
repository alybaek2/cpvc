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
            LocalMachineTests machineTests = new LocalMachineTests();
            machineTests.Setup();
            using (LocalMachine machine = machineTests.CreateMachine())
            {
                machine.Key(Keys.A, true);
                machine.Key(Keys.A, false);
                machine.LoadDisc(0, null);
                CoreRequest request = machine.LoadTape(null);

                machine.Start();
                request.Wait(10000);
                machine.Stop();
                machine.WaitForRequestedToMatchRunning();

                machine.AddBookmark(false);
                _bookmarkTicks.Add(machine.Ticks);

                request = machine.RunUntil(machine.Ticks + 1000);
                machine.Start();
                request.Wait(10000);
                machine.Stop();
                machine.WaitForRequestedToMatchRunning();

                machine.AddBookmark(false);
                _bookmarkTicks.Add(machine.Ticks);

                request = machine.RunUntil(machine.Ticks + 4000000);
                machine.Start();
                request.Wait(10000);
                machine.Stop();
                machine.WaitForRequestedToMatchRunning();

                _finalHistoryEvent = machine.History.CurrentEvent;
            }
        }

        private ReplayMachine CreateMachine()
        {
            ReplayMachine replayMachine = new ReplayMachine(_finalHistoryEvent);
            replayMachine.AudioBuffer.OverrunThreshold = int.MaxValue;

            return replayMachine;
        }

        [Test]
        public void StartAndStop()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();
            RunningState runningState1 = replayMachine.RunningState;

            // Act
            replayMachine.Start();
            replayMachine.WaitForRequestedToMatchRunning();
            RunningState runningState2 = replayMachine.RunningState;
            replayMachine.Stop();
            replayMachine.WaitForRequestedToMatchRunning();
            RunningState runningState3 = replayMachine.RunningState;

            // Verify
            Assert.AreEqual(RunningState.Paused, runningState1);
            Assert.AreEqual(RunningState.Running, runningState2);
            Assert.AreEqual(RunningState.Paused, runningState3);
        }

        [Test]
        public void CanStart()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            // Verify
            Assert.True(replayMachine.CanStart);
            Assert.False(replayMachine.CanStop);
        }

        [Test]
        public void CanStop()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            // Act
            replayMachine.Start();
            replayMachine.WaitForRequestedToMatchRunning();

            // Verify
            Assert.False(replayMachine.CanStart);
            Assert.True(replayMachine.CanStop);
        }

        [Test]
        public void EndTicks()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            // Verify
            Assert.AreEqual(_finalHistoryEvent.Ticks, replayMachine.EndTicks);
        }

        [Test]
        public void Toggle()
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            machine.Start();
            machine.WaitForRequestedToMatchRunning();

            // Act
            RunningState runningState1 = machine.RunningState;
            machine.ToggleRunning();
            machine.WaitForRequestedToMatchRunning();
            RunningState runningState2 = machine.RunningState;
            machine.ToggleRunning();
            machine.WaitForRequestedToMatchRunning();

            // Verify
            Assert.AreEqual(RunningState.Running, runningState1);
            Assert.AreEqual(RunningState.Paused, runningState2);
            Assert.AreEqual(RunningState.Running, machine.RunningState);
        }

        [Test]
        public void Name()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            // Act
            machine.Name = "Test";

            // Verify
            Assert.AreEqual("Test", machine.Name);
        }

        [Test]
        public void CanClose()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            // Verify
            Assert.True(machine.CanClose);
        }

        [Test]
        public void CloseTwice()
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            machine.Close();

            // Act and Verify
            Assert.DoesNotThrow(() =>
            {
                machine.Close();
            });
        }

        [Test]
        public void SeekToStart()
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            machine.Start();
            while (machine.Ticks == 0);

            machine.Stop();
            machine.WaitForRequestedToMatchRunning();

            // Act
            machine.SeekToStart();

            // Verify
            Assert.AreEqual(0, machine.Ticks);
        }

        [Test]
        public void StartAtEnd()
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            machine.SeekToNextBookmark();
            machine.SeekToNextBookmark();

            machine.Start();
            while (machine.Ticks < machine.EndTicks)
            {
                machine.AdvancePlayback(100000);
                Thread.Sleep(10);
            }

            // Act
            Mock<MachineAuditorDelegate> auditor = new Mock<MachineAuditorDelegate>();
            machine.Auditors += auditor.Object;
            machine.Start();
            machine.WaitForRequestedToMatchRunning();
            while (machine.Ticks < machine.EndTicks)
            {
                machine.AdvancePlayback(100000);
                Thread.Sleep(10);
            }

            // Verify
            auditor.VerifyNoOtherCalls();
            Assert.AreEqual(RunningState.Paused, machine.RunningState);
            Assert.AreEqual(machine.EndTicks, machine.Ticks);
        }

        [Test]
        public void SeekToPrevAndNextBookmark()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            // Act and Verify
            foreach (UInt64 bookmarkTick in _bookmarkTicks)
            {
                machine.SeekToNextBookmark();
                Assert.AreEqual(bookmarkTick, machine.Ticks);
            }

            UInt64 finalTicks = machine.Ticks;
            machine.SeekToNextBookmark();
            Assert.AreEqual(finalTicks, machine.Ticks);

            _bookmarkTicks.Reverse();
            foreach (UInt64 bookmarkTick in _bookmarkTicks)
            {
                Assert.AreEqual(bookmarkTick, machine.Ticks);
                machine.SeekToPreviousBookmark();
            }

            Assert.AreEqual(0, machine.Ticks);
            machine.SeekToPreviousBookmark();
            Assert.AreEqual(0, machine.Ticks);
        }

        //[TestCase(RunningState.Paused)]
        //[TestCase(RunningState.Running)]
        //public void SetRunning(RunningState runningState)
        //{
        //    // Setup
        //    ReplayMachine machine = CreateMachine();
        //    if (runningState == RunningState.Running)
        //    {
        //        machine.Start();
        //    }

        //    // Act
        //    machine.SeekToNextBookmark();

        //    // Verify
        //    Assert.AreEqual(runningState, machine.RunningState);
        //}

        /// <summary>
        /// Tests that the machine doesn't throw an exception when there are no PropertyChanged
        /// handlers registered and a property is changed.
        /// </summary>
        [Test]
        public void NoPropertyChangedHandlers()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            // Act and Verify
            Assert.DoesNotThrow(() => machine.Status = "Status");
        }

        [Test]
        public void StatusChanged()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            machine.PropertyChanged += propChanged.Object;

            // Act
            machine.Status = "Status";

            // Verify
            propChanged.Verify(p => p(machine, It.Is<PropertyChangedEventArgs>(e => e.PropertyName == "Status")));
        }

        [Test]
        public void Auditor()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();
            Mock<MachineAuditorDelegate> auditor = new Mock<MachineAuditorDelegate>();
            replayMachine.Auditors += auditor.Object;

            // Act
            replayMachine.Start();
            replayMachine.WaitForRequestedToMatchRunning();
            Thread.Sleep(1000);
            replayMachine.Stop();
            replayMachine.WaitForRequestedToMatchRunning();

            // Verify
            auditor.Verify(a => a(It.Is<CoreAction>(coreAction => coreAction != null && coreAction.Type == CoreRequest.Types.RunUntil)), Times.AtLeastOnce());
        }
    }
}
