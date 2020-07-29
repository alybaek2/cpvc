using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class FileByteStreamTests
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

            FileSystem fs = new FileSystem();
            using (IFileByteStream file = fs.OpenFileByteStream(filepath))
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
        public void WriteByteArrayAndClose()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.txt");
            System.IO.File.Delete(filepath);

            FileSystem fileSystem = new FileSystem();
            IFileByteStream file = fileSystem.OpenFileByteStream(filepath);

            // Act
            file.Write(new byte[] { 0x01, 0x02, 0x03 });
            file.Close();

            // Verify
            CheckAndDelete(filepath);
        }

        /// <summary>
        /// Ensures that a file can be written to and read from using both array
        /// and single byte versions of the Read and Write methods.
        /// </summary>
        [Test]
        public void WriteAndReadBytes()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.txt");
            System.IO.File.Delete(filepath);

            FileSystem fileSystem = new FileSystem();
            IFileByteStream file = fileSystem.OpenFileByteStream(filepath);

            // Act
            file.Write(0x01);
            file.Write(0x02);
            file.Write(0x03);
            file.Write(new byte[] { 0x04, 0x05, 0x06 });

            // Verify
            file.Position = 0;
            Assert.AreEqual(6, file.Length);
            Assert.AreEqual(0x01, file.ReadByte());
            Assert.AreEqual(0x02, file.ReadByte());
            Assert.AreEqual(0x03, file.ReadByte());
            byte[] bytes = new byte[3];
            file.ReadBytes(bytes, 3);
            Assert.AreEqual(new byte[] { 0x04, 0x05, 0x06 }, bytes);

            // Verify reading past the end of the file.
            Assert.Throws<Exception>(() => file.ReadByte());
            Assert.AreEqual(0, file.ReadBytes(bytes, 1));

            Assert.AreEqual(6, file.Position);

            file.Close();
        }
    }
}
