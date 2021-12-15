using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    [TestFixture]
    public class LocalMachineTests
    {
        private Mock<IFileSystem> _mockFileSystem;
        private Mock<MachineAuditorDelegate> _mockAuditor;

        private string _filename = "test.cpvc";

        private LocalMachine _machine;

        public LocalMachine CreateMachine()
        {
            LocalMachine machine = LocalMachine.New("test", null, null);
            machine.Auditors += _mockAuditor.Object;

            // For consistency with automated builds, use all zero ROMs.
            byte[] zeroROM = new byte[0x4000];
            machine.Core.SetLowerROM(zeroROM);
            machine.Core.SetUpperROM(0, zeroROM);
            machine.Core.SetUpperROM(7, zeroROM);

            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);

            return machine;
        }

        [SetUp]
        public void Setup()
        {
            _mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            _mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            _mockFileSystem.Setup(fileSystem => fileSystem.FileLength(AnyString())).Returns(100);

            MockTextFile mockTextFile = new MockTextFile();
            mockTextFile.WriteLine("name:Test");
            mockTextFile.WriteLine("key:1,10,58,True");
            mockTextFile.WriteLine("key:2,20,58,False");
            mockTextFile.WriteLine("current:1");
            mockTextFile.WriteLine("deletebranch:2");
            _mockFileSystem.Setup(fs => fs.OpenTextFile(_filename)).Callback(() => mockTextFile.SeekToStart()).Returns(mockTextFile);

            _mockAuditor = new Mock<MachineAuditorDelegate>();

            _machine = CreateMachine();
        }

        [TearDown]
        public void Teardown()
        {
            _machine.Dispose();
            _machine = null;
            _mockFileSystem = null;
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
            _machine.Start();

            Assert.AreEqual(RunningState.Running, _machine.RunningState);

            using (_machine.AutoPause())
            {
                Assert.AreEqual(RunningState.Paused, _machine.RunningState);

                using (_machine.AutoPause())
                {
                    Assert.AreEqual(RunningState.Paused, _machine.RunningState);
                }

                Assert.AreEqual(RunningState.Paused, _machine.RunningState);
            }

            Assert.AreEqual(RunningState.Running, _machine.RunningState);
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
            using (LocalMachine machine = LocalMachine.New("test", null, null))
            {
                machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
                machine.Auditors += _mockAuditor.Object;

                if (createBookmark)
                {
                    RunForAWhile(machine);
                    machine.AddBookmark(false);
                }

                UInt64 ticks = machine.Core.Ticks;
                HistoryEvent bookmarkEvent = machine.History.CurrentEvent;
                byte[] state = machine.Core.GetState();

                RunForAWhile(machine);

                // Act
                machine.JumpToMostRecentBookmark();

                // Verify
                Assert.AreEqual(machine.History.CurrentEvent, bookmarkEvent);
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

        ///// <summary>
        ///// Ensures that a machine opened "lazily" sets the appropriate RequiresOpen property.
        ///// </summary>
        //[Test]
        //public void OpenLazy()
        //{
        //    // Setup
        //    _mockBinaryWriter.Content = new List<byte>
        //    {
        //        0x00,
        //              0x04, 0x00, 0x00, 0x00,
        //              (byte)'T', (byte)'e', (byte)'s', (byte)'t'
        //    };

        //    // Act
        //    using (Machine machine = Machine.Open("Test", "test.cpvc", _mockFileSystem.Object, true))
        //    {
        //        // Verify
        //        Assert.IsTrue(machine.RequiresOpen);
        //        Assert.AreEqual(machine.Filepath, "test.cpvc");
        //        Assert.AreEqual(machine.Name, "Test");
        //    }
        //}

        //[Test]
        //public void OpenInvalidBlockType()
        //{
        //    // Setup
        //    _mockBinaryWriter.Content = new List<byte>
        //    {
        //        0x7f  // Unknown block type - should cause an exception when read.
        //    };

        //    // Act and Verify
        //    Assert.Throws<Exception>(() =>
        //    {
        //        using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false)) { }
        //    });
        //}

        //[Test]
        //public void CanClose([Values(false, true)] bool requiresOpen)
        //{
        //    // Setup
        //    if (requiresOpen)
        //    {
        //        _machine.Close();
        //    }

        //    // Act
        //    bool canClose = _machine.CanClose();

        //    // Verify
        //    Assert.AreEqual(!requiresOpen, canClose);
        //}

        /// <summary>
        /// Ensures an existing machine is opened with the expected state.
        /// </summary>
        //[Test]
        //public void Open()
        //{
        //    // Setup
        //    RunForAWhile(_machine);
        //    _machine.Key(Keys.A, true);
        //    RunForAWhile(_machine);
        //    _machine.LoadDisc(0, null);
        //    RunForAWhile(_machine);
        //    _machine.LoadTape(null);
        //    RunForAWhile(_machine);
        //    _machine.Reset();
        //    RunForAWhile(_machine);
        //    _machine.AddBookmark(false);
        //    HistoryEvent bookmarkEvent = _machine.History.CurrentEvent;
        //    RunForAWhile(_machine);
        //    _machine.JumpToMostRecentBookmark();
        //    HistoryEvent eventToDelete = bookmarkEvent.Children[0];
        //    RunForAWhile(_machine);
        //    _machine.SetBookmark(bookmarkEvent, null);
        //    _machine.TrimTimeline(eventToDelete);
        //    _machine.Close();

        //    using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
        //    {
        //        // Verify
        //        Assert.IsFalse(machine.RequiresOpen);
        //        Assert.AreEqual(machine.Filepath, "test.cpvc");
        //        Assert.AreEqual(machine.Name, "test");

        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, machine.History.RootEvent.Type);
        //        Assert.AreEqual(CoreRequest.Types.CoreVersion, machine.History.RootEvent.CoreAction.Type);
        //        Assert.AreEqual(1, machine.History.RootEvent.Children.Count);

        //        HistoryEvent historyEvent = machine.History.RootEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
        //        Assert.AreEqual(CoreRequest.Types.KeyPress, historyEvent.CoreAction.Type);
        //        Assert.AreEqual(Keys.A, historyEvent.CoreAction.KeyCode);
        //        Assert.IsTrue(historyEvent.CoreAction.KeyDown);
        //        Assert.AreEqual(1, historyEvent.Children.Count);

        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
        //        Assert.AreEqual(CoreRequest.Types.LoadDisc, historyEvent.CoreAction.Type);
        //        Assert.AreEqual(0, historyEvent.CoreAction.Drive);
        //        Assert.IsNull(historyEvent.CoreAction.MediaBuffer.GetBytes());
        //        Assert.AreEqual(1, historyEvent.Children.Count);

        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
        //        Assert.AreEqual(CoreRequest.Types.LoadTape, historyEvent.CoreAction.Type);
        //        Assert.IsNull(historyEvent.CoreAction.MediaBuffer.GetBytes());
        //        Assert.AreEqual(1, historyEvent.Children.Count);

        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
        //        Assert.AreEqual(CoreRequest.Types.Reset, historyEvent.CoreAction.Type);
        //        Assert.AreEqual(1, historyEvent.Children.Count);

        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.Checkpoint, historyEvent.Type);
        //        Assert.IsNull(historyEvent.Bookmark);
        //        Assert.AreEqual(1, historyEvent.Children.Count);

        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.Checkpoint, historyEvent.Type);
        //        Assert.IsNull(historyEvent.Bookmark);
        //        Assert.AreEqual(1, historyEvent.Children.Count);

        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.Checkpoint, historyEvent.Type);
        //        Assert.IsNotNull(historyEvent.Bookmark);

        //        // Opening the machine should add a "Version" event.
        //        historyEvent = historyEvent.Children[0];
        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, historyEvent.Type);
        //        Assert.AreEqual(CoreRequest.Types.CoreVersion, historyEvent.CoreAction.Type);

        //        Assert.AreEqual(historyEvent, machine.History.CurrentEvent);

        //        _mockAuditor.Verify(a => a(It.Is<CoreAction>(c => c.Type == CoreRequest.Types.LoadCore)), Times.Once);
        //    }
        //}

        ///// <summary>
        ///// Ensure that if a machine file is missing the final system bookmark that's written when
        ///// a machine is closed, the machine when opened should set the current event to the most
        ///// recent event that has a bookmark.
        ///// </summary>
        //[Test]
        //public void OpenWithMissingFinalBookmark()
        //{
        //    // Setup
        //    RunForAWhile(_machine);
        //    _machine.AddBookmark(false);
        //    RunForAWhile(_machine);
        //    _machine.LoadDisc(0, null);
        //    RunForAWhile(_machine);

        //    int endpos = _mockBinaryWriter.Content.Count;

        //    _machine.Close();

        //    // Remove the final system bookmark that was added when the machine was closed.
        //    _mockBinaryWriter.Content.RemoveRange(endpos, _mockBinaryWriter.Content.Count - endpos);

        //    // Act
        //    using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
        //    {
        //        // Verify - when opened, the machine should rewind to the bookmark, then add a Version
        //        //          history event at that point.
        //        HistoryEvent bookmarkEvent = machine.History.RootEvent.Children[0];
        //        Assert.AreEqual(2, bookmarkEvent.Children.Count);
        //        Assert.AreEqual(machine.History.CurrentEvent, bookmarkEvent.Children[1]);
        //        Assert.AreEqual(HistoryEvent.Types.CoreAction, bookmarkEvent.Children[1].Type);
        //        Assert.AreEqual(CoreRequest.Types.CoreVersion, bookmarkEvent.Children[1].CoreAction.Type);
        //    }
        //}

        ///// <summary>
        ///// Ensures that if a bookmark has somehow been corrupted, an exception is thrown
        ///// when opening the machine.
        ///// </summary>
        //[Test]
        //public void OpenWithCorruptedFinalBookmark()
        //{
        //    // Setup
        //    RunForAWhile(_machine);
        //    _machine.AddBookmark(true);

        //    Bookmark corruptedBookmark = new Bookmark(true, 5, new byte[] { }, new byte[] { });
        //    _machine.SetBookmark(_machine.History.CurrentEvent, corruptedBookmark);

        //    _machine.Close();

        //    // Act and Verify
        //    Assert.Throws<System.IndexOutOfRangeException>(() =>
        //    {
        //        using (Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
        //        {
        //        }
        //    });
        //}

        /// <summary>
        /// Ensures the correct number of audio samples are generated after running the core.
        /// </summary>
        /// <param name="ticks">The number of ticks to run the core for.</param>
        /// <param name="expectedSamples">The number of audio samples that should be written.</param>
        [TestCase(4UL, 1)]
        [TestCase(250UL, 4)]
        [TestCase(504UL, 7)]
        [TestCase(85416UL, 1025)]
        public void GetAudio(UInt64 ticks, int expectedSamples)
        {
            // Setup
            _machine.Core.SetLowerROM(new byte[0x4000]);
            _machine.Core.SetUpperROM(0, new byte[0x4000]);
            _machine.Core.SetUpperROM(7, new byte[0x4000]);

            // Act
            List<UInt16> audioSamples = new List<UInt16>();
            _machine.Core.RunUntil(ticks, StopReasons.None, audioSamples);

            // Verify
            Assert.AreEqual(expectedSamples, audioSamples.Count);
        }

        [Test]
        public void Toggle()
        {
            // Setup
            _machine.Start();

            // Act
            RunningState state1 = _machine.RunningState;
            _machine.ToggleRunning();
            RunningState state2 = _machine.RunningState;
            _machine.ToggleRunning();

            // Verify
            Assert.AreEqual(RunningState.Running, state1);
            Assert.AreEqual(RunningState.Paused, state2);
            Assert.AreEqual(RunningState.Running, _machine.RunningState);
        }

        //[Test]
        //public void RewriteFile()
        //{
        //    // Setup
        //    RunForAWhile(_machine);
        //    _machine.LoadDisc(0, null);
        //    RunForAWhile(_machine);
        //    _machine.AddBookmark(false);
        //    RunForAWhile(_machine);

        //    HistoryEvent bookmarkEvent = _machine.History.CurrentEvent;

        //    _machine.LoadDisc(0, null);
        //    RunForAWhile(_machine);

        //    HistoryEvent eventToDelete = _machine.History.CurrentEvent;

        //    _machine.JumpToMostRecentBookmark();
        //    _machine.LoadDisc(0, null);
        //    RunForAWhile(_machine);
        //    _machine.JumpToMostRecentBookmark();
        //    _machine.LoadTape(null);
        //    RunForAWhile(_machine);
        //    _machine.TrimTimeline(eventToDelete.Children[0]);
        //    _machine.AddBookmark(false);
        //    _machine.Close();

        //    using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
        //    {
        //        Mock<IMachineFileReader> mockFileReader = new Mock<IMachineFileReader>(MockBehavior.Loose);

        //        MockFileByteStream rewriteTempFile = new MockFileByteStream();
        //        _mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream("test.cpvc.new")).Returns(rewriteTempFile.Object);

        //        MockSequence sequence = new MockSequence();
        //        mockFileReader.InSequence(sequence).Setup(x => x.SetName("test")).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(VersionEvent(0))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CoreActionEvent(1, CoreRequest.Types.LoadDisc))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CheckpointEvent(2))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CoreActionEvent(5, CoreRequest.Types.LoadDisc))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CheckpointEvent(6))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.SetCurrentEvent(2)).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CoreActionEvent(7, CoreRequest.Types.LoadTape))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(CheckpointEvent(9))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.AddHistoryEvent(VersionEvent(10))).Verifiable();
        //        mockFileReader.InSequence(sequence).Setup(x => x.SetCurrentEvent(10)).Verifiable();

        //        // Act
        //        machine.Compact(false);

        //        MachineFile file2 = new MachineFile(rewriteTempFile.Object);
        //        file2.ReadFile(mockFileReader.Object);

        //        // Verify
        //        mockFileReader.Verify();
        //        mockFileReader.VerifyNoOtherCalls();

        //        Assert.AreEqual("Compacted machine file by 0%", machine.Status);
        //    }
        //}

        [Test]
        public void EnableTurbo()
        {
            // Act
            _machine.EnableTurbo(true);
            _machine.Start();
            if (!RunUntilAudioOverrun(_machine.Core, 10000))
            {
                Assert.Fail("Failed to wait for audio overrun.");
            }

            UInt64 turboDuration = _machine.Core.Ticks;

            // Empty out the audio buffer.
            _machine.AdvancePlayback(1000000);

            _machine.EnableTurbo(false);
            if (!RunUntilAudioOverrun(_machine.Core, 10000))
            {
                Assert.Fail("Failed to wait for audio overrun.");
            }

            UInt64 normalDuration = _machine.Core.Ticks - turboDuration;

            // Verify - speed should be at least doubled.
            double actualSpeedFactor = ((double)turboDuration) / ((double)normalDuration);
            Assert.Greater(actualSpeedFactor, 2);
        }

        //[Test]
        //public void CorruptCheckpointBookmark()
        //{
        //    // Setup
        //    _mockBinaryWriter.Content = new List<byte>
        //    {
        //        0x00,
        //              0x04, 0x00, 0x00, 0x00,
        //              (byte)'T', (byte)'e', (byte)'s', (byte)'t',
        //        0x05,
        //              0x00, 0x00, 0x00, 0x00,
        //              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0x01,
        //              0x00,
        //              0x01, 0x01, 0x00, 0x00, 0x00  // State blob length is 1, but no actual blob bytes exist... should cause an exception!
        //    };

        //    // Act and Verify
        //    Assert.That(() => Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false), Throws.Exception);
        //}

        //[Test]
        //public void SetBookmarkOnNonCheckpoint()
        //{
        //    // Setup
        //    _mockBinaryWriter.Content = new List<byte>
        //    {
        //        0x00,
        //              0x04, 0x00, 0x00, 0x00,
        //              (byte)'T', (byte)'e', (byte)'s', (byte)'t',
        //        0x05,
        //              0x00, 0x00, 0x00, 0x00,
        //              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0x00,
        //        0x01,
        //              0x01, 0x00, 0x00, 0x00,
        //              0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0xba
        //    };

        //    using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
        //    {
        //        int pos = _mockBinaryWriter.Content.Count;

        //        // Act
        //        machine.SetBookmark(machine.History.RootEvent.Children[0], null);

        //        // Verify
        //        Assert.IsNotNull(machine.History.RootEvent);
        //        Assert.GreaterOrEqual(machine.History.RootEvent.Children.Count, 1);
        //        Assert.IsEmpty(machine.History.RootEvent.Children[0].Children);
        //        Assert.AreEqual(pos, _mockBinaryWriter.Content.Count);
        //    }
        //}

        //[Test]
        //public void DeleteRootEvent()
        //{
        //    // Setup
        //    _mockBinaryWriter.Content = new List<byte>
        //    {
        //        0x00,
        //              0x04, 0x00, 0x00, 0x00,
        //              (byte)'T', (byte)'e', (byte)'s', (byte)'t',
        //        0x05,
        //              0x00, 0x00, 0x00, 0x00,
        //              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0x00,
        //        0x01,
        //              0x01, 0x00, 0x00, 0x00,
        //              0x64, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        //              0xba,
        //        0x06,
        //              0x00, 0x00, 0x00, 0x00
        //    };

        //    // Act
        //    using (Machine machine = Machine.Open("test", "test.cpvc", _mockFileSystem.Object, false))
        //    {
        //        // Verify
        //        Assert.IsNotNull(machine.History.RootEvent);
        //        Assert.IsNotEmpty(machine.History.RootEvent.Children);
        //    }
        //}

        //[Test]
        //public void TrimTimelineRoot()
        //{
        //    // Setup
        //    int pos = _mockBinaryWriter.Content.Count;

        //    // Act
        //    _machine.TrimTimeline(_machine.History.RootEvent);

        //    // Verify
        //    Assert.IsNotNull(_machine.History.RootEvent);
        //    Assert.IsEmpty(_machine.History.RootEvent.Children);
        //    Assert.AreEqual(pos, _mockBinaryWriter.Content.Count);
        //}

        [Test]
        public void Volume()
        {
            // Act
            _machine.Volume = 100;

            // Verify
            Assert.AreEqual(100, _machine.Volume);
        }

        [Test]
        public void RunningNoCore()
        {
            // Act
            _machine.Close();

            // Verify
            Assert.AreEqual(RunningState.Paused, _machine.RunningState);
        }

        [Test]
        public void TicksNoCore()
        {
            // Act
            RunForAWhile(_machine);
            _machine.Close();

            // Verify
            Assert.Zero(_machine.Ticks);
        }

        [Test]
        public void AdvancePlaybackNoCore()
        {
            // Setup
            _machine.Close();

            // Act and Verify
            Assert.DoesNotThrow(() => _machine.AdvancePlayback(1));
        }

        [Test]
        public void ReadAudioNoCore()
        {
            // Setup
            _machine.Close();

            // Act and Verify
            Assert.DoesNotThrow(() => _machine.ReadAudio(null, 0, 1));
        }

        [Test]
        public void SetSameCore()
        {
            // Act and Verify - note that if CoreMachine.Core_set didn't do a check for reference
            //                  equality between the new core and current core, an exception would
            //                  later be thrown due to Dispose() being called.
            Assert.DoesNotThrow(() =>
            {
                _machine.Core = _machine.Core;
            });
        }

        //[Test]
        //public void SetCurrentEvent()
        //{
        //    // Setup
        //    for (byte k = 0; k < 80; k++)
        //    {
        //        _machine.Key(k, true);
        //    }

        //    RunForAWhile(_machine);

        //    _machine.AddBookmark(true);
        //    HistoryEvent bookmarkEvent = _machine.History.CurrentEvent;
        //    RunForAWhile(_machine);

        //    // Act and Verify
        //    _machine.JumpToBookmark(bookmarkEvent);
        //    Assert.AreEqual(bookmarkEvent, _machine.History.CurrentEvent);

        //    bool[] keys = new bool[80];
        //    Array.Clear(keys, 0, keys.Length);
        //    RequestProcessedDelegate auditor = (core, request, action) =>
        //    {
        //        if (request != null && request.Type == CoreRequest.Types.KeyPress)
        //        {
        //            keys[request.KeyCode] = request.KeyDown;
        //        }
        //    };

        //    _machine.Core.Auditors += auditor;
        //    _machine.Start();

        //    WaitForQueueToProcess(_machine.Core);

        //    for (byte j = 0; j < 80; j++)
        //    {
        //        Assert.IsFalse(keys[j]);
        //    }
        //}

        //[Test]
        //public void DeleteEventInvalidId()
        //{
        //    // Act and Verify - probably better to verify that the history is unchanged...
        //    Assert.DoesNotThrow(() => _machine.DeleteEvent(9999));
        //}

        //[Test]
        //public void SetBookmarkInvalidId()
        //{
        //    // Act and Verify
        //    Assert.DoesNotThrow(() => _machine.SetBookmark(9999, null));
        //}

        //[Test]
        //public void SetCurrentEventInvalidId()
        //{
        //    // Act and Verify
        //    Assert.DoesNotThrow(() => _machine.SetCurrentEvent(9999));
        //}

        [TestCase(0, 1, true)]
        [TestCase(100, 101, true)]
        [TestCase(255, 0, true)]
        [TestCase(255, 255, false)]
        public void SetVolume(byte volume1, byte volume2, bool notified)
        {
            // Setup
            _machine.Volume = volume1;
            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            _machine.PropertyChanged += propChanged.Object;

            // Act
            _machine.Volume = volume2;

            // Verify
            Assert.AreEqual(_machine.Volume, volume2);
            if (notified)
            {
                propChanged.Verify(PropertyChanged(_machine, "Volume"), Times.Once);
            }

            propChanged.VerifyNoOtherCalls();
        }

        [Test]
        public void Reverse()
        {
            // Setup

            // Run for long enough to generate one snapshot, so that we can enter reverse mode.
            RunForAWhile(_machine, 1000000, 60000);
            UInt64 ticksAfter = _machine.Ticks;

            // Act
            _machine.Reverse();

            byte[] buffer = new byte[48000];
            _machine.ReadAudio(buffer, 0, buffer.Length / 4);

            // Verify - this test is incomplete. Need checks for reversal of audio
            //          samples. Probably easier to do this once the Core class is
            //          hidden behind an interface and can be mocked.
            Assert.AreEqual(RunningState.Reverse, _machine.RunningState);
            Assert.Less(_machine.Ticks, ticksAfter);
        }

        /// <summary>
        /// Test to ensure that multiple Reverse calls only require a single ReverseStop
        /// call to get back to the original running state.
        /// </summary>
        [Test]
        public void ReverseTwice()
        {
            // Setup

            // Run for long enough to generate one snapshot, so that we can enter reverse mode.
            RunForAWhile(_machine, 1000000, 60000);

            _machine.SetRunningState(RunningState.Running);
            _machine.Reverse();
            _machine.Reverse();

            // Act
            _machine.ReverseStop();

            // Verify
            Assert.AreEqual(RunningState.Running, _machine.RunningState);
        }

        [TestCase(RunningState.Paused)]
        [TestCase(RunningState.Running)]
        public void ReverseStop(RunningState runningState)
        {
            // Setup

            // Run for long enough to generate one snapshot, so that we can enter reverse mode.
            RunForAWhile(_machine, 100000, 6000);

            _machine.SetRunningState(runningState);
            _machine.Reverse();

            // Act
            _machine.ReverseStop();

            // Verify
            Assert.AreEqual(runningState, _machine.RunningState);
        }

        /// <summary>
        /// Ensures that a newly-created machine has an IdleRequest handler. This is checked indirectly by
        /// running a machine and ensuring the Ticks property increases.
        /// </summary>
        [Test]
        public void NewMachineHasIdleRequestHandler()
        {
            // Setup
            using (LocalMachine machine = LocalMachine.New("test", null, null))
            {
                // Act
                RunForAWhile(machine);

                // Verify
                Assert.Greater(machine.Ticks, 0);
            }
        }

        [Test]
        public void CantCompact()
        {
            // Act and Verify
            Assert.Throws<Exception>(() => _machine.Compact(_mockFileSystem.Object));
        }

        [Test]
        public void Compact()
        {
            // Setup
            string tmpFilename = String.Format("{0}.tmp", _filename);
            LocalMachine machine = LocalMachine.OpenFromFile(_mockFileSystem.Object, _filename);
            machine.Close();

            MockTextFile mockNewTextFile = new MockTextFile();
            _mockFileSystem.Setup(fs => fs.OpenTextFile(tmpFilename)).Returns(mockNewTextFile);

            // Act
            machine.Compact(_mockFileSystem.Object);

            // Verify
            int keyLineCount = 0;
            string line;
            while ((line = mockNewTextFile.ReadLine()) != null)
            {
                if (line.StartsWith("key:"))
                {
                    keyLineCount++;
                }
            }

            Assert.AreEqual(1, keyLineCount);
            _mockFileSystem.Verify(fs => fs.ReplaceFile(_filename, tmpFilename), Times.Once());
        }

        [Test]
        public void SnapshotLimitPropertyChanged()
        {
            // Setup
            LocalMachine machine = LocalMachine.OpenFromFile(_mockFileSystem.Object, _filename);
            Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
            machine.PropertyChanged += propChanged.Object;

            // Act - note that setting the property to itself should not trigger the "property changed" event.
            machine.SnapshotLimit = machine.SnapshotLimit;
            machine.SnapshotLimit = machine.SnapshotLimit + 42;

            // Verify
            propChanged.Verify(p => p(machine, It.Is<PropertyChangedEventArgs>(e => e.PropertyName == nameof(machine.SnapshotLimit))), Times.Once());
        }

        [Test]
        public void NoPropertyChangedHandlers()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("Test", null, null);

            // Act and Verify
            Assert.DoesNotThrow(() => machine.SnapshotLimit = machine.SnapshotLimit + 42);
        }

        [Test]
        public void CanStart()
        {
            // Setup
            _machine.Stop();

            // Verify
            Assert.True(_machine.CanStart);
        }

        [Test]
        public void CantStart()
        {
            // Setup
            _machine.Start();

            // Verify
            Assert.False(_machine.CanStart);
        }

        [Test]
        public void CantStartClosedMachine()
        {
            // Setup
            _machine.Close();

            // Verify
            Assert.False(_machine.CanStart);
        }

        [Test]
        public void CantStopClosedMachine()
        {
            // Setup
            _machine.Close();

            // Verify
            Assert.False(_machine.CanStop);
        }

        [Test]
        public void ToggleSnapshotLimitOff()
        {
            // Setup
            _machine.SnapshotLimit = 100;

            // Act
            _machine.ToggleReversibilityEnabled();

            // Verify
            Assert.Zero(_machine.SnapshotLimit);
        }

        [Test]
        public void ToggleSnapshotLimitOn()
        {
            // Setup
            _machine.SnapshotLimit = 0;

            // Act
            _machine.ToggleReversibilityEnabled();

            // Verify
            Assert.NotZero(_machine.SnapshotLimit);
        }

        [Test]
        public void PersistAlreadyPersistedMachine()
        {
            // Setup
            LocalMachine machine = LocalMachine.OpenFromFile(_mockFileSystem.Object, _filename);

            // Act and Verify
            Assert.Throws<InvalidOperationException>(() => machine.Persist(_mockFileSystem.Object, _filename));
        }

        [Test]
        public void PersistToEmptyFilename()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("Test", null, null);

            // Act and Verify
            Assert.Throws<ArgumentException>(() => machine.Persist(_mockFileSystem.Object, ""));
        }

        [Test]
        public void SnapshotLimit()
        {
            // Setup
            LocalMachine machine = LocalMachine.New("Test", null, null);
            machine.SnapshotLimit = 2;
            machine.Core.PushRequest(CoreRequest.CreateSnapshot(1000000));
            machine.Core.PushRequest(CoreRequest.CreateSnapshot(1000001));

            // Act
            TestHelpers.Run(machine, 400000);
            machine.Start();

            // Verify
            CoreRequest request1 = CoreRequest.DeleteSnapshot(1000000);
            CoreRequest request2 = CoreRequest.DeleteSnapshot(1000001);
            CoreAction action1 = TestHelpers.ProcessOneRequest(machine.Core, request1, 2000);
            CoreAction action2 = TestHelpers.ProcessOneRequest(machine.Core, request2, 2000);
            machine.Stop();

            Assert.Null(action1);
            Assert.Null(action2);
        }
    }
}