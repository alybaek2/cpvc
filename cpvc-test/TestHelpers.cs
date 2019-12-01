using Moq;
using System;
using System.Collections.Generic;
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
            return It.IsAny<Times>();
        }

        static public string AnyString()
        {
            return It.IsAny<string>();
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
        /// Returns a mock IFile which writes to a list of strings.
        /// </summary>
        /// <param name="lines">A list of strings the mock will write too.</param>
        /// <returns>A mock IFile which writes to the given list of strings.</returns>
        static public IFile MockFileWriter(List<string> lines)
        {
            Mock<IFile> mockWriter = new Mock<IFile>(MockBehavior.Strict);
            mockWriter.Setup(s => s.WriteLine(AnyString())).Callback<string>(line => lines.Add(line));
            mockWriter.Setup(s => s.Close());

            return mockWriter.Object;
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
        /// Runs a machine for at least 1 tick.
        /// </summary>
        /// <param name="machine">The machine to run.</param>
        static public void RunForAWhile(Machine machine)
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

        static public string GetTempFilepath(string filename)
        {
            return String.Format("{0}\\{1}", System.IO.Path.GetTempPath(), filename);
        }
    }
}
