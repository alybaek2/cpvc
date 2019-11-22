using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class FileSystemTests
    {
        [Test]
        public void OpenFile()
        {
            // Setup
            System.IO.File.Delete("opentest.txt");
            FileSystem fs = new FileSystem();
            IFile file = fs.OpenFile("opentest.txt");

            // Act
            file.WriteLine("abc");
            file.Close();

            // Verify
            string contents = System.IO.File.ReadAllText("opentest.txt");
            Assert.AreEqual("abc\r\n", contents);
        }

        [Test]
        public void RenameFile()
        {
            // Setup
            System.IO.File.Delete("renametest.txt");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllText("renametest.txt", "abc");
            System.IO.File.Delete("renametest2.txt");

            // Act
            fs.RenameFile("renametest.txt", "renametest2.txt");

            // Verify
            Assert.IsFalse(System.IO.File.Exists("renametest.txt"));
            Assert.IsTrue(System.IO.File.Exists("renametest2.txt"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReplaceFile(bool exists)
        {
            // Setup
            FileSystem fs = new FileSystem();
            System.IO.File.Delete("replacetest2.txt");
            System.IO.File.WriteAllText("replacetest2.txt", "new");
            System.IO.File.Delete("replacetest.txt");
            if (exists)
            {
                System.IO.File.WriteAllText("replacetest.txt", "old");
            }

            // Act
            fs.ReplaceFile("replacetest.txt", "replacetest2.txt");

            // Verify
            Assert.IsTrue(System.IO.File.Exists("replacetest.txt"));
            Assert.IsFalse(System.IO.File.Exists("replacetest2.txt"));
            Assert.AreEqual("new", System.IO.File.ReadAllText("replacetest.txt"));
        }

        [Test]
        public void DeleteFile()
        {
            // Setup
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllText("replacetest.txt", "abc");

            // Act
            fs.DeleteFile("replacetest.txt");

            // Verify
            Assert.IsFalse(System.IO.File.Exists("replacetest.txt"));
        }

        [Test]
        public void ReadLines()
        {
            // Setup
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllLines("linestest.txt", new string[] { "abc" });

            // Act
            string[] lines = fs.ReadLines("linestest.txt");

            // Verify
            Assert.AreEqual(new string[] { "abc" }, lines);
        }

        [Test]
        public void ReadBytes()
        {
            // Setup
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllBytes("bytestest.txt", new byte[] { 0x01, 0x02 });

            // Act
            byte[] bytes = fs.ReadBytes("bytestest.txt");
            System.IO.File.Delete("test.zip");

            // Verify
            Assert.AreEqual(new byte[] { 0x01, 0x02 }, bytes);
        }

        [Test]
        public void GetZipFileEntryNames()
        {
            // Setup
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllBytes("test.zip", Resources.test);

            // Act
            List<string> entryNames = fs.GetZipFileEntryNames("test.zip");
            System.IO.File.Delete("test.zip");

            // Verify
            Assert.AreEqual(2, entryNames.Count);
            Assert.IsTrue(entryNames.Contains("test1.txt"));
            Assert.IsTrue(entryNames.Contains("test2.txt"));
        }

        [TestCase("test1.txt", "abc\r\n")]
        [TestCase("TEST2.txt", "123\r\n")]
        [TestCase("test3.txt", null)]
        public void GetZipFileEntry(string entryName, string expectedContents)
        {
            // Setup
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllBytes("test.zip", Resources.test);

            // Act
            byte[] contents = fs.GetZipFileEntry("test.zip", entryName);
            System.IO.File.Delete("test.zip");

            // Verify
            byte[] expectedBytes = (expectedContents != null) ? Encoding.ASCII.GetBytes(expectedContents) : null;
            Assert.AreEqual(expectedBytes, contents);
        }
    }
}
