using Moq;
using NUnit.Framework;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class CoreTests
    {
        //private Mock<CoreEventHandler> _mockEventHanlder;

        [SetUp]
        public void Setup()
        {
            //_mockEventHanlder = new Mock<CoreEventHandler>();
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

        //[Test]
        //public void ProcessesKeyTwice()
        //{
        //    // Setup
        //    Mock<CoreEventHandler> mockEventHanlder = new Mock<CoreEventHandler>();

        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnCoreAction += mockEventHanlder.Object;
        //        core.Start();

        //        // Act
        //        core.KeyPress(Keys.Space, true);
        //        core.KeyPress(Keys.Space, true);
        //        WaitForQueueToProcess(core);

        //        // Verify
        //        mockEventHanlder.Verify(x => x(core, It.Is<CoreEventArgs>(args => IsKeyRequest(args.Request, Keys.Space, true) && IsKeyRequest(args.Action, Keys.Space, true))), Times.Once);
        //        mockEventHanlder.Verify(x => x(core, It.Is<CoreEventArgs>(args => IsKeyRequest(args.Request, Keys.Space, true) && args.Action == null)), Times.Once);
        //    }
        //}

        //[Test]
        //public void ProcessesReset()
        //{
        //    // Setup
        //    Mock<CoreEventHandler> mockEventHanlder = new Mock<CoreEventHandler>();
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnCoreAction += mockEventHanlder.Object;
        //        core.Start();

        //        // Act
        //        core.Reset();
        //        WaitForQueueToProcess(core);

        //        // Verify
        //        mockEventHanlder.Verify(x => x(core, It.Is<CoreEventArgs>(args => IsResetRequest(args.Request) && IsResetRequest(args.Action))), Times.Once);
        //    }
        //}

        //[Test]
        //public void OnBeginVSyncEventRaised()
        //{
        //    // Setup
        //    Mock<EventHandler> mockBeginVSync = new Mock<EventHandler>();
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnIdle += (sender, args) =>
        //        {
        //            args.Handled = true;
        //            args.Request = CoreRequest.RunUntil(core.Ticks + 1000);
        //        };
        //        core.OnBeginVSync += mockBeginVSync.Object;

        //        // Act - run for at least as long as two VSync's (one VSync would be about 4000000 / 50, or 80000 ticks).
        //        core.Start();
        //        while (core.Ticks < 2 * 80000)
        //        {
        //            // Empty out the audio buffer just in case on an overrun.
        //            core.AdvancePlayback(10000);
        //        }

        //        core.Stop();

        //        // Verify
        //        mockBeginVSync.Verify(beginVSync => beginVSync(core, null), Times.AtLeastOnce);
        //        mockBeginVSync.VerifyNoOtherCalls();
        //    }
        //}

        //[Test]
        //public void ProcessesRequestsInCorrectOrder()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnCoreAction += _mockEventHanlder.Object;

        //        MockSequence sequence = new MockSequence();
        //        _mockEventHanlder.InSequence(sequence).Setup(x => x(core, It.Is<CoreEventArgs>(args => args.Action.Type == CoreRequest.Types.KeyPress && args.Request.Type == CoreRequest.Types.KeyPress))).Verifiable();
        //        _mockEventHanlder.InSequence(sequence).Setup(x => x(core, It.Is<CoreEventArgs>(args => args.Action.Type == CoreRequest.Types.LoadTape && args.Request.Type == CoreRequest.Types.LoadTape))).Verifiable();
        //        _mockEventHanlder.InSequence(sequence).Setup(x => x(core, It.Is<CoreEventArgs>(args => args.Action.Type == CoreRequest.Types.LoadDisc && args.Request.Type == CoreRequest.Types.LoadDisc))).Verifiable();
        //        _mockEventHanlder.InSequence(sequence).Setup(x => x(core, It.Is<CoreEventArgs>(args => args.Action.Type == CoreRequest.Types.Reset && args.Request.Type == CoreRequest.Types.Reset))).Verifiable();

        //        // Act
        //        core.Start();
        //        core.KeyPress(Keys.Space, true);
        //        core.LoadTape(null);
        //        core.LoadDisc(0, null);
        //        core.Reset();

        //        // Verify
        //        _mockEventHanlder.VerifyAll();
        //    }
        //}

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

        //[Test]
        //public void RunForVSync()
        //{
        //    // Setup
        //    Core core = new Core(Core.LatestVersion, Core.Type.CPC6128);

        //    // Act
        //    core.RunForVSync(2);

        //    // Verify - running for 2 VSyncs means we should have completed at
        //    //          least 1 full frames, each of which should last approximately
        //    //          80000 (4000000 ticks / 50 frames) ticks each.
        //    Assert.Greater(core.Ticks, 1 * 80000);
        //}

        //[Test]
        //public void RunForVSyncWithHandler()
        //{
        //    // Setup
        //    Mock<EventHandler> mockBeginVSync = new Mock<EventHandler>();
        //    Core core = new Core(Core.LatestVersion, Core.Type.CPC6128);
        //    core.OnBeginVSync += mockBeginVSync.Object;
        //    core.Start();

        //    // Act
        //    ProcessOneRequest(core, CoreRequest.RunUntil(100000));

        //    // Verify
        //    Assert.GreaterOrEqual(core.Ticks, 100000);
        //    mockBeginVSync.Verify(v => v(core, null), Times.Once);
        //}

        //[Test]
        //public void CoreVersion()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.Start();

        //        core.SetScreen();
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.RunUntil(core.Ticks + 1));

        //        // Act
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.CoreVersion(1));
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.RunUntil(core.Ticks + 1));

        //        // Verify - To do!
        //    }
        //}

        [Test]
        public void CreateInvalidVersion()
        {
            // Setup and Verify
            Assert.Throws<ArgumentException>(() => new Core(2, Core.Type.CPC6128));
        }

        //[Test]
        //public void ProcessInvalidRequest()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        CoreRequest request = new CoreRequest((CoreRequest.Types)999);
        //        Mock<CoreEventHandler> mockEventHanlder = new Mock<CoreEventHandler>();
        //        core.OnCoreAction += mockEventHanlder.Object;
        //        core.Start();

        //        // Act
        //        TestHelpers.ProcessOneRequest(core, request);

        //        // Verify
        //        mockEventHanlder.Verify(x => x(core, It.Is<CoreEventArgs>(args => args.Request == request && args.Action == null)), Times.Once);
        //    }
        //}

        //[Test]
        //public void StartTwice()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.Start();

        //        // Act
        //        core.Start();

        //        // Verify
        //        Assert.DoesNotThrow(() => ProcessOneRequest(core, CoreRequest.RunUntil(10)));
        //    }
        //}

        //[Test]
        //public void TicksNotifyLimit()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        Mock<PropertyChangedEventHandler> mockPropChanged = new Mock<PropertyChangedEventHandler>();
        //        core.PropertyChanged += mockPropChanged.Object;

        //        // Act
        //        core.PushRequest(CoreRequest.RunUntil(1000000));
        //        core.Start();
        //        WaitForQueueToProcess(core);
        //        core.Stop();

        //        // Verify
        //        mockPropChanged.Verify(p => p(core, It.Is<PropertyChangedEventArgs>(a => a != null && a.PropertyName == "Ticks")), Times.Exactly((int)(50 * core.Ticks / 4000000)));
        //    }
        //}

        //[Test]
        //public void RevertToInvalidSnapshot()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.Start();

        //        // Act
        //        CoreAction action = TestHelpers.ProcessOneRequest(core, CoreRequest.RevertToSnapshot(42), 2000);
        //        core.Stop();

        //        // Verify
        //        Assert.IsNull(action);
        //    }
        //}

        //[Test]
        //public void LoadCore()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.Start();

        //        TestHelpers.ProcessOneRequest(core, CoreRequest.KeyPress(Keys.A, true));
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.RunUntil(core.Ticks + 1));
        //        byte[] state = core.GetState();
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.RunUntil(core.Ticks + 1));

        //        // Act
        //        byte[] preloadedState = core.GetState();
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.LoadCore(new MemoryBlob(state)));

        //        // Verify
        //        byte[] loadedState = core.GetState();
        //        Assert.AreEqual(state, loadedState);
        //        Assert.AreNotEqual(preloadedState, loadedState);
        //    }
        //}

        /// <summary>
        /// Ensures that a core raises an OnIdle event when the request queue is empty.
        /// </summary>
        //[Test]
        //public void OnIdleHandler()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnIdle += (sender, args) =>
        //        {
        //            args.Handled = true;
        //            args.Request = CoreRequest.RunUntil(core.Ticks + 1000);
        //        };
        //        ManualResetEvent processed = new ManualResetEvent(false);
        //        core.OnCoreAction += (sender, args) =>
        //        {
        //            processed.Set();
        //        };
        //        core.Start();

        //        // Act
        //        processed.WaitOne(2000);

        //        // Verify
        //        Assert.Greater(core.Ticks, 0);
        //    }
        //}

        /// <summary>
        /// Ensures that a core doesn't process any requests if it has no OnIdle handler.
        /// </summary>
        //[Test]
        //public void NoIdleHandler()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnCoreAction += _mockEventHanlder.Object;

        //        // Act
        //        core.Start();
        //        System.Threading.Thread.Sleep(50);

        //        // Verify
        //        _mockEventHanlder.VerifyNoOtherCalls();
        //    }
        //}

        //[Test]
        //public void StopOnAudioOverrun()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnIdle += (sender, args) =>
        //        {
        //            args.Handled = true;
        //            args.Request = CoreRequest.RunUntil(core.Ticks + 1000);
        //        };

        //        // Act
        //        core.Start();
        //        bool overrun = RunUntilAudioOverrun(core, 1000);

        //        // Verify
        //        Assert.True(overrun);
        //    }
        //}

        //[Test]
        //public void ResumeAfterAudioOverrun()
        //{
        //    // Setup
        //    using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
        //    {
        //        core.OnIdle += (sender, args) =>
        //        {
        //            args.Handled = true;
        //            args.Request = CoreRequest.RunUntil(core.Ticks + 1000);
        //        };
        //        core.Start();
        //        if (!RunUntilAudioOverrun(core, 10000))
        //        {
        //            Assert.Fail("Failed to wait for audio overrun");
        //        }

        //        UInt64 ticks = core.Ticks;

        //        // Act - empty out the audio buffer and continue running
        //        core.AudioBuffer.Advance(12000);
        //        TestHelpers.ProcessOneRequest(core, CoreRequest.RunUntil(ticks + 4000000));

        //        // Verify
        //        Assert.Greater(core.Ticks, ticks);
        //    }
        //}

        [Test]
        public void CopyScreenNull()
        {
            // Setup
            using (Core core = new Core(Core.LatestVersion, Core.Type.CPC6128))
            {
                // Act and Verify
                Assert.DoesNotThrow(() => core.CopyScreen(IntPtr.Zero, 0));
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
                Assert.DoesNotThrow(() => core.CopyScreen(buffer, 100));
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
