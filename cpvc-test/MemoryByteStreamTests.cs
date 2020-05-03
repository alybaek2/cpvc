using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Collections.Generic;

namespace CPvC.Test
{
    public class MemoryByteStreamTests
    {
        [Test]
        public void WriteByte()
        {
            // Setup
            byte expected = 0xcd;
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            byte b = bs.ReadByte();
            Assert.AreEqual(expected, b);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void WriteUInt16()
        {
            // Setup
            UInt16 expected = 0xcdef;
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            UInt16 u = bs.ReadUInt16();
            Assert.AreEqual(expected, u);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void WriteUInt32()
        {
            // Setup
            UInt32 expected = 0x12345678;
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            UInt32 u = bs.ReadUInt32();
            Assert.AreEqual(expected, u);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void WriteUInt64()
        {
            // Setup
            UInt64 expected = 0x0123456789ABCDEF;
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            UInt64 u = bs.ReadUInt64();
            Assert.AreEqual(expected, u);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void WriteString()
        {
            // Setup
            string expected = "TestString!";
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            string str = bs.ReadString();
            Assert.AreEqual(expected, str);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void Clear()
        {
            // Setup
            byte[] expected = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            MemoryByteStream bs = new MemoryByteStream();
            bs.WriteArray(expected);

            // Act
            bs.Position = 0;
            byte[] before = bs.ReadArray();
            bs.Clear();

            // Verify
            Assert.Greater(before.Length, 0);
            Assert.Zero(bs.Length);
        }

        [Test]
        public void WriteArray()
        {
            // Setup
            byte[] expected = new byte[] { 0x12, 0x34, 0x56, 0x78 };
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.WriteArray(expected);

            // Verify
            bs.Position = 0;
            byte[] bytes = bs.ReadArray();
            Assert.AreEqual(expected, bytes);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void WriteInt32()
        {
            // Setup
            Int32 expected = -123456789;
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            Int32 u = bs.ReadInt32();
            Assert.AreEqual(expected, u);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void WriteBool(bool expected)
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream();

            // Act
            bs.Write(expected);

            // Verify
            bs.Position = 0;
            bool b = bs.ReadBool();
            Assert.AreEqual(expected, b);
            Assert.AreEqual(bs.Length, bs.Position);
        }

        [Test]
        public void ReadByteTooShort()
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream();

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadByte());
        }

        [Test]
        public void ReadUInt16TooShort([Range(0, 1)] int size)
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream(new byte[size]);

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadUInt16());
        }

        [Test]
        public void ReadInt32TooShort([Range(0, 3)] int size)
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream(new byte[size]);

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadInt32());
        }

        [Test]
        public void ReadUInt32TooShort([Range(0, 3)] int size)
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream(new byte[size]);

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadUInt32());
        }

        [Test]
        public void ReadUInt64TooShort([Range(0, 7)] int size)
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream(new byte[size]);

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadUInt64());
        }

        [Test]
        public void ReadArrayTooShort()
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write(new byte[] { 0x01, 0x02, 0x03, 0x04 });
            byte[] bytes = bs.AsBytes();
            bytes = bytes.Take(bytes.Length - 1).ToArray();
            bs = new MemoryByteStream(bytes);

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadArray());
        }

        [Test]
        public void ReadStringTooShort()
        {
            // Setup
            MemoryByteStream bs = new MemoryByteStream();
            bs.Write("TestString!");
            byte[] bytes = bs.AsBytes();
            bytes = bytes.Take(bytes.Length - 1).ToArray();
            bs = new MemoryByteStream(bytes);

            // Act and Verify
            Assert.Throws<Exception>(() => bs.ReadString());
        }
    }
}
