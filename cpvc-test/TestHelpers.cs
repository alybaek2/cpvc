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

        public class UnknownRequest : MachineRequest
        {
        }

        public class UnknownAction : IMachineAction
        {
            public UInt64 Ticks { get; }
        }

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

        /// <summary>
        /// Returns a lambda representing the invocation of the IFileSystem.ReadBytes method with any filename.
        /// </summary>
        static public Expression<Func<IFileSystem, byte[]>> ReadBytes()
        {
            return fileSystem => fileSystem.ReadBytes(AnyString());
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
            machine.PushRequest(new RunUntilRequest(machine.Ticks + ticks));
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
                    throw new Exception(String.Format("Waited too long for Machine to run! {0} {1}", machine.Ticks, machine.RunningState));
                }

                System.Threading.Thread.Sleep(sleepTime);
                timeWaited += sleepTime;

                // Empty out the audio buffer to prevent the machine from stalling...
                machine.AdvancePlayback(48000);
            }

            machine.Stop();
            
        }

        static public void ProcessRemoteRequest(RemoteMachine machine, ReceiveCoreActionDelegate receive, IMachineAction action)
        {
            MachineRequest request = receive(action);
            machine.Start();

            bool result = request.Wait(10000);

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

            if (request1.GetType() != request2.GetType())
            {
                return false;
            }

            switch (request1)
            {
                case CoreVersionRequest coreVersionRequest1:
                    return coreVersionRequest1.Version == ((CoreVersionRequest)request2).Version;
                case KeyPressRequest keyPressRequest1:
                    {
                        KeyPressRequest keyPressRequest2 = (KeyPressRequest)request2;
                        return keyPressRequest1.KeyCode == keyPressRequest2.KeyCode && keyPressRequest1.KeyDown == keyPressRequest2.KeyDown;
                    }
                case LoadCoreRequest loadCoreRequest1:
                    {
                        LoadCoreRequest loadCoreRequest2 = (LoadCoreRequest)request2;
                        return loadCoreRequest1.State.GetBytes().SequenceEqual(loadCoreRequest2.State.GetBytes());
                    }
                case LoadDiscRequest loadDiscRequest1:
                    {
                        LoadDiscRequest loadDiscRequest2 = (LoadDiscRequest)request2;
                        return loadDiscRequest1.Drive == loadDiscRequest2.Drive && loadDiscRequest1.MediaBuffer.GetBytes().SequenceEqual(loadDiscRequest2.MediaBuffer.GetBytes());
                    }
                case LoadTapeRequest loadTapeRequest1:
                    {
                        LoadTapeRequest loadTapeRequest2 = (LoadTapeRequest)request2;
                        return loadTapeRequest1.MediaBuffer.GetBytes().SequenceEqual(loadTapeRequest2.MediaBuffer.GetBytes());
                    }
                case ResetRequest _:
                    return true;
                case RunUntilRequest runUntilRequest1:
                    {
                        RunUntilRequest runUntilRequest2 = (RunUntilRequest)request2;
                        return runUntilRequest1.StopTicks == runUntilRequest2.StopTicks;
                    }
                case CreateSnapshotRequest createSnapshotRequest1:
                    {
                        CreateSnapshotRequest createSnapshotRequest2 = (CreateSnapshotRequest)request2;
                        return createSnapshotRequest1.SnapshotId == createSnapshotRequest2.SnapshotId;
                    }
                case DeleteSnapshotRequest deleteSnapshotRequest1:
                    {
                        DeleteSnapshotRequest deleteSnapshotRequest2 = (DeleteSnapshotRequest)request2;
                        return deleteSnapshotRequest1.SnapshotId == deleteSnapshotRequest2.SnapshotId;
                    }
                case RevertToSnapshotRequest revertToSnapshotRequest1:
                    {
                        RevertToSnapshotRequest revertToSnapshotRequest2 = (RevertToSnapshotRequest)request2;
                        return revertToSnapshotRequest1.SnapshotId == revertToSnapshotRequest2.SnapshotId;
                    }
            }

            return false;
        }

        static public bool CoreActionsEqual(IMachineAction action1, IMachineAction action2)
        {
            if (!CoreRequestsEqual((MachineRequest)action1, (MachineRequest)action2))
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

            if (action1.GetType() != action2.GetType())
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
                if (machine.RunningState == actualRunningState)
                {
                    e.Set();
                }
            };

            try
            {
                machine.PropertyChanged += handler;
                if (machine.RunningState != actualRunningState)
                {
                    if (!e.WaitOne(30000))
                    {
                        throw new TimeoutException(String.Format("Timeout while waiting for machine's expected running state '{0}' to match its actual running state '{1}'.", actualRunningState, machine.RunningState));
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
