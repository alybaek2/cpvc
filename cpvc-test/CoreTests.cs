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

        static private CoreRequest KeyRequest(byte keycode, bool down)
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.KeyPress && r.KeyCode == keycode && r.KeyDown == down);
        }

        static private CoreAction KeyAction(byte keycode, bool down)
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.KeyPress && r.KeyCode == keycode && r.KeyDown == down);
        }

        static private CoreRequest TapeRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.LoadTape);
        }

        static private CoreAction TapeAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.LoadTape);
        }

        static private CoreRequest ResetRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.Reset);
        }

        static private CoreAction ResetAction()
        {
            return It.Is<CoreAction>(r => r != null && r.Type == CoreActionBase.Types.Reset);
        }

        static private CoreRequest RunUntilRequest()
        {
            return It.Is<CoreRequest>(r => r != null && r.Type == CoreActionBase.Types.RunUntil);
        }

        static private CoreAction RunUntilAction()
        {
            return It.Is<CoreAction>(r => r == null || r.Type == CoreActionBase.Types.RunUntil);
        }

        [Test]
        public void ThrowsWhenCreatingUnknownType()
        {
            // Setup
            Mock<IFileSystem> mock = GetFileSystem(0x4000);

            // Act and Verify
            Assert.Throws<ArgumentException>(() => Core.Create((Core.Type)99));

            mock.VerifyNoOtherCalls();
        }

        [Test]
        public void ProcessesKeyTwice()
        {
            // Setup
            Mock<RequestProcessedDelegate> mockRequestProcessed = new Mock<RequestProcessedDelegate>();
            using (Core core = Core.Create(Core.Type.CPC6128))
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
                mockRequestProcessed.Verify(x => x(core, RunUntilRequest(), RunUntilAction()), TimesAny());
                mockRequestProcessed.VerifyNoOtherCalls();
            }
        }

        [Test]
        public void ProcessesActionsInCorrectOrder()
        {
            // Setup
            Mock<RequestProcessedDelegate> mockRequestProcessed = new Mock<RequestProcessedDelegate>();
            using (Core core = Core.Create(Core.Type.CPC6128))
            {
                core.Auditors += mockRequestProcessed.Object;

                MockSequence sequence = new MockSequence();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, KeyRequest(Keys.Space, true), KeyAction(Keys.Space, true))).Verifiable();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, TapeRequest(), TapeAction())).Verifiable();
                mockRequestProcessed.InSequence(sequence).Setup(x => x(core, ResetRequest(), ResetAction())).Verifiable();

                // Act
                core.KeyPress(Keys.Space, true);
                core.LoadTape(null);
                core.Reset();

                core.Start();
                core.WaitForRequestQueueEmpty();
                core.Stop();

                // Verify
                mockRequestProcessed.Verify();
                mockRequestProcessed.Verify(x => x(core, RunUntilRequest(), RunUntilAction()), TimesAny());
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
            using (Core core = Core.Create(Core.Type.CPC6128))
            {
                core.Volume = volume1;
                object sender = null;
                PropertyChangedEventArgs args = null;
                core.PropertyChanged += ((object changedSender, PropertyChangedEventArgs changedArgs) => { sender = changedSender; args = changedArgs; });

                // Act
                core.Volume = volume2;

                // Verify
                Assert.AreEqual(core.Volume, volume2);
                if (notified)
                {
                    Assert.AreEqual(sender, core);
                    Assert.IsNotNull(args);
                    Assert.AreEqual(args.PropertyName, "Volume");
                }
                else
                {
                    Assert.IsNull(sender);
                    Assert.IsNull(args);
                }
            }
        }

        /// <summary>
        /// Ensures the correct number of audio samples are generated after running the core.
        /// </summary>
        /// <param name="ticks">The number of ticks to run the core for.</param>
        /// <param name="expectedSamples">The number of audio samples that should be written.</param>
        [TestCase(4UL, 1)]
        [TestCase(250UL, 4)]
        [TestCase(504UL, 7)]
        public void GetAudio(UInt64 ticks, int expectedSamples)
        {
            // Setup
            using (Core core = Core.Create(Core.Type.CPC6128))
            {
                int bufferSize = 1000;
                byte[] channelA = new byte[bufferSize];
                byte[] channelB = new byte[bufferSize];
                byte[] channelC = new byte[bufferSize];

                // Act
                core.RunUntil(ticks, StopReasons.AudioOverrun);
                int samples = core.GetAudioBuffers(bufferSize, channelA, channelB, channelC);

                // Verify
                Assert.AreEqual(expectedSamples, samples);
            }
        }
    }
}
