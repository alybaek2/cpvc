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
                Run(machine, 4000000);

                _finalHistoryEvent = machine.History.CurrentEvent;
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
            ReplayMachine replayMachine = CreateMachine();
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
            Assert.True(machine.CanClose());
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
            RunForAWhile(machine);

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
            while (machine.RunningState == RunningState.Running)
            {
                Thread.Sleep(10);
            }

            // Act
            Mock<MachineAuditorDelegate> auditor = new Mock<MachineAuditorDelegate>();
            machine.Auditors += auditor.Object;
            machine.Start();
            while (machine.RunningState == RunningState.Running)
            {
                Thread.Sleep(10);
            }

            // Verify
            auditor.VerifyNoOtherCalls();
            if (machine.RunningState == RunningState.Running)
            {
                machine.Stop();
                Assert.Fail();
            }

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

        [TestCase(RunningState.Paused)]
        [TestCase(RunningState.Running)]
        public void SetRunning(RunningState runningState)
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            if (runningState == RunningState.Running)
            {
                machine.Start();
            }

            // Act
            machine.SeekToNextBookmark();

            // Verify
            Assert.AreEqual(runningState, machine.RunningState);
        }

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
            Thread.Sleep(10);
            replayMachine.Stop();

            // Verify
            auditor.Verify(a => a(It.Is<CoreAction>(coreAction => coreAction != null && coreAction.Type == CoreRequest.Types.RunUntil)), Times.AtLeastOnce());
        }
    }
}
