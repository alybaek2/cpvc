using NUnit.Framework;

namespace CPvC.Test
{
    public class CoreRequestTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void LoadDisc(bool eject)
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };

            // Act
            MachineRequest request = MachineRequest.LoadDisc(1, eject ? null : bytes);

            // Verify
            Assert.AreEqual(MachineRequest.Types.LoadDisc, request.Type);
            Assert.AreEqual(1, request.Drive);

            if (eject)
            {
                Assert.IsNull(request.MediaBuffer.GetBytes());
            }
            else
            {
                Assert.AreEqual(bytes, request.MediaBuffer.GetBytes());
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LoadTape(bool eject)
        {
            // Setup
            byte[] bytes = new byte[] { 0x01, 0x02 };

            // Act
            MachineRequest request = MachineRequest.LoadTape(eject ? null : bytes);

            // Verify
            Assert.AreEqual(MachineRequest.Types.LoadTape, request.Type);

            if (eject)
            {
                Assert.IsNull(request.MediaBuffer.GetBytes());
            }
            else
            {
                Assert.AreEqual(bytes, request.MediaBuffer.GetBytes());
            }
        }
    }
}
