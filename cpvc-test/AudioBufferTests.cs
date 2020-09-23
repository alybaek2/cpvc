using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class AudioBufferTests
    {
        [Test]
        public void ReadFront()
        {
            // Setup
            AudioBuffer audioBuffer = new AudioBuffer();

            // Act
            audioBuffer.Write(0x0102);
            audioBuffer.Write(0x0304);

            // Verify
            UInt16 sample1;
            bool pop1 = audioBuffer.ReadFront(out sample1);
            UInt16 sample2;
            bool pop2 = audioBuffer.ReadFront(out sample2);
            UInt16 sample3;
            bool pop3 = audioBuffer.ReadFront(out sample3);

            Assert.IsTrue(pop1);
            Assert.AreEqual(0x0102, sample1);
            Assert.IsTrue(pop2);
            Assert.AreEqual(0x0304, sample2);
            Assert.IsFalse(pop3);
        }

        [Test]
        public void ReadBack()
        {
            // Setup
            AudioBuffer audioBuffer = new AudioBuffer();

            // Act
            audioBuffer.Write(0x0102);
            audioBuffer.Write(0x0304);

            // Verify
            UInt16 sample1;
            bool pop1 = audioBuffer.ReadBack(out sample1);
            UInt16 sample2;
            bool pop2 = audioBuffer.ReadBack(out sample2);
            UInt16 sample3;
            bool pop3 = audioBuffer.ReadBack(out sample3);

            Assert.IsTrue(pop1);
            Assert.AreEqual(0x0304, sample1);
            Assert.IsTrue(pop2);
            Assert.AreEqual(0x0102, sample2);
            Assert.IsFalse(pop3);
        }

        [Test]
        public void ReadBackEmpty()
        {
            // Setup
            AudioBuffer audioBuffer = new AudioBuffer();

            // Verify
            UInt16 sample1;
            bool pop = audioBuffer.ReadFront(out sample1);

            Assert.IsFalse(pop);
        }

        [Test]
        public void ReadFrontEmpty()
        {
            // Setup
            AudioBuffer audioBuffer = new AudioBuffer();

            // Verify
            UInt16 sample1;
            bool pop = audioBuffer.ReadFront(out sample1);

            Assert.IsFalse(pop);
        }
    }
}
