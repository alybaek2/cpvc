using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace CPvC.Test
{
    public class FileSystemTests
    {
        [Test]
        public void OpenBinaryFile()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("opentest.txt");
            System.IO.File.Delete(filepath);
            FileSystem fs = new FileSystem();
            IBinaryFile file = fs.OpenBinaryFile(filepath);

            // Act
            file.WriteByte(0xfe);
            file.Close();

            // Verify
            byte[] contents = System.IO.File.ReadAllBytes(filepath);
            Assert.AreEqual(new byte[] { 0xfe }, contents);
        }

        [Test]
        public void RenameFile()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("renametest.txt");
            string filepath2 = TestHelpers.GetTempFilepath("renametest2.txt");
            System.IO.File.Delete(filepath);
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllText(filepath, "abc");
            System.IO.File.Delete(filepath2);

            // Act
            fs.RenameFile(filepath, filepath2);

            // Verify
            Assert.IsFalse(System.IO.File.Exists(filepath));
            Assert.IsTrue(System.IO.File.Exists(filepath2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ReplaceFile(bool exists)
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("replacetest.txt");
            string filepath2 = TestHelpers.GetTempFilepath("replacetest2.txt");
            FileSystem fs = new FileSystem();
            System.IO.File.Delete(filepath2);
            System.IO.File.WriteAllText(filepath2, "new");
            System.IO.File.Delete(filepath);
            if (exists)
            {
                System.IO.File.WriteAllText(filepath, "old");
            }

            // Act
            fs.ReplaceFile(filepath, filepath2);

            // Verify
            Assert.IsTrue(System.IO.File.Exists(filepath));
            Assert.IsFalse(System.IO.File.Exists(filepath2));
            Assert.AreEqual("new", System.IO.File.ReadAllText(filepath));
        }

        [Test]
        public void DeleteFile()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("deletetest.txt");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllText(filepath, "abc");

            // Act
            fs.DeleteFile(filepath);

            // Verify
            Assert.IsFalse(System.IO.File.Exists(filepath));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Exists(bool exists)
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("deletetest.txt");
            FileSystem fs = new FileSystem();
            if (exists)
            {
                System.IO.File.WriteAllText(filepath, "abc");
            }

            // Act and Verify
            Assert.AreEqual(exists, fs.Exists(filepath));
        }

        [Test]
        public void ReadLines()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("linestest.txt");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllLines(filepath, new string[] { "abc" });

            // Act
            IEnumerable<string> lines = fs.ReadLines(filepath);

            // Verify
            CollectionAssert.AreEqual(new string[] { "abc" }, lines);
            System.IO.File.Delete(filepath);
        }

        [Test]
        public void ReadBytes()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("bytestest.txt");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllBytes(filepath, new byte[] { 0x01, 0x02 });

            // Act
            byte[] bytes = fs.ReadBytes(filepath);
            System.IO.File.Delete("test.zip");

            // Verify
            Assert.AreEqual(new byte[] { 0x01, 0x02 }, bytes);
        }

        [Test]
        public void GetZipFileEntryNames()
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.zip");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllBytes(filepath, Resources.test);

            // Act
            List<string> entryNames = fs.GetZipFileEntryNames(filepath);
            System.IO.File.Delete(filepath);

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
            string filepath = TestHelpers.GetTempFilepath("test.zip");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllBytes(filepath, Resources.test);

            // Act
            byte[] contents = fs.GetZipFileEntry(filepath, entryName);
            System.IO.File.Delete(filepath);

            // Verify
            byte[] expectedBytes = (expectedContents != null) ? Encoding.ASCII.GetBytes(expectedContents) : null;
            Assert.AreEqual(expectedBytes, contents);
        }

        [TestCase("")]
        [TestCase("abc123")]
        public void FileLength(string contents)
        {
            // Setup
            string filepath = TestHelpers.GetTempFilepath("test.txt");
            FileSystem fs = new FileSystem();
            System.IO.File.WriteAllText(filepath, contents);

            // Act
            Int64 length = fs.FileLength(filepath);
            System.IO.File.Delete(filepath);

            // Verify
            Assert.AreEqual(contents.Length, length);
        }
    }
}
