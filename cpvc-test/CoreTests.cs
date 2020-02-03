using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class CoreTests
    {
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
            Assert.Throws<ArgumentException>(() => Core.Create(Core.LatestVersion, (Core.Type)99));

            mock.VerifyNoOtherCalls();
        }

        [Test]
        public void ProcessesKeyTwice()
        {
            // Setup
            Mock<RequestProcessedDelegate> mockRequestProcessed = new Mock<RequestProcessedDelegate>();
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Auditors += mockRequestProcessed.Object;

                // Act
                core.Start();
                core.KeyPress(Keys.Space, true);
                core.KeyPress(Keys.Space, true);
                core.WaitForRequestQueueEmpty();
                core.Stop();

                // Verify
                mockRequestProcessed.Verify(x => x(core, KeyRequest(Keys.Space, true), KeyAction(Keys.Space, true)), Times.Once);
                mockRequestProcessed.Verify(x => x(core, KeyRequest(Keys.Space, true), null), Times.Once);
                mockRequestProcessed.Verify(x => x(core, RunUntilRequest(), RunUntilAction()), AnyTimes());
                mockRequestProcessed.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void VSyncDelegateCalled()
        {
            // Setup
            Mock<BeginVSyncDelegate> mockVSync = new Mock<BeginVSyncDelegate>();
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.BeginVSync += mockVSync.Object;

                // Act - run for at least as long as two VSync's (one VSync would be about 4000000 / 50, or 80000 ticks).
                core.Start();
                while (core.Ticks < 2 * 80000)
                {
                    // Empty out the audio buffer just in case on an overrun.
                    core.AdvancePlayback(10000);
                }

                core.Stop();

                // Verify
                mockVSync.Verify(beginVSync => beginVSync(core), Times.AtLeastOnce);
                mockVSync.VerifyNoOtherCalls();
            }
        }

        /// <summary>
        /// Ensures a newly instantiated Core has no samples in the audio buffer.
        /// </summary>
        [Test]
        public void ReadNoAudioSamples()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                // Verify
                byte[] buffer = new byte[100];
                int samplesRead = core.ReadAudio16BitStereo(buffer, 0, buffer.Length);
                Assert.AreEqual(0, samplesRead);
            }
        }

        [Test]
        public void ProcessesActionsInCorrectOrder()
        {
            // Setup
            Mock<RequestProcessedDelegate> mockRequestProcessed = new Mock<RequestProcessedDelegate>();
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Auditors += mockRequestProcessed.Object;

                MockSequence sequence = new MockSequence();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, KeyRequest(Keys.Space, true), KeyAction(Keys.Space, true))).Verifiable();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, TapeRequest(), TapeAction())).Verifiable();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, DiscRequest(), DiscAction())).Verifiable();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, ResetRequest(), ResetAction())).Verifiable();

                // Act
                core.KeyPress(Keys.Space, true);
                core.LoadTape(null);
                core.LoadDisc(0, null);
                core.Reset();

                core.Start();
                core.WaitForRequestQueueEmpty();
                core.Stop();

                // Verify
                mockRequestProcessed.Verify();
                mockRequestProcessed.Verify(x => x(core, RunUntilRequest(), RunUntilAction()), AnyTimes());
                mockRequestProcessed.VerifyNoOtherCalls();
            }
        }

        [TestCase(0, 1, true)]
        [TestCase(100, 101, true)]
        [TestCase(255, 0, true)]
        [TestCase(255, 255, false)]
        public void SetVolume(byte volume1, byte volume2, bool notified)
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Volume = volume1;
                Mock<PropertyChangedEventHandler> propChanged = new Mock<PropertyChangedEventHandler>();
                core.PropertyChanged += propChanged.Object;

                // Act
                core.Volume = volume2;

                // Verify
                Assert.AreEqual(core.Volume, volume2);
                if (notified)
                {
                    propChanged.Verify(PropertyChanged(core, "Volume"), Times.Once);
                }

                propChanged.VerifyNoOtherCalls();
            }
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
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
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
            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);

            // Act
            core.Dispose();

            // Verify
            Assert.DoesNotThrow(() => core.Dispose());
        }

        [Test]
        public void RunForVSync()
        {
            // Setup
            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);

            // Act
            core.RunForVSync(10);

            // Verify - running for 10 VSyncs means we should have completed at
            //          least 9 full frames, each of which should last approximately
            //          80000 (4000000 ticks / 50 frames) ticks each.
            Assert.Greater(core.Ticks, 9 * 80000);
        }

        [Test]
        public void CreateInvalidVersion()
        {
            // Setup and Verify
            Assert.Throws<Exception>(() => Core.Create(2, Core.Type.CPC6128));
        }
    }
}
