using NUnit.Framework;

namespace CPvC.Test
{
    public class FileTests
    {
        private void CheckAndDelete(string filepath)
        {
            // Since File will open a file exclusively, if the file can be successfully opened
            // now, it means File.Dispose was correctly invoked and closed the file.
            byte[] contents = null;
            Assert.DoesNotThrow(() => contents = System.IO.File.ReadAllBytes(filepath));
            Assert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, contents);

            // Ensure that the Close/Dispose call properly closes and releases the file, by ensuring
            // that deleting the file completes successfully and doesn't throw an exception.
            Assert.DoesNotThrow(() => System.IO.File.Delete(filepath));
            Assert.IsFalse(System.IO.File.Exists(filepath));
        }

        /// <summary>
        /// Ensures that the Dispose method correctly closes and releases the file.
        /// </summary>
        /// <remarks>
        /// This test also ensures that calling both Dispose and Close still results in the expected behaviour.
        /// </remarks>
        /// <param name="close">Indicates if Close should also be called in addition to Dispose.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void WriteLinesAndDispose(bool close)
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.txt");
            System.IO.File.Delete(filepath);
            using (BinaryFile file = new BinaryFile(filepath))
            {
                // Act
                file.Write(new byte[] { 0x01, 0x02, 0x03 });

                if (close)
                {
                    file.Close();
                }
            }

            // Verify
            CheckAndDelete(filepath);
        }

        /// <summary>
        /// Ensures that the Close method correctly closes and releases the file.
        /// </summary>
        [Test]
        public void WriteLinesAndClose()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.txt");
            System.IO.File.Delete(filepath);
            BinaryFile file = new BinaryFile(filepath);

            // Act
            file.Write(new byte[] { 0x01, 0x02, 0x03 });
            file.Close();

            // Verify
            CheckAndDelete(filepath);
        }
    }
}
