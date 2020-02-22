using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class CoreActionTests
    {
        [Test]
        public void CloneCoreVersion()
        {
            // Setup
            CoreAction action = CoreAction.CoreVersion(100, 1);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.CoreVersion, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(1, clone.Version);
        }

        [Test]
        public void CloneReset()
        {
            // Setup
            CoreAction action = CoreAction.Reset(100);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.Reset, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
        }

        [Test]
        public void CloneKeyPress()
        {
            // Setup
            CoreAction action = CoreAction.KeyPress(100, 78, true);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.KeyPress, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(78, clone.KeyCode);
            Assert.AreEqual(true, clone.KeyDown);
        }

        [Test]
        public void CloneLoadDisc()
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };
            CoreAction action = CoreAction.LoadDisc(100, 1, new MemoryBlob(bytes));

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadDisc, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(1, clone.Drive);
            Assert.AreNotSame(action.MediaBuffer, clone.MediaBuffer);
            Assert.AreEqual(bytes, clone.MediaBuffer.GetBytes());
        }

        [Test]
        public void CloneLoadTape()
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };
            CoreAction action = CoreAction.LoadTape(100, new MemoryBlob(bytes));

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadTape, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreNotSame(action.MediaBuffer, clone.MediaBuffer);
            Assert.AreEqual(bytes, clone.MediaBuffer.GetBytes());
        }

        [Test]
        public void CloneRunUntil()
        {
            // Setup
            CoreAction action = CoreAction.RunUntilForce(100, 4000000);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.RunUntilForce, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(4000000, clone.StopTicks);
        }

        [Test]
        public void CloneQuit()
        {
            // Setup
            CoreAction action = new CoreAction(CoreRequest.Types.Quit, 100);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.Quit, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
        }
    }
}
