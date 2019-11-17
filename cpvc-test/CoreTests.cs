using Moq;
using NUnit.Framework;
using System;
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
    }
}
