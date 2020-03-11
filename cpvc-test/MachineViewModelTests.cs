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

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public void Pause(bool nullMachine, bool requiresOpen, bool running)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            mockMachine.SetupGet(x => x.Running).Returns(running);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.PauseCommand.Execute(null);

            // Verify
            mockPausableMachine.Verify(m => m.Stop(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && !requiresOpen && running, model.PauseCommand.CanExecute(null));
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(false, false, true)]
        [TestCase(true, false, true)]
        [TestCase(false, true, true)]
        [TestCase(true, true, true)]
        public void Resume(bool nullMachine, bool requiresOpen, bool running)
        {
            // Setup
            Mock<ICoreMachine> mockMachine = new Mock<ICoreMachine>();
            Mock<IOpenableMachine> mockOpenableMachine = mockMachine.As<IOpenableMachine>();
            Mock<IPausableMachine> mockPausableMachine = mockMachine.As<IPausableMachine>();
            mockOpenableMachine.SetupGet(x => x.RequiresOpen).Returns(requiresOpen);
            mockMachine.SetupGet(x => x.Running).Returns(running);
            MachineViewModel model = new MachineViewModel(nullMachine ? null : mockMachine.Object, null, null, null, null, null);

            // Act
            model.ResumeCommand.Execute(null);

            // Verify
            mockPausableMachine.Verify(m => m.Start(), nullMachine ? Times.Never() : Times.Once());
            Assert.AreEqual(!nullMachine && !requiresOpen && !running, model.ResumeCommand.CanExecute(null));
        }
    }
}
