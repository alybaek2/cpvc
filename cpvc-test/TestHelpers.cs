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

        static public bool IsKeyRequest(MachineRequest request, byte keycode, bool down)
        {
            return request != null && request.Type == MachineRequest.Types.KeyPress && request.KeyCode == keycode && request.KeyDown == down;
        }

        static public MachineRequest KeyRequest(byte keycode, bool down)
        {
            return It.Is<MachineRequest>(r => IsKeyRequest(r, keycode, down));
        }

        static public MachineAction KeyAction(byte keycode, bool down)
        {
            return It.Is<MachineAction>(r => IsKeyRequest(r, keycode, down));
        }

        static public MachineRequest DiscRequest()
        {
            return It.Is<MachineRequest>(r => r != null && r.Type == MachineRequest.Types.LoadDisc);
        }

        static public MachineAction DiscAction()
        {
            return It.Is<MachineAction>(r => r != null && r.Type == MachineRequest.Types.LoadDisc);
        }

        static public MachineRequest TapeRequest()
        {
            return It.Is<MachineRequest>(r => r != null && r.Type == MachineRequest.Types.LoadTape);
        }

        static public MachineAction TapeAction()
        {
            return It.Is<MachineAction>(r => r != null && r.Type == MachineRequest.Types.LoadTape);
        }

        static public MachineAction RunUntilActionForce()
        {
            return It.Is<MachineAction>(r => r == null || r.Type == MachineRequest.Types.RunUntil);
        }

        static public bool IsResetRequest(MachineRequest request)
        {
            return request != null && request.Type == MachineRequest.Types.Reset;
        }

        static public MachineRequest ResetRequest()
        {
            return It.Is<MachineRequest>(r => r != null && r.Type == MachineRequest.Types.Reset);
        }

        static public MachineAction ResetAction()
        {
            return It.Is<MachineAction>(r => r != null && r.Type == MachineRequest.Types.Reset);
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
        static public UInt64 Run(IPausableMachine machine, UInt64 ticks)
        {
            UInt64 beforeTicks = machine.Ticks;
            UInt64 afterTicks = beforeTicks + ticks;

            ManualResetEvent e = new ManualResetEvent(false);
            //machine.Core.OnIdle += (sender, args) =>
            //{
            //    args.Handled = true;
            //    e.Set();
            //    args.Request = null;
            //};
            machine.PushRequest(MachineRequest.RunUntil(machine.Ticks + ticks));
            machine.Start();

            while (machine.Ticks < afterTicks)
            {
                machine.AdvancePlayback(48000);
                Thread.Sleep(10);
            }

            machine.Stop();

            return machine.Ticks - beforeTicks;
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
                    throw new Exception(String.Format("Waited too long for Machine to run! {0} {1}", machine.Ticks, machine.ActualRunningState));
                }

                System.Threading.Thread.Sleep(sleepTime);
                timeWaited += sleepTime;

                // Empty out the audio buffer to prevent the machine from stalling...
                machine.AdvancePlayback(48000);
            }

            machine.Stop();
            
        }

        static public MachineAction ProcessOneRequest(Core core, MachineRequest request, int timeout = 1000)
        {
            MachineRequest nextRequest = MachineRequest.RunUntil(0);

            MachineAction action = null;
            ManualResetEvent e = new ManualResetEvent(false);

            //core.OnCoreAction += (sender, args) =>
            //{
            //    // Advance the audio playback so RunUntil requests don't stall.
            //    core.AdvancePlayback(100000);

            //    if (ReferenceEquals(sender, core))
            //    {
            //        if (args.Request == nextRequest)
            //        {
            //            e.Set();
            //        }
            //        else if (args.Request == request)
            //        {
            //            action = args.Action;
            //        }
            //    }

            //};

            throw new Exception("Fix me!");
            //core.PushRequest(request);
            //core.PushRequest(nextRequest);

            // Wait for at most one second.
            bool result = e.WaitOne(timeout);
            if (!result)
            {
                throw new TimeoutException("Timeout while waiting for request to process.");
            }

            return action;
        }

        /// <summary>
        /// Returns once all the requests currently in the core's request queue are processed.
        /// </summary>
        /// <param name="core">Core to run.</param>
        static public void WaitForQueueToProcess(Core core)
        {
            // Insert a request that will effectively do nothing and wait for it
            // to be processed.
            ProcessOneRequest(core, MachineRequest.RunUntil(0), 10000);
        }

        static public void ProcessRemoteRequest(RemoteMachine machine, ReceiveCoreActionDelegate receive, MachineAction action)
        {
            receive(action);
            machine.Start();

            bool result = action.Wait(10000);

            machine.Stop();

            if (!result)
            {
                throw new TimeoutException("Timeout while waiting for request to process.");
            }
        }

        static public bool RunUntilAudioOverrun(Machine machine, int timeout)
        {
            int elapsed = 0;
            while ((elapsed < timeout) && machine.AudioBuffer.WaitForUnderrun(0))
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

        static public LocalMachine CreateTestMachine()
        {
            Mock<IFileSystem> mockFileSystem = new Mock<IFileSystem>(MockBehavior.Strict);
            mockFileSystem.Setup(fileSystem => fileSystem.DeleteFile(AnyString()));
            mockFileSystem.Setup(fileSystem => fileSystem.ReplaceFile(AnyString(), AnyString()));
            mockFileSystem.Setup(fileSystem => fileSystem.FileLength(AnyString())).Returns(100);

            MockTextFile mockTextFile = new MockTextFile();
            mockFileSystem.Setup(fileSystem => fileSystem.OpenTextFile(AnyString())).Returns(mockTextFile);

            LocalMachine machine = LocalMachine.New("test", null);

            // For consistency with automated builds, use all zero ROMs.
            //byte[] zeroROM = new byte[0x4000];
            //machine.Core.SetLowerROM(zeroROM);
            //machine.Core.SetUpperROM(0, zeroROM);
            //machine.Core.SetUpperROM(7, zeroROM);

            //machine.Core.OnIdle += (sender, args) =>
            //{
            //    args.Handled = true;
            //    args.Request = CoreRequest.RunUntil(machine.Core.Ticks + 1000);
            //};

            machine.RunUntil(machine.Ticks + 1000);
            machine.Key(Keys.A, true);
            machine.RunUntil(machine.Ticks + 1000);
            machine.Key(Keys.A, false);
            machine.RunUntil(machine.Ticks + 1000);
            machine.LoadDisc(0, null);
            machine.RunUntil(machine.Ticks + 1000);
            machine.LoadTape(null);
            machine.RunUntil(machine.Ticks + 1000);
            machine.AddBookmark(false);
            MachineRequest request = machine.RunUntil(machine.Ticks + 1000);
            machine.Start();
            request.Wait(2000);

            machine.Stop().Wait();
            machine.AddBookmark(false);
            request = machine.RunUntil(machine.Ticks + 1000);
            machine.Start().Wait(2000);

            machine.Stop().Wait();

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

        static public bool CoreRequestsEqual(MachineRequest request1, MachineRequest request2)
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
                case MachineRequest.Types.CoreVersion:
                    return request1.Version == request2.Version;
                case MachineRequest.Types.KeyPress:
                    return request1.KeyCode == request2.KeyCode && request1.KeyDown == request2.KeyDown;
                case MachineRequest.Types.LoadCore:
                    return request1.CoreState.GetBytes().SequenceEqual(request2.CoreState.GetBytes());
                case MachineRequest.Types.LoadDisc:
                    return request1.Drive == request2.Drive && request1.MediaBuffer.GetBytes().SequenceEqual(request2.MediaBuffer.GetBytes());
                case MachineRequest.Types.LoadTape:
                    return request1.MediaBuffer.GetBytes().SequenceEqual(request2.MediaBuffer.GetBytes());
                case MachineRequest.Types.Reset:
                    return true;
                case MachineRequest.Types.RunUntil:
                    return request1.StopTicks == request2.StopTicks;
                case MachineRequest.Types.CreateSnapshot:
                case MachineRequest.Types.RevertToSnapshot:
                case MachineRequest.Types.DeleteSnapshot:
                    return request1.SnapshotId == request2.SnapshotId;
            }

            return false;
        }

        static public bool CoreActionsEqual(MachineAction action1, MachineAction action2)
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

            if (event1.Id != event2.Id)
            {
                return false;
            }

            if (event1.Children.Count != event2.Children.Count)
            {
                return false;
            }

            if (event1.Ticks != event2.Ticks)
            {
                return false;
            }

            // Should be implemented as a virtual Equals method.
            switch (event1)
            {
                case BookmarkHistoryEvent bookmarkEvent1:
                    if (event2 is BookmarkHistoryEvent bookmarkEvent2)
                    {
                        if (!BookmarksEqual(bookmarkEvent1.Bookmark, bookmarkEvent2.Bookmark))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }

                    break;
                case CoreActionHistoryEvent coreActionEvent1:
                    if (event2 is CoreActionHistoryEvent coreActionEvent2)
                    {
                        if (!CoreActionsEqual(coreActionEvent1.CoreAction, coreActionEvent2.CoreAction))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case RootHistoryEvent _:
                    if (!(event2 is RootHistoryEvent))
                    {
                        return false;
                    }
                    break;
                default:
                    throw new Exception(String.Format("Unknown history event type {0}", event1.GetType().Name));
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

        static public bool HistoriesEqual(History history1, History history2)
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

        static public void Wait(Machine machine, RunningState actualRunningState)
        {
            ManualResetEvent e = new ManualResetEvent(false);
            PropertyChangedEventHandler handler = (sender, args) =>
            {
                if (machine.ActualRunningState == actualRunningState)
                {
                    e.Set();
                }
            };

            try
            {
                machine.PropertyChanged += handler;
                if (machine.ActualRunningState != actualRunningState)
                {
                    if (!e.WaitOne(30000))
                    {
                        throw new TimeoutException(String.Format("Timeout while waiting for machine's expected running state '{0}' to match its actual running state '{1}'.", actualRunningState, machine.ActualRunningState));
                    }
                }
            }
            finally
            {
                machine.PropertyChanged -= handler;
            }
        }
    }
}
