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
        private Mock<IFileSystem> _mockFileSystem;
        private MockFileByteStream _mockBinaryWriter;
        private Mock<MachineAuditorDelegate> _mockAuditor;

        public Machine CreateMachine()
        {
            Machine machine = Machine.New("test", "test.cpvc", _mockFileSystem.Object);
            machine.Auditors += _mockAuditor.Object;

            // For consistency with automated builds, use all zero ROMs.
            byte[] zeroROM = new byte[0x4000];
            machine.Core.SetLowerROM(zeroROM);
            machine.Core.SetUpperROM(0, zeroROM);
            machine.Core.SetUpperROM(7, zeroROM);

            return machine;
        }

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.FileLength(AnyString())).Returns(100);

            _mockBinaryWriter = new MockFileByteStream();

            _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream("test.cpvc")).Returns(_mockBinaryWriter.Object);

            _mockAuditor = new Mock<MachineAuditorDelegate>();
        }

        [TearDown]
        public void Teardown()
        {
            _mockFileSystem = null;
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
            int endpos = 0;
            using (Machine machine = CreateMachine())
            {
                machine.AddBookmark(true);

                if (startBeforeClosing)
                {
                    RunForAWhile(machine);
                }

                ticks = machine.Core.Ticks;
                endpos = _mockBinaryWriter.Content.Count;
            }

            // Verify
            if (startBeforeClosing)
            {
                byte[] expectedBinary = new byte[] { 0x05, 0x02, 0x00, 0x00, 0x00 };
                Assert.AreEqual(expectedBinary, _mockBinaryWriter.Content.GetRange(endpos, expectedBinary.Length));
                Assert.AreEqual(0x01, _mockBinaryWriter.Content[endpos + 21]);
                Assert.AreEqual(0x01, _mockBinaryWriter.Content[endpos + 22]);
            }
            else
            {
                Assert.AreEqual(endpos, _mockBinaryWriter.Content.Count);
            }
        }

        /// <summary>
        /// Ensures that a system bookmark isn't created when a machine at the root event is closed.
        /// </summary>
        [Test]
        public void NoSystemBookmarksOnClose()
        {
            // Act
            int pos = 0;
            using (Machine machine = CreateMachine())
            {
                pos = _mockBinaryWriter.Content.Count;
            }

            // Verify
            Assert.AreEqual(pos, _mockBinaryWriter.Content.Count);
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

                Assert.AreEqual(RunningState.Running, machine.Core.RunningState);

                using (machine.AutoPause())
                {
                    Assert.AreEqual(RunningState.Paused, machine.Core.RunningState);

                    using (machine.AutoPause())
                    {
                        Assert.AreEqual(RunningState.Paused, machine.Core.RunningState);
                    }

                    Assert.AreEqual(RunningState.Paused, machine.Core.RunningState);
                }

                Assert.AreEqual(RunningState.Running, machine.Core.RunningState);
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
                machine.Auditors += _mockAuditor.Object;

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
                machine.JumpToMostRecentBookmark();

                // Verify
                Assert.AreEqual(machine.CurrentEvent.Id, bookmarkId);
                Assert.AreEqual(machine.Core.Ticks, ticks);
                Assert.AreEqual(state, machine.Core.GetState());

                if (createBookmark)
                {
                    _mockAuditor.Verify(a => a(It.Is<CoreAction>(c => c.Type == CoreRequest.Types.LoadCore && c.Ticks == ticks)), Times.Once);
                }
                else
                {
                    _mockAuditor.Verify(a => a(It.Is<CoreAction>(c => c.Type == CoreRequest.Types.Reset && c.Ticks == 0)), Times.Once);
                }
            }
        }

        /// <summary>
        /// Ensures that a machine opened "lazily" sets the appropriate RequiresOpen property.
        /// </summary>
        [Test]
        public void OpenLazy()
        {
            // Setup
            _mockBinaryWriter.Content = new List<byte>
            {
                0x00,
                      0x04, 0x00, 0x00, 0x00,
                      (byte)'T', (byte)'e', (byte)'s', (byte)'t',
                0x05,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00
            };

            // Act
            using (Machine machine = Machine.Open("Test", "test.cpvc", _mockFileSystem.Object, true))
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
            _mockBinaryWriter.Content = new List<byte>();

            // Act and Verify
            Assert.Throws<Exception>(() =>
            {
                using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false)) { }
            });
        }

        [Test]
        public void OpenInvalidBlockType()
        {
            // Setup
            _mockBinaryWriter.Content = new List<byte>
            {
                0x7f  // Unknown block type - should cause an exception when read.
            };

            // Act and Verify
            Assert.Throws<Exception>(() =>
            {
                using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false)) { }
            });
        }

        [Test]
        public void CanClose([Values(false, true)] bool requiresOpen)
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                if (requiresOpen)
                {
                    machine.Close();
                }

                // Act
                bool canClose = machine.CanClose();

                // Verify
                Assert.AreEqual(!requiresOpen, canClose);
            }
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
                machine.JumpToMostRecentBookmark();
                HistoryEvent eventToDelete = bookmarkEvent.Children[0];
                RunForAWhile(machine);
                machine.SetBookmark(bookmarkEvent, null);
                machine.TrimTimeline(eventToDelete);
            }

            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                // Verify
                Assert.IsFalse(machine.RequiresOpen);
                Assert.AreEqual(machine.Filepath, "test.cpvc");
                Assert.AreEqual(machine.Name, "test");

                Assert.AreEqual(HistoryEvent.Types.CoreAction, machine.RootEvent.Type);
                Assert.AreEqual(CoreRequest.Types.CoreVersion, machine.RootEvent.CoreAction.Type);
                Assert.AreEqual(1, machine.RootEvent.Children.Count);

                HistoryEvent historyEvent = machine.RootEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreRequest.Types.KeyPress, historyEvent.CoreAction.Type);
                Assert.AreEqual(Keys.A, historyEvent.CoreAction.KeyCode);
                Assert.IsTrue(historyEvent.CoreAction.KeyDown);
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreRequest.Types.LoadDisc, historyEvent.CoreAction.Type);
                Assert.AreEqual(0, historyEvent.CoreAction.Drive);
                Assert.IsNull(historyEvent.CoreAction.MediaBuffer.GetBytes());
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreRequest.Types.LoadTape, historyEvent.CoreAction.Type);
                Assert.IsNull(historyEvent.CoreAction.MediaBuffer.GetBytes());
                Assert.AreEqual(1, historyEvent.Children.Count);

                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreRequest.Types.Reset, historyEvent.CoreAction.Type);
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

                // Opening the machine should add a "Version" event.
                historyEvent = historyEvent.Children[0];
                Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
                Assert.AreEqual(CoreRequest.Types.CoreVersion, historyEvent.CoreAction.Type);

                Assert.AreEqual(historyEvent, machine.CurrentEvent);

                _mockAuditor.Verify(a => a(It.Is<CoreAction>(c => c.Type == CoreRequest.Types.LoadCore)), Times.Once);
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
            int endpos = 0;
            using (Machine machine = CreateMachine())
            {
                RunForAWhile(machine);
                machine.AddBookmark(false);
                RunForAWhile(machine);
                machine.LoadDisc(0, null);
                RunForAWhile(machine);

                endpos = _mockBinaryWriter.Content.Count;
            }

            // Remove the final system bookmark that was added when the machine was closed.
            _mockBinaryWriter.Content.RemoveRange(endpos, _mockBinaryWriter.Content.Count - endpos);

            // Act
            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                // Verify - when opened, the machine should rewind to the bookmark, then add a Version
                //          history event at that point.
                HistoryEvent bookmarkEvent = machine.RootEvent.Children[0];
                Assert.AreEqual(2, bookmarkEvent.Children.Count);
                Assert.AreEqual(machine.CurrentEvent, bookmarkEvent.Children[1]);
                Assert.AreEqual(HistoryEvent.Types.CoreAction, bookmarkEvent.Children[1].Type);
                Assert.AreEqual(CoreRequest.Types.CoreVersion, bookmarkEvent.Children[1].CoreAction.Type);
            }
        }

        /// <summary>
        /// Ensures that if a bookmark has somehow been corrupted, an exception is thrown
        /// when opening the machine.
        /// </summary>
        [Test]
        public void OpenWithCorruptedFinalBookmark()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                RunForAWhile(machine);
                machine.AddBookmark(true);

                Bookmark corruptedBookmark = new Bookmark(true, 5, new byte[] { }, new byte[] { });
                machine.SetBookmark(machine.CurrentEvent, corruptedBookmark);
            }

            // Act and Verify
            Assert.Throws<System.IndexOutOfRangeException>(() =>
            {
                using (Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
                {
                }
            });
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
                RunningState state1 = machine.RunningState;
                machine.ToggleRunning();
                RunningState state2 = machine.RunningState;
                machine.ToggleRunning();

                // Verify
                Assert.AreEqual(RunningState.Running, state1);
                Assert.AreEqual(RunningState.Paused, state2);
                Assert.AreEqual(RunningState.Running, machine.RunningState);
            }
        }

        [Test]
        public void RewriteFile()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                RunForAWhile(machine);
                machine.LoadDisc(0, null);
                RunForAWhile(machine);
                machine.AddBookmark(false);
                RunForAWhile(machine);

                HistoryEvent bookmarkEvent = machine.CurrentEvent;

                machine.LoadDisc(0, null);
                RunForAWhile(machine);

                HistoryEvent eventToDelete = machine.CurrentEvent;

                machine.JumpToMostRecentBookmark();
                machine.LoadDisc(0, null);
                RunForAWhile(machine);
                machine.JumpToMostRecentBookmark();
                machine.LoadTape(null);
                RunForAWhile(machine);
                machine.TrimTimeline(eventToDelete.Children[0]);
                machine.SetBookmark(bookmarkEvent, null);
            }

            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);

                MockFileByteStream rewriteTempFile = new MockFileByteStream();
                _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream("test.cpvc.new")).Returns(rewriteTempFile.Object);

                MockSequence sequence = new MockSequence();
                mockFileReader.InSequence(sequence).Setup(x => x.SetName("test")).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(VersionEvent(0))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CoreActionEvent(1, CoreRequest.Types.LoadDisc))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CheckpointEvent(2))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CoreActionEvent(5, CoreRequest.Types.LoadDisc))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CheckpointEvent(6))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.SetCurrentEvent(2)).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CoreActionEvent(7, CoreRequest.Types.LoadTape))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CheckpointEvent(9))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(VersionEvent(10))).Verifiable();
                mockFileReader.InSequence(sequence).Setup(x => x.SetCurrentEvent(10)).Verifiable();

                // Act
                machine.Compact(false);

                MachineFile file2 = new MachineFile(rewriteTempFile.Object);
                file2.ReadFile(mockFileReader.Object);

                // Verify
                mockFileReader.Verify();
                mockFileReader.VerifyNoOtherCalls();

                Assert.AreEqual("Compacted machine file by 0%", machine.Status);
            }
        }

        [Test]
        public void EnableTurbo()
        {
            // Setup
            UInt64 turboDuration = 0;
            UInt64 normalDuration = 0;
            using (Machine machine = CreateMachine())
            {
                // Act
                machine.EnableTurbo(true);

                // Run long enough to fill the audio buffer.
                Run(machine, 8000000, true);
                turboDuration = machine.Core.Ticks;

                // Empty out the audio buffer.
                machine.AdvancePlayback(1000000);

                machine.EnableTurbo(false);
                Run(machine, 8000000, true);
                normalDuration = machine.Core.Ticks - turboDuration;
            }

            // Verify
            double expectedSpeedFactor = 10.0;
            double actualSpeedFactor = ((double)turboDuration) / ((double)normalDuration);

            // We can't expect the actual factor to be *precisely* 10 times greater than
            // normal, so just make sure it's reasonably close.
            Assert.Less(Math.Abs(1 - (actualSpeedFactor / expectedSpeedFactor)), 0.001);
        }

        [Test]
        public void CorruptCheckpointBookmark()
        {
            // Setup
            _mockBinaryWriter.Content = new List<byte>
            {
                0x00,
                      0x04, 0x00, 0x00, 0x00,
                      (byte)'T', (byte)'e', (byte)'s', (byte)'t',
                0x05,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x01,
                      0x00,
                      0x01, 0x01, 0x00, 0x00, 0x00  // State blob length is 1, but no actual blob bytes exist... should cause an exception!
            };

            // Act and Verify
            Assert.That(() => Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false), Throws.Exception);
        }

        [Test]
        public void SetBookmarkOnNonCheckpoint()
        {
            // Setup
            _mockBinaryWriter.Content = new List<byte>
            {
                0x00,
                      0x04, 0x00, 0x00, 0x00,
                      (byte)'T', (byte)'e', (byte)'s', (byte)'t',
                0x05,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00,
                0x01,
                      0x01, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0xba
            };

            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                int pos = _mockBinaryWriter.Content.Count;

                // Act
                machine.SetBookmark(machine.RootEvent.Children[0], null);

                // Verify
                Assert.IsNotNull(machine.RootEvent);
                Assert.GreaterOrEqual(machine.RootEvent.Children.Count, 1);
                Assert.IsEmpty(machine.RootEvent.Children[0].Children);
                Assert.AreEqual(pos, _mockBinaryWriter.Content.Count);
            }
        }

        [Test]
        public void DeleteRootEvent()
        {
            // Setup
            _mockBinaryWriter.Content = new List<byte>
            {
                0x00,
                      0x04, 0x00, 0x00, 0x00,
                      (byte)'T', (byte)'e', (byte)'s', (byte)'t',
                0x05,
                      0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0x00,
                0x01,
                      0x01, 0x00, 0x00, 0x00,
                      0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                      0xba,
                0x06,
                      0x00, 0x00, 0x00, 0x00
            };

            // Act
            using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
            {
                // Verify
                Assert.IsNotNull(machine.RootEvent);
                Assert.IsNotEmpty(machine.RootEvent.Children);
            }
        }

        [Test]
        public void TrimTimelineRoot()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                int pos = _mockBinaryWriter.Content.Count;

                // Act
                machine.TrimTimeline(machine.RootEvent);

                // Verify
                Assert.IsNotNull(machine.RootEvent);
                Assert.IsEmpty(machine.RootEvent.Children);
                Assert.AreEqual(pos, _mockBinaryWriter.Content.Count);
            }
        }

        [Test]
        public void Volume()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act
                machine.Volume = 100;

                // Verify
                Assert.AreEqual(100, machine.Volume);
            }
        }

        [Test]
        public void VolumeNoCore()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act
                machine.Volume = 100;
                machine.Close();

                // Verify
                Assert.Zero(machine.Volume);
            }
        }

        [Test]
        public void RunningNoCore()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act
                machine.Close();

                // Verify
                Assert.AreEqual(RunningState.Paused, machine.RunningState);
            }
        }

        [Test]
        public void TicksNoCore()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act
                RunForAWhile(machine);
                machine.Close();

                // Verify
                Assert.Zero(machine.Ticks);
            }
        }

        [Test]
        public void AdvancePlaybackNoCore()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                machine.Close();

                // Act and Verify
                Assert.DoesNotThrow(() => machine.AdvancePlayback(1));
            }
        }

        [Test]
        public void ReadAudioNoCore()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                machine.Close();

                // Act and Verify
                Assert.DoesNotThrow(() => machine.ReadAudio(null, 0, 1));
            }
        }

        [Test]
        public void SetSameCore()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act and Verify - note that if CoreMachine.Core_set didn't do a check for reference
                //                  equality between the new core and current core, an exception would
                //                  later be thrown due to Dispose() being called.
                Assert.DoesNotThrow(() =>
                {
                    machine.Core = machine.Core;
                });
            }
        }

        [Test]
        public void SetCurrentEvent()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                for (byte k = 0; k < 80; k++)
                {
                    machine.Key(k, true);
                }

                RunForAWhile(machine);

                machine.AddBookmark(true);
                HistoryEvent bookmarkEvent = machine.CurrentEvent;
                RunForAWhile(machine);

                // Act and Verify
                machine.JumpToBookmark(bookmarkEvent);
                Assert.AreEqual(bookmarkEvent, machine.CurrentEvent);

                bool[] keys = new bool[80];
                Array.Clear(keys, 0, keys.Length);
                RequestProcessedDelegate auditor = (core, request, action) =>
                {
                    if (request != null && request.Type == CoreRequest.Types.KeyPress)
                    {
                        keys[request.KeyCode] = request.KeyDown;
                    }
                };

                machine.Core.Auditors += auditor;
                ProcessQueueAndStop(machine.Core);

                for (byte j = 0; j < 80; j++)
                {
                    Assert.IsFalse(keys[j]);
                }
            }
        }

        [Test]
        public void DeleteEventInvalidId()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act and Verify - probably better to verify that the history is unchanged...
                Assert.DoesNotThrow(() => machine.DeleteEvent(9999));
            }
        }

        [Test]
        public void SetBookmarkInvalidId()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act and Verify
                Assert.DoesNotThrow(() => machine.SetBookmark(9999, null));
            }
        }

        [Test]
        public void SetCurrentEventInvalidId()
        {
            // Setup
            using (Machine machine = CreateMachine())
            {
                // Act and Verify
                Assert.DoesNotThrow(() => machine.SetCurrentEvent(9999));
            }
        }

        //[Test]
        //public void 
    }
}