using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class CoreTests
    {
        private Mock<RequestProcessedDelegate> _mockRequestProcessed;

        [SetUp]
        public void Setup()
        {
            _mockRequestProcessed = new Mock<RequestProcessedDelegate>();
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
                core.Start();

                // Act
                core.KeyPress(Keys.Space, true);
                core.KeyPress(Keys.Space, true);
                WaitForQueueToProcess(core);

                // Verify
                mockRequestProcessed.Verify(x => x(core, KeyRequest(Keys.Space, true), KeyAction(Keys.Space, true)), Times.Once);
                mockRequestProcessed.Verify(x => x(core, KeyRequest(Keys.Space, true), null), Times.Once);
            }
        }

        [Test]
        public void VSyncDelegateCalled()
        {
            // Setup
            Mock<BeginVSyncDelegate> mockVSync = new Mock<BeginVSyncDelegate>();
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.IdleRequest = () => CoreRequest.RunUntil(core.Ticks + 1000);
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

        [Test]
        public void ProcessesActionsInCorrectOrder()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Auditors += _mockRequestProcessed.Object;

                MockSequence sequence = new MockSequence();
                _mockRequestProcessed.InSequence(sequence).Setup(x => x(core, KeyRequest(Keys.Space, true), KeyAction(Keys.Space, true))).Verifiable();
                _mockRequestProcessed.InSequence(sequence).Setup(x => x(core, TapeRequest(), TapeAction())).Verifiable();
                _mockRequestProcessed.InSequence(sequence).Setup(x => x(core, DiscRequest(), DiscAction())).Verifiable();
                _mockRequestProcessed.InSequence(sequence).Setup(x => x(core, ResetRequest(), ResetAction())).Verifiable();

                // Act
                core.Start();
                core.KeyPress(Keys.Space, true);
                core.LoadTape(null);
                core.LoadDisc(0, null);
                ProcessRequest(core, CoreRequest.Reset());

                // Verify
                _mockRequestProcessed.Verify();
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
            core.RunForVSync(2);

            // Verify - running for 2 VSyncs means we should have completed at
            //          least 1 full frames, each of which should last approximately
            //          80000 (4000000 ticks / 50 frames) ticks each.
            Assert.Greater(core.Ticks, 1 * 80000);
        }

        [Test]
        public void RunForVSyncWithHandler()
        {
            // Setup
            Mock<BeginVSyncDelegate> mock = new Mock<BeginVSyncDelegate>();
            Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128);
            core.BeginVSync += mock.Object;
            core.Start();

            // Act
            ProcessRequest(core, CoreRequest.RunUntil(100000));

            // Verify
            mock.Verify(v => v(core), Times.Once);
        }

        [Test]
        public void CoreVersion()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Start();

                core.SetScreen();
                TestHelpers.ProcessRequest(core, CoreRequest.RunUntil(core.Ticks + 1));

                // Act
                TestHelpers.ProcessRequest(core, CoreRequest.CoreVersion(1));
                TestHelpers.ProcessRequest(core, CoreRequest.RunUntil(core.Ticks + 1));

                // Verify - To do!
            }
        }

        [Test]
        public void CreateInvalidVersion()
        {
            // Setup and Verify
            Assert.Throws<Exception>(() => Core.Create(2, Core.Type.CPC6128));
        }

        [Test]
        public void ProcessInvalidRequest()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                CoreRequest request = new CoreRequest((CoreRequest.Types)999);
                Mock<RequestProcessedDelegate> mockAuditor = new Mock<RequestProcessedDelegate>();
                core.Auditors += mockAuditor.Object;
                core.Start();

                // Act
                TestHelpers.ProcessRequest(core, request);

                // Verify
                mockAuditor.Verify(a => a(core, request, null));
            }
        }

        [Test]
        public void StartTwice()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Start();

                // Act
                core.Start();

                // Verify
                Assert.DoesNotThrow(() => ProcessRequest(core, CoreRequest.RunUntil(10)));
            }
        }

        [Test]
        public void TicksNotifyLimit()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.IdleRequest = () => CoreRequest.RunUntil(core.Ticks + 1000);
                Mock<PropertyChangedEventHandler> mockPropChanged = new Mock<PropertyChangedEventHandler>();
                core.PropertyChanged += mockPropChanged.Object;

                // Act
                core.Start();
                while (core.Ticks < 10000000)
                {
                    core.AdvancePlayback(10000);
                }

                core.Stop();

                // Verify
                mockPropChanged.Verify(p => p(core, It.Is<PropertyChangedEventArgs>(a => a != null && a.PropertyName == "Ticks")), Times.Exactly((int)(50 * core.Ticks / 4000000)));
            }
        }

        [Test]
        public void LoadCore()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.Start();

                TestHelpers.ProcessRequest(core, CoreRequest.KeyPress(Keys.A, true));
                TestHelpers.ProcessRequest(core, CoreRequest.RunUntil(core.Ticks + 1));
                byte[] state = core.GetState();
                TestHelpers.ProcessRequest(core, CoreRequest.RunUntil(core.Ticks + 1));

                // Act
                byte[] preloadedState = core.GetState();
                TestHelpers.ProcessRequest(core, CoreRequest.LoadCore(new MemoryBlob(state)));

                // Verify
                byte[] loadedState = core.GetState();
                Assert.AreEqual(state, loadedState);
                Assert.AreNotEqual(preloadedState, loadedState);
            }
        }

        /// <summary>
        /// Ensures that a core calls its IdleRequest delegate when the request queue is empty.
        /// </summary>
        [Test]
        public void IdleRequest()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.IdleRequest = () => CoreRequest.RunUntil(core.Ticks + 1000);
                core.Start();

                // Act
                TestHelpers.WaitForNextRequestProcessed(core);

                // Verify
                Assert.Greater(core.Ticks, 0);
            }
        }

        /// <summary>
        /// Ensures that a core doesn't process any requests if it has no IdleRequest delegate.
        /// </summary>
        [Test]
        public void NullIdleRequest()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.IdleRequest = null;
                core.Start();

                // Act
                bool result = TestHelpers.WaitForNextRequestProcessed(core);

                // Verify
                Assert.False(result);
            }
        }

        [Test]
        public void StopOnAudioOverrun()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.IdleRequest = () => CoreRequest.RunUntil(core.Ticks + 1000);

                // Act
                core.Start();
                bool overrun = RunUntilAudioOverrun(core, 1000);

                // Verify
                Assert.True(overrun);
            }
        }

        [Test]
        public void ResumeAfterAudioOverrun()
        {
            // Setup
            using (Core core = Core.Create(Core.LatestVersion, Core.Type.CPC6128))
            {
                core.IdleRequest = () => CoreRequest.RunUntil(core.Ticks + 1000);
                core.Start();
                if (!RunUntilAudioOverrun(core, 1000))
                {
                    Assert.Fail("Failed to wait for audio overrun");
                }

                UInt64 ticks = core.Ticks;

                // Act - empty out the audio buffer and continue running
                core.AudioBuffer.Advance(12000);
                TestHelpers.ProcessRequest(core, CoreRequest.RunUntil(ticks + 4000000));

                // Verify
                Assert.Greater(core.Ticks, ticks);
            }
        }
    }
}
