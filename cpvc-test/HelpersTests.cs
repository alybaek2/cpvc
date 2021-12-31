using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

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

        [Test]
        public void GetEndpointLocalhost()
        {
            // Act
            IPEndPoint endpoint = Helpers.GetEndpoint("localhost", 6128);

            // Verify
            Assert.IsNotNull(endpoint);
            Assert.AreEqual(AddressFamily.InterNetwork, endpoint.AddressFamily);
            Assert.AreEqual(6128, endpoint.Port);
            Assert.AreEqual(new byte[] { 127, 0, 0, 1 }, endpoint.Address.GetAddressBytes());
        }

        [Test]
        public void GetEndpointLocalhostIPv6()
        {
            // Act
            IPEndPoint endpoint = Helpers.GetEndpoint("::1", 6128);

            // Verify
            Assert.IsNull(endpoint);
        }

        [Test]
        public void HexStringFromBytes()
        {
            // Setup
            byte[] bytes = new byte[] { 0x00, 0x09, 0x0a, 0x0f, 0x90, 0xa0, 0xf0 };

            // Act
            string hex = Helpers.StrFromBytes(bytes);

            // Verify
            Assert.AreEqual("00090A0F90A0F0", hex);
        }

        [Test]
        public void BytesFromHexString()
        {
            // Setup
            string hex = "00090A0F90A0F0";

            // Act
            byte[] bytes = Helpers.BytesFromStr(hex);

            // Verify
            Assert.AreEqual(new byte[] { 0x00, 0x09, 0x0a, 0x0f, 0x90, 0xa0, 0xf0 }, bytes);
        }

        [TestCase(16)]
        [TestCase(255)]
        public void HexCharFromInvalidByte(byte b)
        {
            // Act and Verify
            Assert.Throws<ArgumentException>(() => Helpers.HexCharFromByte(b));
        }

        [TestCase('/')]
        [TestCase(':')]
        [TestCase('@')]
        [TestCase('G')]
        public void HexByteFromInvalidChar(char c)
        {
            // Act and Verify
            Assert.Throws<ArgumentException>(() => Helpers.HexByteFromChar(c));
        }
    }
}
