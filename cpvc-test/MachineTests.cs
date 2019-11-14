using CPvC;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace cpvc_test
{
    [TestFixture]
    public class MachineTests
    {
        private List<string> _lines;
        private Mock<IFileSystem> _mockFileSystem;

        private static string AnyString()
        {
            return It.IsAny<string>();
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
            using (Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object))
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
            using (Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object))
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
            using (Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object))
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
    }
}
