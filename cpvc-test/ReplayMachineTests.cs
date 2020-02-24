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
                RunForAWhile(machine);

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

                while (replayMachine.Running)
                {
                    RunForAWhile(replayMachine);
                }

                // Act
                replayMachine.Start();

                // Verify
                Assert.False(replayMachine.Running);
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

        [TestCase(false)]
        [TestCase(true)]
        public void SetRunning(bool running)
        {
            // Setup
            using (ReplayMachine replayMachine = CreateMachine())
            {
                if (running)
                {
                    replayMachine.Start();
                }

                // Act
                replayMachine.SeekToNextBookmark();

                // Verify
                Assert.AreEqual(running, replayMachine.Running);
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
                // Act and Verify - note that EnableGreyscale will trigger a change on the "Bitmap" property.
                Assert.DoesNotThrow(() => replayMachine.Volume = 100);
            }
        }
    }
}
