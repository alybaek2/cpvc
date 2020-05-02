using Moq;
using NUnit.Framework;
using System;

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
            byte b = bs.ReadOneByte();
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
    }
}
