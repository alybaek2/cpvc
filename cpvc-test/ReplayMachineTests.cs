﻿using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [SetUpFixture]
    public class ReplayMachineSetup
    {
        static private HistoryEvent _finalHistoryEvent;
        static private List<UInt64> _bookmarkTicks;

        static public HistoryEvent FinalHistoryEvent
        {
            get
            {
                return _finalHistoryEvent;
            }
        }

        static public List<UInt64> BookmarkTicks
        {
            get
            {
                return _bookmarkTicks;
            }
        }

        public ReplayMachineSetup()
        {

        }

        [OneTimeSetUp]
        public void Setup()
        {
            _bookmarkTicks = new List<UInt64>();
            using (LocalMachine machine = LocalMachine.New("test", null))
            {
                machine.AudioBuffer.OverrunThreshold = int.MaxValue;

                machine.Key(Keys.A, true);
                machine.Key(Keys.A, false);
                machine.LoadDisc(0, null);
                machine.LoadTape(null);
                CoreRequest request = machine.RunUntil(machine.Ticks + 100);

                machine.Start();
                request.Wait(10000);
                machine.Stop();
                Wait(machine);

                machine.AddBookmark(false);
                _bookmarkTicks.Add(machine.Ticks);

                request = machine.RunUntil(machine.Ticks + 100);
                machine.Start();
                request.Wait(10000);
                machine.Stop();
                Wait(machine);

                machine.AddBookmark(false);
                _bookmarkTicks.Add(machine.Ticks);

                request = machine.RunUntil(machine.Ticks + 100);
                machine.Start();
                request.Wait(10000);
                machine.Stop();
                Wait(machine);

                _finalHistoryEvent = machine.History.CurrentEvent;
            }
        }

        [OneTimeTearDown]
        public void Teardown()
        {

        }
    }

    public class ReplayMachineTests
    {
        private HistoryEvent _finalHistoryEvent;
        private List<UInt64> _bookmarkTicks;

        [SetUp]
        public void Setup()
        {
            _bookmarkTicks = ReplayMachineSetup.BookmarkTicks;
            _finalHistoryEvent = ReplayMachineSetup.FinalHistoryEvent;
        }

        [TearDown]
        public void Teardown()
        {
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
            RunningState runningState1 = replayMachine.ActualRunningState;

            // Act
            replayMachine.Start();
            Wait(replayMachine);
            RunningState runningState2 = replayMachine.ActualRunningState;
            replayMachine.Stop();
            Wait(replayMachine);
            RunningState runningState3 = replayMachine.ActualRunningState;

            // Verify
            Assert.AreEqual(RunningState.Paused, runningState1);
            Assert.AreEqual(RunningState.Running, runningState2);
            Assert.AreEqual(RunningState.Paused, runningState3);
            replayMachine.Close();
        }

        [Test]
        public void CanStart()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            // Verify
            Assert.True(replayMachine.CanStart);
            Assert.False(replayMachine.CanStop);
            replayMachine.Close();
        }

        [Test]
        public void CanStop()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            // Act
            replayMachine.Start();
            Wait(replayMachine);

            // Verify
            Assert.False(replayMachine.CanStart);
            Assert.True(replayMachine.CanStop);
            replayMachine.Close();
        }

        [Test]
        public void EndTicks()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            // Verify
            Assert.AreEqual(_finalHistoryEvent.Ticks, replayMachine.EndTicks);
            replayMachine.Close();
        }

        [Test]
        public void Toggle()
        {
            // Setup
            ReplayMachine machine = CreateMachine();
            machine.Start();
            Wait(machine);

            // Act
            RunningState runningState1 = machine.ActualRunningState;
            machine.ToggleRunning();
            Wait(machine);
            RunningState runningState2 = machine.ActualRunningState;
            machine.ToggleRunning();
            Wait(machine);

            // Verify
            Assert.AreEqual(RunningState.Running, runningState1);
            Assert.AreEqual(RunningState.Paused, runningState2);
            Assert.AreEqual(RunningState.Running, machine.ActualRunningState);
            machine.Close();
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
            machine.Close();
        }

        [Test]
        public void CanClose()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            // Verify
            Assert.True(machine.CanClose);
            machine.Close();
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
            Wait(machine);

            // Act
            machine.SeekToStart();

            // Verify
            Assert.AreEqual(0, machine.Ticks);
            machine.Close();
        }

        [Test]
        public void StartAtEnd()
        {
            // Setup
            ReplayMachine machine = CreateMachine();

            machine.SeekToNextBookmark();
            machine.SeekToNextBookmark();

            machine.Start();
            Wait(machine, RunningState.Paused);


            // Act
            // Really need to check that the RunningState never changed to Running, and remains as Paused.
            machine.Start();
            Wait(machine, RunningState.Paused);

            // Verify
            Assert.AreEqual(RunningState.Paused, machine.ActualRunningState);
            Assert.AreEqual(machine.EndTicks, machine.Ticks);
            machine.Close();
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
            machine.Close();
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
            machine.Close();
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
            machine.Close();
        }

        [Test]
        public void Auditor()
        {
            // Setup
            ReplayMachine replayMachine = CreateMachine();

            List<CoreAction> actions = new List<CoreAction>();
            replayMachine.Auditors += (action) =>
            {
                actions.Add(action);
            };

            // Act
            replayMachine.Start();
            Wait(replayMachine);
            while (replayMachine.Ticks < 3000)
            {
                // Probably better to add an auditor and wait for a RunUntil.
                System.Threading.Thread.Sleep(10);
            }
            replayMachine.Stop();
            Wait(replayMachine);

            // Verify
            Assert.NotZero(actions.Count);

            replayMachine.Close();
        }
    }
}
