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
            IMachineAction action = MachineAction.CoreVersion(100, 1);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is CoreVersionAction);
            CoreVersionAction coreVersionAction = (CoreVersionAction)clone;
            Assert.AreEqual(100, coreVersionAction.Ticks);
            Assert.AreEqual(1, coreVersionAction.Version);
        }

        [Test]
        public void CloneReset()
        {
            // Setup
            IMachineAction action = MachineAction.Reset(100);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is ResetAction);
            Assert.AreEqual(100, clone.Ticks);
        }

        [Test]
        public void CloneKeyPress()
        {
            // Setup
            IMachineAction action = MachineAction.KeyPress(100, 78, true);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is KeyPressAction);
            KeyPressAction keyPressAction = (KeyPressAction)clone;
            Assert.AreEqual(100, keyPressAction.Ticks);
            Assert.AreEqual(78, keyPressAction.KeyCode);
            Assert.AreEqual(true, keyPressAction.KeyDown);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CloneLoadDisc(bool eject)
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };
            LoadDiscAction action = MachineAction.LoadDisc(100, 1, eject ? null : MemoryBlob.Create(bytes));

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is LoadDiscAction);
            LoadDiscAction loadDiscActionClone = (LoadDiscAction)clone;
            Assert.AreEqual(100, loadDiscActionClone.Ticks);
            Assert.AreEqual(1, loadDiscActionClone.Drive);

            if (eject)
            {
                Assert.IsNull(loadDiscActionClone.MediaBuffer);
            }
            else
            {
                Assert.AreNotSame(action.MediaBuffer, loadDiscActionClone.MediaBuffer);
                Assert.AreEqual(bytes, loadDiscActionClone.MediaBuffer.GetBytes());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CloneLoadTape(bool eject)
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };
            LoadTapeAction action = MachineAction.LoadTape(100, eject ? null : MemoryBlob.Create(bytes));

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is LoadTapeAction);
            LoadTapeAction loadTapeActionClone = (LoadTapeAction)clone;
            Assert.AreEqual(100, loadTapeActionClone.Ticks);

            if (eject)
            {
                Assert.IsNull(loadTapeActionClone.MediaBuffer);
            }
            else
            {
                Assert.AreNotSame(action.MediaBuffer, loadTapeActionClone.MediaBuffer);
                Assert.AreEqual(bytes, loadTapeActionClone.MediaBuffer.GetBytes());
            }
        }

        [Test]
        public void CloneRunUntil()
        {
            // Setup
            IMachineAction action = MachineAction.RunUntil(100, 4000000, null);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is RunUntilAction);
            RunUntilAction runUntilAction = (RunUntilAction)clone;
            Assert.AreEqual(100, runUntilAction.Ticks);
            Assert.AreEqual(4000000, runUntilAction.StopTicks);
            Assert.AreEqual(null, runUntilAction.AudioSamples);
        }

        [Test]
        public void CloneRunUntilWithAudio()
        {
            // Setup
            List<UInt16> audioSamples = new List<UInt16> { 0x01, 0x02 };
            IMachineAction action = MachineAction.RunUntil(100, 4000000, audioSamples);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is RunUntilAction);
            RunUntilAction runUntilAction = (RunUntilAction)clone;
            Assert.AreEqual(100, runUntilAction.Ticks);
            Assert.AreEqual(4000000, runUntilAction.StopTicks);
            Assert.AreEqual(new List<UInt16> { 0x01, 0x02 }, runUntilAction.AudioSamples);
        }

        [Test]
        public void CloneRevertToSnapshot()
        {
            // Setup
            IMachineAction action = MachineAction.RevertToSnapshot(100, 42);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is RevertToSnapshotAction);
            RevertToSnapshotAction revertToSnapshotAction = (RevertToSnapshotAction)clone;
            Assert.AreEqual(100, revertToSnapshotAction.Ticks);
            Assert.AreEqual(42, revertToSnapshotAction.SnapshotId);
        }

        [Test]
        public void CloneCreateSnapshot()
        {
            // Setup
            IMachineAction action = MachineAction.CreateSnapshot(100, 42);

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is CreateSnapshotAction);
            CreateSnapshotAction createSnapshotAction = (CreateSnapshotAction)clone;
            Assert.AreEqual(100, createSnapshotAction.Ticks);
            Assert.AreEqual(42, createSnapshotAction.SnapshotId);
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

            IMachineAction action = MachineAction.LoadCore(100, MemoryBlob.Create(state));

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.True(clone is LoadCoreAction);
            LoadCoreAction loadCoreAction = (LoadCoreAction)clone;
            Assert.AreEqual(100, loadCoreAction.Ticks);
            Assert.AreEqual(state, loadCoreAction.State.GetBytes());
        }

        [Test]
        public void CloneInvalidType()
        {
            // Setup
            TestHelpers.UnknownAction action = new TestHelpers.UnknownAction();

            // Act
            IMachineAction clone = MachineAction.Clone(action);

            // Verify
            Assert.IsNull(clone);
        }
    }
}
