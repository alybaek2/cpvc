using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class FileTests
    {
        private void CheckAndDelete(string filename)
        {
            // Since File will open a file exclusively, if the file can be successfully opened
            // now, it means File.Dispose was correctly invoked and closed the file.
            string[] lines = null;
            Assert.DoesNotThrow(() => lines = System.IO.File.ReadAllLines("test.txt"));
            Assert.AreEqual(2, lines.Length);
            Assert.AreEqual("abc", lines[0]);
            Assert.AreEqual("123", lines[1]);
        }

        [Test]
        public void WriteLinesAndDispose()
        {
            // Setup
            System.IO.File.Delete("test.txt");
            using (File file = new File("test.txt"))
            {
                // Act
                file.WriteLine("abc");
                file.WriteLine("123");
            }

            // Verify
            CheckAndDelete("test.txt");
        }

        [Test]
        public void WriteLinesAndClose()
        {
            // Setup
            System.IO.File.Delete("test.txt");
            File file = new File("test.txt");

            // Act
            file.WriteLine("abc");
            file.WriteLine("123");
            file.Close();

            // Verify
            CheckAndDelete("test.txt");
        }
    }
}
