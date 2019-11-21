using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class MachineTests
    {
        private List<string> _lines;
        private Mock<IFileSystem> _mockFileSystem;

        private Machine CreateMachine()
        {
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);

            // For consistency with automated builds, use all zero ROMs.
            byte[] zeroROM = new byte[0x4000];
            machine.Core.SetLowerROM(zeroROM);
            machine.Core.SetUpperROM(0, zeroROM);
            machine.Core.SetUpperROM(7, zeroROM);

            return machine;    
        }

        private void RunForAWhile(Machine machine)
        {
            UInt64 startTicks = machine.Core.Ticks;

            int timeWaited = 0;
            int sleepTime = 10;
            machine.Start();
            while (machine.Core.Ticks == startTicks)
            {
                if (timeWaited > 1000)
                {
                    throw new Exception(String.Format("Waited too long for Machine to run! {0} {1}", machine.Core.Ticks, machine.Core.Running));
                }

                System.Threading.Thread.Sleep(sleepTime);
                timeWaited += sleepTime;

                // Empty out the audio buffer to prevent the machine from stalling...
                machine.AdvancePlayback(48000);
            }

            machine.Stop();
        }

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));

            Mock<IFile> mockWriter = new Mock<IFile>(MockBehavior.Strict);
            mockWriter.Setup(s => s.WriteLine(AnyString())).Callback<string>(line => _lines.Add(line));
            mockWriter.Setup(s => s.Close());

            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFile(AnyString())).Returns(mockWriter.Object);

            _lines = new List<string>();
        }

        /// <summary>
        /// Ensures that the machine writes a system bookmark when closing, except when the
        /// current event is already a system bookmark at the same point in time. This can
        /// occur when a machine is open in a paused state, then immediately closed.
        /// </summary>
        /// <param name="startBeforeClosing">Indicates if the machine should be run for a short period of time before being closed.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void ConsecutiveSystemBookmarksOnClose(bool startBeforeClosing)
        {
            // Act
            UInt64 ticks = 0;
            using (Machine machine = CreateMachine())
            {
                machine.AddBookmark(true);

                if (startBeforeClosing)
                {
                    RunForAWhile(machine);
                }

                ticks = machine.Core.Ticks;
            }

            // Verify
            if (startBeforeClosing)
            {
                Assert.AreEqual(4, _lines.Count);
                Assert.True(_lines[3].StartsWith(String.Format("checkpoint:2:{0}:1:", ticks)));
            }
            else
            {
                Assert.AreEqual(3, _lines.Count);
            }
        }

        /// <summary>
        /// Ensures that a system bookmark isn't created when a machine at the root event is closed.
        /// </summary>
        [Test]
        public void NoSystemBookmarksOnClose()
        {
            // Act
            int linesCount = 0;
            using (Machine machine = CreateMachine())
            {
                linesCount = _lines.Count;
            }

            // Verify
            Assert.AreEqual(linesCount, _lines.Count);
        }

        /// <summary>
        /// Ensures that the AutoPause method pauses a running machine and that when the object returned
        /// by AutoPause is disposed of, the machine will resume. Also ensures correct behaviour for
        /// nested calls to AutoPause.
        /// </summary>
        [Test]
        public void AutoPause()
        {
            // Act and Verify
            using (Machine machine = CreateMachine())
            {
                machine.Start();

                Assert.IsTrue(machine.Core.Running);

                using (machine.AutoPause())
                {
                    Assert.IsFalse(machine.Core.Running);

                    using (machine.AutoPause())
                    {
                        Assert.IsFalse(machine.Core.Running);
                    }

                    Assert.IsFalse(machine.Core.Running);
                }

                Assert.IsTrue(machine.Core.Running);
            }
        }

        /// <summary>
        /// Ensures the SeekToLastBookmark method works as expected. When a previous bookmark exists, the machine should revert
        /// to that state. If no previous bookmark exists, the machine reverts to the root event (equivalent to a hard reset).
        /// </summary>
        /// <param name="createBookmark">Indicates if a bookmark should be created prior to calling SeekToLastBookmark.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void SeekToLastBookmark(bool createBookmark)
        {
            // Setup
            using (Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object))
            {
                if (createBookmark)
                {
                    RunForAWhile(machine);
                    machine.AddBookmark(false);
                }

                UInt64 ticks = machine.Core.Ticks;
                int bookmarkId = machine.CurrentEvent.Id;
                byte[] state = machine.Core.GetState();

                RunForAWhile(machine);

                // Act
                machine.SeekToLastBookmark();

                // Verify
                Assert.AreEqual(machine.CurrentEvent.Id, bookmarkId);
                Assert.AreEqual(machine.Core.Ticks, ticks);
                Assert.AreEqual(state, machine.Core.GetState());
            }
        }

        /// <summary>
        /// Ensures that a machine opened "lazily" sets the appropriate RequiresOpen property.
        /// </summary>
        [Test]
        public void OpenLazy()
        {
            // Setup
            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines("test.cpvc")).Returns(new string[] { "name:Test", "checkpoint:0:0:0:0" });

            // Act
            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, true))
            {
                // Verify
                Assert.IsTrue(machine.RequiresOpen);
                Assert.AreEqual(machine.Filepath, "test.cpvc");
                Assert.AreEqual(machine.Name, "Test");
            }
        }

        [Test]
        public void OpenNoCurrentEvent()
        {
            // Setup
            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines("test.cpvc")).Returns(new string[] { "name:Test" });

            // Act and Verify
            Assert.Throws<Exception>(() =>
            {
                using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false)) { }
            });
        }

        [Test]
        public void OpenInvalidToken()
        {
            // Setup
            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines("test.cpvc")).Returns(new string[] { "invalid:0" });

            // Act and Verify
            Assert.Throws<Exception>(() =>
            {
                using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false)) { }
            });
        }

        /// <summary>
        /// Ensures an existing machine is opened with the expected state.
        /// </summary>
        [Test]
        public void Open()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                RunForAWhile(machine);
                machine.Key(Keys.A, true);
                RunForAWhile(machine);
                machine.LoadDisc(0, null);
                RunForAWhile(machine);
                machine.LoadTape(null);
                RunForAWhile(machine);
                machine.Reset();
                RunForAWhile(machine);
                machine.AddBookmark(false);
                HistoryEvent bookmarkEvent = machine.CurrentEvent;
                RunForAWhile(machine);
                machine.SeekToLastBookmark();
                HistoryEvent eventToDelete = bookmarkEvent.Children[0];
                RunForAWhile(machine);
                machine.SetBookmark(bookmarkEvent, null);
                machine.TrimTimeline(eventToDelete);
            }

            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines("test.cpvc")).Returns(_lines.ToArray());
            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                // Verify
                Assert.IsFalse(machine.RequiresOpen);
                Assert.AreEqual(machine.Filepath, "test.cpvc");
                Assert.AreEqual(machine.Name, "test");

                Assert.AreEqual(HistoryEvent.Types.Checkpoint, machine.RootEvent.Type);
                Assert.AreEqual(1, machine.RootEvent.Children.Count);

                HistoryEvent historyEvent = machine.RootEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreActionBase.Types.KeyPress, historyEvent.CoreAction.Type);
                Assert.AreEqual(Keys.A, historyEvent.CoreAction.KeyCode);
                Assert.IsTrue(historyEvent.CoreAction.KeyDown);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreActionBase.Types.LoadDisc, historyEvent.CoreAction.Type);
                Assert.AreEqual(0, historyEvent.CoreAction.Drive);
                Assert.IsNull(historyEvent.CoreAction.MediaBuffer);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreActionBase.Types.LoadTape, historyEvent.CoreAction.Type);
                Assert.IsNull(historyEvent.CoreAction.MediaBuffer);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreActionBase.Types.Reset, historyEvent.CoreAction.Type);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.Checkpoint, historyEvent.Type);
                Assert.IsNull(historyEvent.Bookmark);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.Checkpoint, historyEvent.Type);
                Assert.IsNull(historyEvent.Bookmark);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.Checkpoint, historyEvent.Type);
                Assert.IsNotNull(historyEvent.Bookmark);

                Assert.AreEqual(historyEvent, machine.CurrentEvent);
            }
        }

        /// <summary>
        /// Ensure that if a machine file is missing the final system bookmark that's written when
        /// a machine is closed, the machine when opened should set the current event to the most
        /// recent event that has a bookmark.
        /// </summary>
        [Test]
        public void OpenWithMissingFinalBookmark()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                RunForAWhile(machine);
                machine.AddBookmark(false);
                RunForAWhile(machine);
            }

            // Remove the final system bookmark that was added when the machine was closed.
            _lines.RemoveAt(_lines.Count - 1);
            _mockFileSystem.Setup(fileSystem => fileSystem.ReadLines("test.cpvc")).Returns(_lines.ToArray());

            // Act
            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                // Verify
                HistoryEvent bookmarkEvent = machine.RootEvent.Children[0];
                Assert.AreEqual(bookmarkEvent, machine.CurrentEvent);
            }
        }

        /// <summary>
        /// Ensures the correct number of audio samples are generated after running the core.
        /// </summary>
        /// <param name="ticks">The number of ticks to run the core for.</param>
        /// <param name="expectedSamples">The number of audio samples that should be written.</param>
        /// <param name="bufferSize">The size of the buffer to receive audio data.</param>
        [TestCase(4UL, 1, 100)]
        [TestCase(250UL, 1, 7)]
        [TestCase(250UL, 4, 100)]
        [TestCase(504UL, 7, 100)]
        [TestCase(85416UL, 1025, 4104)]  // This test case ensures we do at least 2 iterations of the while loop in ReadAudio16BitStereo.
        public void GetAudio(UInt64 ticks, int expectedSamples, int bufferSize)
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                machine.Core.SetLowerROM(new byte[0x4000]);
                machine.Core.SetUpperROM(0, new byte[0x4000]);
                machine.Core.SetUpperROM(7, new byte[0x4000]);

                byte[] buffer = new byte[bufferSize];

                // Act
                machine.Core.RunUntil(ticks, StopReasons.AudioOverrun);
                int samples = machine.ReadAudio(buffer, 0, bufferSize);

                // Verify
                Assert.AreEqual(expectedSamples, samples);
            }
        }

        [Test]
        public void Toggle()
        {
            // Setup
            using (Machine machine = CreateMachine())
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
