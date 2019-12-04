using NUnit.Framework;

namespace CPvC.Test
{
    public class FileTests
    {
        private void CheckAndDelete(string filepath)
        {
            // Since File will open a file exclusively, if the file can be successfully opened
            // now, it means File.Dispose was correctly invoked and closed the file.
            string[] lines = null;
            Assert.DoesNotThrow(() => lines = System.IO.File.ReadAllLines(filepath));
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual("abc", lines[0]);
            Assert.AreEqual("123", lines[1]);

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
            using (File file = new File(filepath))
            {
                // Act
                file.WriteLine("abc");
                file.WriteLine("123");

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
            File file = new File(filepath);

            // Act
            file.WriteLine("abc");
            file.WriteLine("123");
            file.Close();

            // Verify
            CheckAndDelete(filepath);
        }
    }
}
