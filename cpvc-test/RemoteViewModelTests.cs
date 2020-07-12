using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class RemoteViewModelTests
    {
        private ServerInfo _serverInfo;
        private ReceiveAvailableMachinesDelegate _receiveAvailableMachines;
        private Mock<IRemote> _mockRemote;
        private RemoteViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            _serverInfo = new ServerInfo("localhost", 6128);
            _receiveAvailableMachines = null;
            _mockRemote = new Mock<IRemote>();
            _mockRemote.SetupSet(r => r.ReceiveAvailableMachines = It.IsAny<ReceiveAvailableMachinesDelegate>()).Callback<ReceiveAvailableMachinesDelegate>(callback => _receiveAvailableMachines = callback);
            _viewModel = new RemoteViewModel(_serverInfo, _mockRemote.Object);
            _receiveAvailableMachines(new List<string> { "Machine1", "Machine2" });
        }

        [Test]
        public void Create()
        {
            // Verify
            _mockRemote.Verify(r => r.SendRequestAvailableMachines(), Times.Once());
            Assert.AreEqual(new ObservableCollection<string> { "Machine1", "Machine2" }, _viewModel.MachineNames);
            Assert.AreEqual("localhost", _viewModel.Server.ServerName);
            Assert.AreEqual(6128, _viewModel.Server.Port);
            Assert.IsNotNull(_viewModel.Machine);
        }

        [Test]
        public void EnableLivePreview()
        {
            // Act
            _viewModel.LivePreviewEnabled = true;
            _viewModel.SelectedMachineName = "Machine1";

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine("Machine1"), Times.Once());
            Assert.True(_viewModel.LivePreviewEnabled);
            Assert.AreEqual("Machine1", _viewModel.SelectedMachineName);
        }

        [Test]
        public void EnableLivePreviewNoSelectedMachine()
        {
            // Act
            _viewModel.LivePreviewEnabled = true;

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine("Machine1"), Times.Never());
            Assert.True(_viewModel.LivePreviewEnabled);
            Assert.IsNull(_viewModel.SelectedMachineName);
        }

        [Test]
        public void EnableLivePreviewSelectTwoDifferentMachines()
        {
            // Act
            _viewModel.LivePreviewEnabled = true;
            _viewModel.SelectedMachineName = "Machine1";
            _viewModel.SelectedMachineName = "Machine2";

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine("Machine1"), Times.Once());
            _mockRemote.Verify(r => r.SendSelectMachine("Machine2"), Times.Once());
            Assert.True(_viewModel.LivePreviewEnabled);
            Assert.AreEqual("Machine2", _viewModel.SelectedMachineName);
        }

        [Test]
        public void DisableLivePreview()
        {
            // Act
            _viewModel.LivePreviewEnabled = true;
            _viewModel.SelectedMachineName = "Machine1";
            _viewModel.LivePreviewEnabled = false;

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine("Machine1"), Times.Once());
            _mockRemote.Verify(r => r.SendSelectMachine(""), Times.Once());
            Assert.False(_viewModel.LivePreviewEnabled);
            Assert.AreEqual("Machine1", _viewModel.SelectedMachineName);
        }

        [Test]
        public void EnableLivePreviewNullMachine()
        {
            // Act
            _viewModel.LivePreviewEnabled = true;
            _viewModel.SelectedMachineName = null;

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine(It.IsAny<string>()), Times.Never());
            Assert.True(_viewModel.LivePreviewEnabled);
            Assert.IsNull(_viewModel.SelectedMachineName);
        }

        [Test]
        public void EnableLivePreviewSelectSameMachineTwice()
        {
            // Act
            _viewModel.LivePreviewEnabled = true;
            _viewModel.SelectedMachineName = "Machine1";
            _viewModel.SelectedMachineName = "Machine1";

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine("Machine1"), Times.Once());
            Assert.True(_viewModel.LivePreviewEnabled);
            Assert.AreEqual("Machine1", _viewModel.SelectedMachineName);
        }

        [Test]
        public void EnableLivePreviewDisabled()
        {
            // Act
            _viewModel.LivePreviewEnabled = false;
            _viewModel.SelectedMachineName = "Machine1";
            _viewModel.SelectedMachineName = "Machine1";

            // Verify
            _mockRemote.Verify(r => r.SendSelectMachine("Machine1"), Times.Never());
            Assert.False(_viewModel.LivePreviewEnabled);
            Assert.AreEqual("Machine1", _viewModel.SelectedMachineName);
        }
    }
}
