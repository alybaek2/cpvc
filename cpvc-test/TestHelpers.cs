using Moq;
using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace CPvC.Test
{
    static public class TestHelpers
    {
        /// <summary>
        /// Effectively a Times value representing 0 or more times.
        /// </summary>
        /// <returns>A Times value representing 0 or more times.</returns>
        static public Times AnyTimes()
        {
            return Times.AtMost(int.MaxValue);
        }

        static public string AnyString()
        {
            return It.IsAny<string>();
        }

        static public CoreRequest KeyRequest(byte keycode, bool down)
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreRequest.Types.KeyPress && r.KeyCode == keycode && r.KeyDown == down);
        }

        static public CoreAction KeyAction(byte keycode, bool down)
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreRequest.Types.KeyPress && r.KeyCode == keycode && r.KeyDown == down);
        }

        static public CoreRequest DiscRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreRequest.Types.LoadDisc);
        }

        static public CoreAction DiscAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreRequest.Types.LoadDisc);
        }

        static public CoreRequest TapeRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreRequest.Types.LoadTape);
        }

        static public CoreAction TapeAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreRequest.Types.LoadTape);
        }

        static public CoreAction RunUntilActionForce()
        {
            return It.Is<CoreAction>(r => r == null || r.Type == CoreRequest.Types.RunUntil);
        }

        static public CoreRequest ResetRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreRequest.Types.Reset);
        }

        static public CoreAction ResetAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreRequest.Types.Reset);
        }

        //static public HistoryEvent CoreActionEvent(int id, CoreRequest.Types type)
        //{
        //    return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.CoreAction && h.CoreAction.Type == type && h.Id == id);
        //}

        //static public HistoryEvent CoreActionEvent(int id, UInt64 ticks, CoreRequest.Types type)
        //{
        //    return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.CoreAction && h.CoreAction.Type == type && h.Ticks == ticks && h.Id == id);
        //}

        //static public HistoryEvent KeyPressEvent(int id, UInt64 ticks, byte keyCode, bool keyDown)
        //{
        //    return It.Is<HistoryEvent>(h => h != null &&
        //                                    h.Type == HistoryEvent.Types.CoreAction &&
        //                                    h.CoreAction.Type == CoreRequest.Types.KeyPress &&
        //                                    h.CoreAction.KeyCode == keyCode &&
        //                                    h.CoreAction.KeyDown == keyDown &&
        //                                    h.Ticks == ticks &&
        //                                    h.Id == id);
        //}

        //static public HistoryEvent LoadDiscEvent(int id, UInt64 ticks, byte drive, byte[] disc)
        //{
        //    return It.Is<HistoryEvent>(h => h != null &&
        //                                    h.Type == HistoryEvent.Types.CoreAction &&
        //                                    h.CoreAction.Type == CoreRequest.Types.LoadDisc &&
        //                                    h.CoreAction.Drive == drive &&
        //                                    h.CoreAction.MediaBuffer.GetBytes().SequenceEqual(disc) &&
        //                                    h.Ticks == ticks &&
        //                                    h.Id == id);
        //}

        //static public HistoryEvent LoadTapeEvent(int id, UInt64 ticks, byte[] tape)
        //{
        //    return It.Is<HistoryEvent>(h => h != null &&
        //                                    h.Type == HistoryEvent.Types.CoreAction &&
        //                                    h.CoreAction.Type == CoreRequest.Types.LoadTape &&
        //                                    h.CoreAction.MediaBuffer.GetBytes().SequenceEqual(tape) &&
        //                                    h.Ticks == ticks &&
        //                                    h.Id == id);
        //}

        static public Bookmark BookmarkMatch(bool system, int version, int statePos, int screenPos)
        {
            return It.Is<Bookmark>(
                b => b != null &&
                b.System == system &&
                b.Version == version &&
                ((IStreamBlob)(b.State)).Position == statePos &&
                ((IStreamBlob)(b.Screen)).Position == screenPos);
        }

        //static public HistoryEvent VersionEvent(int id)
        //{
        //    return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.CoreAction && h.CoreAction != null && h.CoreAction.Type == CoreRequest.Types.CoreVersion && h.Id == id);
        //}

        //static public HistoryEvent CheckpointEvent(int id)
        //{
        //    return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.Checkpoint && h.Id == id);
        //}

        //static public HistoryEvent CheckpointWithBookmarkEvent(int id, UInt64 ticks, bool system, int version, int stateBlobPos, int screenBlobPos)
        //{
        //    return It.Is<HistoryEvent>(h => h != null &&
        //                                    h.Type == HistoryEvent.Types.Checkpoint &&
        //                                    h.Id == id &&
        //                                    h.Ticks == ticks &&
        //                                    h.Bookmark != null &&
        //                                    h.Bookmark.System == system &&
        //                                    h.Bookmark.Version == version &&
        //                                    ((IStreamBlob)(h.Bookmark.State)).Position == stateBlobPos &&
        //                                    ((IStreamBlob)(h.Bookmark.Screen)).Position == screenBlobPos);
        //}

        //static public HistoryEvent CheckpointWithoutBookmarkEvent(int id, UInt64 ticks)
        //{
        //    return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.Checkpoint && h.Id == id && h.Ticks == ticks && h.Bookmark == null);
        //}

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.ReadBytes method with a specific filename.
        /// </summary>
        static public Expression<Func<IFileSystem, byte[]>> ReadBytes(string filename)
        {
            return fileSystem => fileSystem.ReadBytes(filename);
        }

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.ReadBytes method with any filename.
        /// </summary>
        static public Expression<Func<IFileSystem, byte[]>> ReadBytes()
        {
            return fileSystem => fileSystem.ReadBytes(AnyString());
        }

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.DeleteFile method with a specific filename.
        /// </summary>
        static public Expression<Action<IFileSystem>> DeleteFile(string filename)
        {
            return fileSystem => fileSystem.DeleteFile(filename);
        }

        /// <summary>
        /// Returns a mock delegate for checking property change events.
        /// </summary>
        /// <param name="source">The expected source of the property changed event.</param>
        /// <param name="name">The expected name of the property that was changed.</param>
        /// <returns>A mock delegate for checking property change events.</returns>
        static public Expression<Action<PropertyChangedEventHandler>> PropertyChanged(object source, string name)
        {
            return propChanged => propChanged(source, It.Is<PropertyChangedEventArgs>(args => args.PropertyName == name));
        }

        /// <summary>
        /// Runs a Machine's core for a certain number of ticks, and optionally stopping on an audio overrun.
        /// </summary>
        /// <param name="machine">The machine whose core should be run.</param>
        /// <param name="ticks">The number of ticks to run the machine for.</param>
        /// <returns>The total number of ticks that the machine ran for. Note this may be slightly larger than <c>ticks</c>, since Z80 instructions take at least 4 ticks.</returns>
        static public UInt64 Run(ICoreMachine machine, UInt64 ticks)
        {
            UInt64 beforeTicks = machine.Core.Ticks;

            ManualResetEvent e = new ManualResetEvent(false);
            machine.Core.IdleRequest = () =>
            {
                e.Set();
                return null;
            };

            machine.Core.PushRequest(CoreRequest.RunUntil(machine.Ticks + ticks));
            machine.Core.Start();

            while (!e.WaitOne(10))
            {
                machine.AdvancePlayback(48000);
            }

            machine.Core.Stop();
            machine.Core.IdleRequest = null;

            return machine.Core.Ticks - beforeTicks;
        }

        /// <summary>
        /// Runs a machine for at least one instruction cycle.
        /// </summary>
        /// <param name="machine">The machine to run.</param>
        static public void RunForAWhile(IPausableMachine machine, UInt32 ticksDuration = 1, int timeout = 1000)
        {
            UInt64 startTicks = machine.Ticks;
            UInt64 endTicks = startTicks + ticksDuration;

            int timeWaited = 0;
            int sleepTime = 10;
            machine.Start();
            while (machine.Ticks < endTicks)
            {
                if (timeWaited > timeout)
                {
                    throw new Exception(String.Format("Waited too long for Machine to run! {0} {1}", machine.Ticks, machine.RunningState));
                }

                System.Threading.Thread.Sleep(sleepTime);
                timeWaited += sleepTime;

                // Empty out the audio buffer to prevent the machine from stalling...
                machine.AdvancePlayback(48000);
            }

            machine.Stop();
        }

        /// <summary>
        /// Enqueues a request in a core and waits for it to be processed. If <c>request</c> is null, this method waits for the next request to be processed.
        /// </summary>
        /// <param name="core">Core to run.</param>
        /// <param name="request">Request to be processed.</param>
        static public void ProcessRequest(Core core, CoreRequest request)
        {
            core.PushRequest(request);
            WaitForQueueToProcess(core);
        }

        /// <summary>
        /// Returns once all the requests currently in the core's request queue are processed.
        /// </summary>
        /// <param name="core">Core to run.</param>
        static public void WaitForQueueToProcess(Core core)
        {
            // Insert a request that will effectively do nothing and wait for it
            // to be processed.
            CoreRequest request = CoreRequest.RunUntil(0);
            ManualResetEvent e = new ManualResetEvent(false);
            RequestProcessedDelegate processed = (c, r, a) =>
            {
                // Advance the audio playback so RunUntil requests don't stall.
                core.AdvancePlayback(100000);

                if (c == core && (request == null || r == request))
                {
                    e.Set();
                }
            };

            core.Auditors += processed;
            core.PushRequest(request);

            // Wait for at most one second.
            bool result = e.WaitOne(1000);

            core.Auditors -= processed;
            if (!result)
            {
                throw new TimeoutException("Timeout while waiting for request to process.");
            }
        }

        static public void ProcessRemoteRequest(RemoteMachine machine, ReceiveCoreActionDelegate receive, CoreAction action)
        {
            ManualResetEvent e = new ManualResetEvent(false);
            RequestProcessedDelegate processed = (c, r, a) =>
            {
                if (c == machine.Core && (r == null || r == action))
                {
                    e.Set();
                }
            };

            machine.Core.Auditors += processed;

            receive(action);
            machine.Start();

            bool result = e.WaitOne(1000);

            machine.Core.Auditors -= processed;

            machine.Stop();

            if (!result)
            {
                throw new TimeoutException("Timeout while waiting for request to process.");
            }
        }

        static public bool WaitForNextRequestProcessed(Core core)
        {
            try
            {
                ProcessRequest(core, null);
            }
            catch (TimeoutException)
            {
                return false;
            }

            return true;
        }

        static public bool RunUntilAudioOverrun(Core core, int timeout)
        {
            int elapsed = 0;
            while ((elapsed < timeout) && core.AudioBuffer.WaitForUnderrun(0))
            {
                Thread.Sleep(10);
                elapsed += 10;
            }

            return (elapsed < timeout);
        }

        static public string GetTempFilepath(string filename)
        {
            return String.Format("{0}\\{1}", System.IO.Path.GetTempPath(), filename);
        }

        static public Machine CreateTestMachine()
        {
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            mockFileSystem.Setup(fileSystem => fileSystem.FileLength(AnyString())).Returns(100);

            MockFileByteStream mockBinaryWriter = new MockFileByteStream();

            mockFileSystem.Setup(fileSystem => fileSystem.OpenFileByteStream("test.cpvc")).Returns(mockBinaryWriter.Object);

            Machine machine = Machine.Create("test", null);

            // For consistency with automated builds, use all zero ROMs.
            byte[] zeroROM = new byte[0x4000];
            machine.Core.SetLowerROM(zeroROM);
            machine.Core.SetUpperROM(0, zeroROM);
            machine.Core.SetUpperROM(7, zeroROM);

            machine.Core.IdleRequest = () => CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            
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
            RunForAWhile(machine);
            machine.AddBookmark(false);
            RunForAWhile(machine);

            return machine;
        }

        static public bool ByteArraysEqual(byte[] bytes1, byte[] bytes2)
        {
            if (bytes1 == bytes2)
            {
                return true;
            }

            if (bytes1 == null || bytes2 == null)
            {
                return false;
            }

            return bytes1.SequenceEqual(bytes2);
        }

        static public bool BookmarksEqual(Bookmark bookmark1, Bookmark bookmark2)
        {
            if (bookmark1 == bookmark2)
            {
                return true;
            }

            if (bookmark1 == null || bookmark2 == null)
            {
                return false;
            }

            if (bookmark1.System != bookmark2.System)
            {
                return false;
            }

            if (bookmark1.Version != bookmark2.Version)
            {
                return false;
            }

            if (!ByteArraysEqual(bookmark1.Screen.GetBytes(), bookmark2.Screen.GetBytes()))
            {
                return false;
            }

            if (!ByteArraysEqual(bookmark1.State.GetBytes(), bookmark2.State.GetBytes()))
            {
                return false;
            }

            return true;

        }

        static public bool CoreRequestsEqual(CoreRequest request1, CoreRequest request2)
        {
            if (request1 == request2)
            {
                return true;
            }

            if (request1 == null || request2 == null)
            {
                return false;
            }

            if (request1.Type != request2.Type)
            {
                return false;
            }

            switch (request1.Type)
            {
                case CoreRequest.Types.CoreVersion:
                    return request1.Version == request2.Version;
                case CoreRequest.Types.KeyPress:
                    return request1.KeyCode == request2.KeyCode && request1.KeyDown == request2.KeyDown;
                case CoreRequest.Types.LoadCore:
                    return request1.CoreState.GetBytes().SequenceEqual(request2.CoreState.GetBytes());
                case CoreRequest.Types.LoadDisc:
                    return request1.Drive == request2.Drive && request1.MediaBuffer.GetBytes().SequenceEqual(request2.MediaBuffer.GetBytes());
                case CoreRequest.Types.LoadTape:
                    return request1.MediaBuffer.GetBytes().SequenceEqual(request2.MediaBuffer.GetBytes());
                case CoreRequest.Types.Quit:
                    return true;
                case CoreRequest.Types.Reset:
                    return true;
                case CoreRequest.Types.RunUntil:
                    return request1.StopTicks == request2.StopTicks;
                case CoreRequest.Types.CreateSnapshot:
                case CoreRequest.Types.RevertToSnapshot:
                case CoreRequest.Types.DeleteSnapshot:
                    return request1.SnapshotId == request2.SnapshotId;
            }

            return false;
        }

        static public bool CoreActionsEqual(CoreAction action1, CoreAction action2)
        {
            if (!CoreRequestsEqual(action1, action2))
            {
                return false;
            }

            if (action1 == action2)
            {
                return true;
            }

            if (action1 == null || action2 == null)
            {
                return false;
            }

            if (action1.Type != action2.Type)
            {
                return false;
            }

            if (action1.Ticks != action2.Ticks)
            {
                return false;
            }

            return true;
        }

        static public bool HistoryEventsEqual(HistoryEvent event1, HistoryEvent event2)
        {
            if (event1 == event2)
            {
                return true;
            }

            if (event1 == null || event2 == null)
            {
                return false;
            }

            if (event1.Children.Count != event2.Children.Count)
            {
                return false;
            }

            if (event1.Type != event2.Type)
            {
                return false;
            }

            switch (event1.Type)
            {
                case HistoryEventType.AddBookmark:
                    if (!BookmarksEqual(event1.Bookmark, event2.Bookmark))
                    {
                        return false;
                    }
                    break;
                case HistoryEventType.AddCoreAction:
                    if (!CoreActionsEqual(event1.CoreAction, event2.CoreAction))
                    {
                        return false;
                    }
                    break;
                case HistoryEventType.None:
                    break;
                default:
                    throw new Exception(String.Format("Unknown history event type {0}", event1.Type));
            }

            // This assumes the children are in the same order. Might need to change this in the future...
            for (int i = 0; i < event1.Children.Count; i++)
            {
                if (!HistoryEventsEqual(event1.Children[i], event2.Children[i]))
                {
                    return false;
                }
            }

            return true;
        }

        static public bool HistoriesEqual(MachineHistory history1, MachineHistory history2)
        {
            if (history1 == history2)
            {
                return true;
            }

            if (history1 == null || history2 == null)
            {
                return false;
            }

            return HistoryEventsEqual(history1.RootEvent, history2.RootEvent);
        }
    }
}
