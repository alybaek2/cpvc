using Moq;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class CoreTests
    {
        [SetUp]
        public void Setup()
        {
        }

        static private Mock<IFileSystem> GetFileSystem(int size)
        {
            Mock<IFileSystem> mock = new Mock<IFileSystem>(MockBehavior.Strict);
            mock.Setup(fileSystem => fileSystem.ReadBytes(AnyString())).Returns(new byte[size]);

            return mock;
        }

        [Test]
        public void ThrowsWhenCreatingUnknownType()
        {
            // Setup
            Mock<IFileSystem> mock = GetFileSystem(0x4000);

            // Act and Verify
            Assert.Throws<ArgumentException>(() => new Core(Core.LatestVersion, (Core.Type)99));

            mock.VerifyNoOtherCalls();
        }

        [TestCase(false, 0x3FFF)]
        [TestCase(false, 0x4000)]
        [TestCase(false, 0x4001)]
        [TestCase(true, 0x3FFF)]
        [TestCase(true, 0x4000)]
        [TestCase(true, 0x4001)]
        public void LoadIncorrectSizeROM(bool lower, int size)
        {
            // Setup
            byte[] rom = new byte[size];
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                TestDelegate action = lower ?
                    (TestDelegate)(() => core.SetLowerROM(rom)) :
                    (TestDelegate)(() => core.SetUpperROM(0, rom));

                // Act and Verify
                if (size == 0x4000)
                {
                    Assert.DoesNotThrow(action);
                }
                else
                {
                    Assert.Throws<ArgumentException>(action);
                }
            }
        }

        [Test]
        public void DisposeTwice()
        {
            // Setup
            Core core = new Core(Core.LatestVersion, Core.Type.CPC6128);

            // Act
            core.Dispose();

            // Verify
            Assert.DoesNotThrow(() => core.Dispose());
        }

        [Test]
        public void LoadState()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                byte[] state = core.GetState();
                core.RunUntil(1000, StopReasons.None, null); // Need a better way to make the current state of the core different.

                // Act
                core.LoadState(state);

                // Verify
                byte[] afterState = core.GetState();
                Assert.AreEqual(state, afterState);
            }
        }

        [Test]
        public void DeleteSnapshot()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.CreateSnapshotSync(42);
                byte[] state = core.GetState();
                core.RunUntil(1000, StopReasons.None, null); // Need a better way to make the current state of the core different.
                byte[] afterState1 = core.GetState();

                // Act
                bool deleteResult = core.DeleteSnapshotSync(42);
                bool revertResult = core.RevertToSnapshotSync(42);

                // Verify
                byte[] afterState2 = core.GetState();
                Assert.AreEqual(afterState1, afterState2);
                Assert.True(deleteResult);
                Assert.False(revertResult);
            }
        }

        [Test]
        public void CoreVersion()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                // Act
                core.ProcessCoreVersion(Core.LatestVersion);

                // Verify - Need better verification. Can't really do much since there's only one version of the core available.
                Assert.AreEqual(Core.LatestVersion, core.Version);
            }
        }

        [Test]
        public void CreateInvalidVersion()
        {
            // Setup and Verify
            Assert.Throws<ArgumentException>(() => new Core(2, Core.Type.CPC6128));
        }

        [Test]
        public void CopyScreenNull()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                // Act and Verify
                Assert.DoesNotThrow(() => core.GetScreen(IntPtr.Zero, 0));
            }
        }

        [Test]
        public void CopyScreenDisposed()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Dispose();
                IntPtr buffer = Marshal.AllocHGlobal(100);

                // Act and Verify
                Assert.DoesNotThrow(() => core.GetScreen(buffer, 100));
                Marshal.FreeHGlobal(buffer);
            }
        }

        [Test]
        public void TicksDisposed()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Dispose();

                // Act and Verify
                Assert.DoesNotThrow(() => { UInt64 ticks = core.Ticks; });
            }
        }
    }
}
