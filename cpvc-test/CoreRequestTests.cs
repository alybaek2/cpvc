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
            LoadDiscRequest request = new LoadDiscRequest(1, MemoryBlob.Create(eject ? null : bytes));

            // Verify
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
            LoadTapeRequest request = new LoadTapeRequest(MemoryBlob.Create(eject ? null : bytes));

            // Verify
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
