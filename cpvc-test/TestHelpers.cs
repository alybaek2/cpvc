using Moq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;

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
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.KeyPress && r.KeyCode == keycode && r.KeyDown == down);
        }

        static public CoreAction KeyAction(byte keycode, bool down)
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.KeyPress && r.KeyCode == keycode && r.KeyDown == down);
        }

        static public CoreRequest DiscRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.LoadDisc);
        }

        static public CoreAction DiscAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.LoadDisc);
        }

        static public CoreRequest TapeRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.LoadTape);
        }

        static public CoreAction TapeAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.LoadTape);
        }

        static public CoreRequest RunUntilRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.RunUntil);
        }

        static public CoreAction RunUntilAction()
        {
            return It.Is<CoreAction>(r => r == null || r.Type == CoreActionBase.Types.RunUntil);
        }

        static public CoreRequest ResetRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.Reset);
        }

        static public CoreAction ResetAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.Reset);
        }

        static public HistoryEvent CoreActionEvent(int id, CoreActionBase.Types type)
        {
            return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.CoreAction && h.CoreAction.Type == type && h.Id == id);
        }

        static public HistoryEvent CoreActionEvent(int id, UInt64 ticks, CoreActionBase.Types type)
        {
            return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.CoreAction && h.CoreAction.Type == type && h.Ticks == ticks && h.Id == id);
        }

        static public HistoryEvent KeyPressEvent(int id, UInt64 ticks, byte keyCode, bool keyDown)
        {
            return It.Is<HistoryEvent>(h => h != null &&
                                            h.Type == HistoryEvent.Types.CoreAction &&
                                            h.CoreAction.Type == CoreActionBase.Types.KeyPress &&
                                            h.CoreAction.KeyCode == keyCode &&
                                            h.CoreAction.KeyDown == keyDown &&
                                            h.Ticks == ticks &&
                                            h.Id == id);
        }

        static public HistoryEvent LoadDiscEvent(int id, UInt64 ticks, byte drive, byte[] disc)
        {
            return It.Is<HistoryEvent>(h => h != null &&
                                            h.Type == HistoryEvent.Types.CoreAction &&
                                            h.CoreAction.Type == CoreActionBase.Types.LoadDisc &&
                                            h.CoreAction.Drive == drive &&
                                            h.CoreAction.MediaBuffer.GetBytes().SequenceEqual(disc) &&
                                            h.Ticks == ticks &&
                                            h.Id == id);
        }

        static public HistoryEvent LoadTapeEvent(int id, UInt64 ticks, byte[] tape)
        {
            return It.Is<HistoryEvent>(h => h != null &&
                                            h.Type == HistoryEvent.Types.CoreAction &&
                                            h.CoreAction.Type == CoreActionBase.Types.LoadTape &&
                                            h.CoreAction.MediaBuffer.GetBytes().SequenceEqual(tape) &&
                                            h.Ticks == ticks &&
                                            h.Id == id);
        }

        static public Bookmark BookmarkMatch(bool system, int statePos, int screenPos)
        {
            return It.Is<Bookmark>(
                b => b != null &&
                b.System == system &&
                ((MachineFile.MachineFileBlob)(b.State))._pos == statePos &&
                ((MachineFile.MachineFileBlob)(b.Screen))._pos == screenPos);
        }

        static public HistoryEvent CheckpointEvent(int id)
        {
            return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.Checkpoint && h.Id == id);
        }

        static public HistoryEvent CheckpointWithBookmarkEvent(int id, UInt64 ticks, bool system, int stateBlobPos)
        {
            return It.Is<HistoryEvent>(h => h != null &&
                                            h.Type == HistoryEvent.Types.Checkpoint &&
                                            h.Id == id &&
                                            h.Ticks == ticks &&
                                            h.Bookmark != null &&
                                            h.Bookmark.System == system &&
                                            ((MachineFile.MachineFileBlob) (h.Bookmark.State))._pos == stateBlobPos &&
                                            ((MemoryBlob)(h.Bookmark.Screen)).GetBytes() == null);
        }

        static public HistoryEvent CheckpointWithoutBookmarkEvent(int id, UInt64 ticks)
        {
            return It.Is<HistoryEvent>(h => h != null && h.Type == HistoryEvent.Types.Checkpoint && h.Id == id && h.Ticks == ticks && h.Bookmark == null);
        }

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
        /// <param name="stopOnAudioOverrun">Indicates if the machine should stop in the event of an audio overrun.</param>
        /// <returns>The total number of ticks that the machine ran for. Note this may be slightly larger than <c>ticks</c>, since Z80 instructions take at least 4 ticks.</returns>
        static public UInt64 Run(Machine machine, UInt64 ticks, bool stopOnAudioOverrun)
        {
            UInt64 beforeTicks = machine.Core.Ticks;
            machine.Core.RunUntil(beforeTicks + ticks, stopOnAudioOverrun ? StopReasons.AudioOverrun : StopReasons.None);

            return machine.Core.Ticks - beforeTicks;
        }

        /// <summary>
        /// Runs a machine for at least the specified number of ticks.
        /// </summary>
        /// <param name="machine">The machine to run.</param>
        /// <param name="ticks">The minimum number of ticks to run the machine for.</param>
        static public void RunForAWhile(Machine machine, UInt64 ticks = 1)
        {
            UInt64 startTicks = machine.Core.Ticks;
            UInt64 endTicks = startTicks + ticks;

            int timeWaited = 0;
            int sleepTime = 10;
            machine.Start();
            while (machine.Core.Ticks < endTicks)
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

        static public string GetTempFilepath(string filename)
        {
            return String.Format("{0}\\{1}", System.IO.Path.GetTempPath(), filename);
        }
    }
}
