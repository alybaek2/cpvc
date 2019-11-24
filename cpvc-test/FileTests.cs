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
        }

        [Test]
        public void WriteLinesAndDispose()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.txt");
            System.IO.File.Delete(filepath);
            using (File file = new File(filepath))
            {
                // Act
                file.WriteLine("abc");
                file.WriteLine("123");
            }

            // Verify
            CheckAndDelete(filepath);
        }

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
