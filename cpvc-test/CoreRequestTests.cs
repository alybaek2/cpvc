using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            CoreRequest request = CoreRequest.LoadDisc(1, eject ? null : bytes);

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadDisc, request.Type);
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
            CoreRequest request = CoreRequest.LoadTape(eject ? null : bytes);

            // Verify
            Assert.AreEqual(CoreRequest.Types.LoadTape, request.Type);

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
