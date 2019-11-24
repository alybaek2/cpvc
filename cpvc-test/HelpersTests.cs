using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CPvC.Test
{
    [TestFixture]
    public class HelpersTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(637084482932687810)]
        public void ShouldConvertFromNumberToDateTimeAndBack(long number)
        {
            // Setup
            string numberStr = number.ToString();

            // Act
            DateTime dateTime = Helpers.NumberToDateTime(numberStr);
            long number2 = Helpers.DateTimeToNumber(dateTime);

            // Verify
            Assert.AreEqual(number, number2);
        }

        [Test]
        public void EmptyStringShouldThrow()
        {
            // Act and Verify
            Assert.Throws<System.FormatException>(() => Helpers.NumberToDateTime(""));
        }

        [TestCase(null, "")]
        [TestCase(new byte[] { }, "")]
        [TestCase(new byte[] { 0x01 }, "01")]
        [TestCase(new byte[] { 0x01, 0xaf, 0x73, 0x5b }, "01AF735B")]
        public void ShouldConvertByteArray(byte[] byteArray, string expectedHexString)
        {
            // Act
            string hexString = Helpers.HexString(byteArray);

            // Verify
            Assert.AreEqual(hexString, expectedHexString);
        }

        [TestCase("6")]
        [TestCase("ab01f")]
        [TestCase("ab01fe340")]
        public void OddStringLengthShouldThrow(string hexString)
        {
            // Act and Verify
            Assert.Throws<System.ArgumentException>(() => Helpers.Bytes(hexString));
        }

        [TestCase("", null)]
        [TestCase("01", new byte[] { 0x01 })]
        [TestCase("01AF735B", new byte[] { 0x01, 0xaf, 0x73, 0x5b })]
        public void ShouldConvertHexString(string hexString, byte[] expectedByteArray)
        {
            // Act
            byte[] byteArray = Helpers.Bytes(hexString);

            // Verify
            Assert.AreEqual(byteArray, expectedByteArray);
        }

        [TestCase(new byte[] { })]
        [TestCase(new byte[] { 0x01 })]
        [TestCase(new byte[] { 0x01, 0xaf, 0x73, 0x5b })]
        public void ShouldCompressAndDecompress(byte[] byteArray)
        {
            // Act
            byte[] compressed = Helpers.Compress(byteArray);
            byte[] decompressed = Helpers.Uncompress(compressed);

            // Verify
            Assert.AreEqual(byteArray, decompressed);
        }

        [TestCase("", new string[] { "" })]
        [TestCase("x", new string[] { "x" })]
        [TestCase("x,y", new string[] { "x", "y" })]
        [TestCase("abc,,de", new string[] { "abc", "", "de" })]
        [TestCase("abc,de,", new string[] { "abc", "de", "" })]
        [TestCase(",abc,de", new string[] { "", "abc", "de" })]
        [TestCase(",\\,\\,,\\,", new string[] { "", ",,", "," })]
        [TestCase(",\\\\,,", new string[] { "", "\\", "", "" })]
        public void ShouldSplitAndJoinWithEscape(string str, string[] expectedTokens)
        {
            // Act
            List<string> tokens = Helpers.SplitWithEscape(',', str);
            string str2 = Helpers.JoinWithEscape(',', tokens);

            // Verify
            Assert.AreEqual(tokens.ToArray(), expectedTokens);
            Assert.AreEqual(str, str2);
        }
    }
}
