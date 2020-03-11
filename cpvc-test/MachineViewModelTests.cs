using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class MachineViewModelTests
    {
        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Open(bool nullMachine, bool requiresOpen)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
            mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.OpenCommand.Execute(null);

            // Verify
            mockOpenableMachine.Verify(m => m.Open(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && requiresOpen, model.OpenCommand.CanExecute(null));
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void Close(bool nullMachine, bool requiresOpen)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
            mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.CloseCommand.Execute(null);

            // Verify
            mockOpenableMachine.Verify(m => m.Close(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && !requiresOpen, model.CloseCommand.CanExecute(null));
        }

        [Test]
        public void Pause(
            [Values(false, true)] bool nullMachine,
            [Values(false, true)] bool requiresOpen,
            [Values(false, true)] bool running,
            [Values(false, true)] bool isOpenable)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            if (isOpenable)
            {
                Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
                mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            }
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            mockMachine.SetupGet(x => x.Running).Returns(running);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.PauseCommand.Execute(null);

            // Verify
            mockPausableMachine.Verify(m => m.Stop(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && (!isOpenable || !requiresOpen) && running, model.PauseCommand.CanExecute(null));
        }

        [Test]
        public void Resume(
            [Values(false, true)] bool nullMachine,
            [Values(false, true)] bool requiresOpen,
            [Values(false, true)] bool running,
            [Values(false, true)] bool isOpenable)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            if (isOpenable)
            {
                Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
                mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            }
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            mockMachine.SetupGet(x => x.Running).Returns(running);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.ResumeCommand.Execute(null);

            // Verify
            mockPausableMachine.Verify(m => m.Start(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && (!isOpenable || !requiresOpen) && !running, model.ResumeCommand.CanExecute(null));
        }

        [Test]
        public void Reset([Values(false, true)] bool nullMachine)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IInteractiveMachine> mockOpenableMachine = mockMachine.As<IInteractiveMachine>();
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.ResetCommand.Execute(null);

            // Verify
            mockOpenableMachine.Verify(m => m.Reset(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, model.ResetCommand.CanExecute(null));
        }

        [Test]
        public void ToggleRunning([Values(false, true)] bool nullMachine)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IPausableMachine> mockOpenableMachine = mockMachine.As<IPausableMachine>();
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.ToggleRunningCommand.Execute(null);

            // Verify
            mockOpenableMachine.Verify(m => m.ToggleRunning(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine, model.ToggleRunningCommand.CanExecute(null));
        }
    }
}
