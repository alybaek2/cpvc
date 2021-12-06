using NUnit.Framework;
using System.IO;

namespace CPvC.Test
{
    public class TextFileTests
    {
        private MemoryStream _memoryStream;
        private TextFile _textFile;

        [SetUp]
        public void Setup()
        {
            _memoryStream = new MemoryStream();
            _textFile = new TextFile(_memoryStream);
        }

        [Test]
        public void WriteAndReadLine()
        {
            // Setup
            string testStr1 = "abcde";
            string testStr2 = "12345";

            // Act
            _textFile.WriteLine(testStr1);
            _textFile.WriteLine(testStr2);
            _memoryStream.Position = 0;
            string line1 = _textFile.ReadLine();
            string line2 = _textFile.ReadLine();

            // Verify
            Assert.AreEqual(testStr1, line1);
            Assert.AreEqual(testStr2, line2);
        }

        [Test]
        public void Close()
        {
            // Act
            _textFile.Close();

            // Verify
            Assert.False(_memoryStream.CanRead);
            Assert.False(_memoryStream.CanWrite);
        }

        [Test]
        public void Dispose()
        {
            // Act
            _textFile.Dispose();

            // Verify
            Assert.False(_memoryStream.CanRead);
            Assert.False(_memoryStream.CanWrite);
        }

        [Test]
        public void DisposeTwice()
        {
            // Setup
            _textFile.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => _textFile.Dispose());
            Assert.False(_memoryStream.CanRead);
            Assert.False(_memoryStream.CanWrite);
        }
    }
}
