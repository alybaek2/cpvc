using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    internal class MemoryFileByteStream : MemoryByteStream, IFileByteStream
    {
        public void Close()
        {
        }

        public void Dispose()
        {
        }
    }

    public class MachineFileTests
    {
        static readonly object[] CoreActionCases =
        {
            CoreAction.KeyPress(100, 42, true),
            CoreAction.CoreVersion(100, 2),
            CoreAction.LoadDisc(100, 1, new MemoryBlob(new byte[] { 0x01, 0x02 })),
            CoreAction.LoadTape(100, new MemoryBlob(new byte[] { 0x01, 0x02 })),
            CoreAction.Reset(100)
        };

        [TestCaseSource(nameof(CoreActionCases))]
        public void WriteAndReadCoreAction(CoreAction coreAction)
        {
            // Setup
            MemoryFileByteStream memStream = new MemoryFileByteStream();
            MachineFile file = new MachineFile(memStream);
            MachineHistory writeHistory = new MachineHistory();
            file.SetMachineHistory(writeHistory);

            // Act
            writeHistory.AddCoreAction(coreAction);
            file = new MachineFile(memStream);
            MachineHistory readHistory = new MachineHistory();
            file.SetMachineHistory(readHistory);
            file.ReadFile();

            // Verify
            Assert.True(HistoriesEqual(readHistory, writeHistory));
        }

        [Test]
        public void WriteAndReadBookmark()
        {
            // Setup
            MemoryFileByteStream memStream = new MemoryFileByteStream();
            MachineFile file = new MachineFile(memStream);
            MachineHistory writeHistory = new MachineHistory();
            file.SetMachineHistory(writeHistory);
            Bookmark bookmark = new Bookmark(false, 1, new byte[] { 0x01, 0x02 }, new byte[] { 0x03, 0x04 });

            // Act
            writeHistory.AddBookmark(100, bookmark);
            file = new MachineFile(memStream);
            MachineHistory readHistory = new MachineHistory();
            file.SetMachineHistory(readHistory);
            file.ReadFile();

            // Verify
            Assert.True(HistoriesEqual(readHistory, writeHistory));
        }

        [Test]
        public void WriteAndReadName()
        {
            // Setup
            MemoryFileByteStream memStream = new MemoryFileByteStream();
            MachineFile file = new MachineFile(memStream);
            Machine machine = new Machine(String.Empty, String.Empty, null);
            file.SetMachine(machine);

            // Act
            machine.Name = "Test";
            file = new MachineFile(memStream);
            Machine newMachine = new Machine(String.Empty, String.Empty, null);
            file.SetMachine(newMachine);
            file.ReadFile();

            // Verify
            Assert.AreEqual(machine.Name, newMachine.Name);
        }
    }
}
