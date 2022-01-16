using NUnit.Framework;
using System;
using System.Collections.Generic;

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

        [TestCase(false)]
        [TestCase(true)]
        public void CloneLoadDisc(bool eject)
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };
            CoreAction action = CoreAction.LoadDisc(100, 1, eject ? null : new MemoryBlob(bytes));

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadDisc, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(1, clone.Drive);

            if (eject)
            {
                Assert.IsNull(clone.MediaBuffer);
            }
            else
            {
                Assert.AreNotSame(action.MediaBuffer, clone.MediaBuffer);
                Assert.AreEqual(bytes, clone.MediaBuffer.GetBytes());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CloneLoadTape(bool eject)
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };
            CoreAction action = CoreAction.LoadTape(100, eject ? null : new MemoryBlob(bytes));

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadTape, clone.Type);
            Assert.AreEqual(100, clone.Ticks);

            if (eject)
            {
                Assert.IsNull(clone.MediaBuffer);
            }
            else
            {
                Assert.AreNotSame(action.MediaBuffer, clone.MediaBuffer);
                Assert.AreEqual(bytes, clone.MediaBuffer.GetBytes());
            }
        }

        [Test]
        public void CloneRunUntil()
        {
            // Setup
            CoreAction action = CoreAction.RunUntil(100, 4000000, null);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.RunUntil, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(4000000, clone.StopTicks);
            Assert.AreEqual(null, clone.AudioSamples);
        }

        [Test]
        public void CloneRunUntilWithAudio()
        {
            // Setup
            List<UInt16> audioSamples = new List<UInt16> { 0x01, 0x02 };
            CoreAction action = CoreAction.RunUntil(100, 4000000, audioSamples);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.RunUntil, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(4000000, clone.StopTicks);
            Assert.AreEqual(new List<UInt16> { 0x01, 0x02 }, clone.AudioSamples);
        }

        [Test]
        public void CloneRevertToSnapshot()
        {
            // Setup
            CoreAction action = CoreAction.RevertToSnapshot(100, 42);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.RevertToSnapshot, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(42, clone.SnapshotId);
        }

        [Test]
        public void CloneCreateSnapshot()
        {
            // Setup
            CoreAction action = CoreAction.CreateSnapshot(100, 42);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.CreateSnapshot, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(42, clone.SnapshotId);
        }

        [Test]
        public void CloneLoadCore()
        {
            // Setup
            byte[] state = new byte[1000];
            for (int i = 0; i < state.Length; i++)
            {
                state[i] = (byte)(i % 0xff);
            }

            CoreAction action = CoreAction.LoadCore(100, new MemoryBlob(state));

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadCore, clone.Type);
            Assert.AreEqual(100, clone.Ticks);
            Assert.AreEqual(state, clone.CoreState.GetBytes());
        }

        [Test]
        public void CloneInvalidType()
        {
            // Setup
            CoreAction action = new CoreAction((CoreRequest.Types)999, 100);

            // Act
            CoreAction clone = action.Clone();

            // Verify
            Assert.IsNull(clone);
        }
    }
}
