﻿using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class AudioBufferTests
    {
        private AudioBuffer _audioBuffer;

        [SetUp]
        public void Setup()
        {
            _audioBuffer = new AudioBuffer(48000);
        }

        // Using the following values for testing:

        // CPC sample    16-bit left   16-bit right
        // 0x0123        0x0319        0x01e0
        // 0x0456        0x098c        0x05ee
        // 0x0789        0x1c84        0x1241
        // 0x0abc        0x4335        0x3175
        // 0x0def        0x7944        0x6060

        // Test at zero volume.
        [TestCase(0,   new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00 }, 3, false)]

        // Test getting a single sample with various buffer sizes; sizes smaller than a single sample (i.e. 4 bytes) should not return anything.
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00 }, 0, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00, 0x00 }, 0, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00, 0x00, 0x00 }, 0, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x19, 0x03, 0xe0, 0x01 }, 1, false)]

        // Test at a lower volume.
        [TestCase(101, new UInt16[] { 0x0123 }, 0, new byte[] { 0x31, 0x00, 0x1d, 0x00 }, 1, false)]

        // Tests to read fewer samples than we have available in the buffer.
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x19, 0x03, 0xe0, 0x01,  0x84, 0x1c, 0x41, 0x12 }, 2, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x44, 0x79, 0x60, 0x60,  0x84, 0x1c, 0x41, 0x12 }, 2, true)]

        // Tests to ensure we don't write more samples into the buffer than we have space for (uses the offset parameter).
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x19, 0x03, 0xe0, 0x01,  0x84, 0x1c, 0x41, 0x12,  0x44, 0x79, 0x60, 0x60 }, 3, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 1, new byte[] { 0x00, 0x19, 0x03, 0xe0,  0x01, 0x84, 0x1c, 0x41,  0x12, 0x00, 0x00, 0x00 }, 2, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 2, new byte[] { 0x00, 0x00, 0x19, 0x03,  0xe0, 0x01, 0x84, 0x1c,  0x41, 0x12, 0x00, 0x00 }, 2, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 3, new byte[] { 0x00, 0x00, 0x00, 0x19,  0x03, 0xe0, 0x01, 0x84,  0x1c, 0x41, 0x12, 0x00 }, 2, false)]

        // As above, but reading the audio buffer in reverse.
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x44, 0x79, 0x60, 0x60,  0x84, 0x1c, 0x41, 0x12,  0x19, 0x03, 0xe0, 0x01 }, 3, true)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 1, new byte[] { 0x00, 0x44, 0x79, 0x60,  0x60, 0x84, 0x1c, 0x41,  0x12, 0x00, 0x00, 0x00 }, 2, true)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 2, new byte[] { 0x00, 0x00, 0x44, 0x79,  0x60, 0x60, 0x84, 0x1c,  0x41, 0x12, 0x00, 0x00 }, 2, true)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 3, new byte[] { 0x00, 0x00, 0x00, 0x44,  0x79, 0x60, 0x60, 0x84,  0x1c, 0x41, 0x12, 0x00 }, 2, true)]
        public void Render(byte volume, UInt16[] cpcSamples, int offset, byte[] expectedBuffer, int expectedSamplesWritten, bool reverse)
        {
            // Setup
            foreach (UInt16 cpcSample in cpcSamples)
            {
                _audioBuffer.Write(cpcSample);
            }

            // Act
            byte[] buffer = new byte[expectedBuffer.Length];
            int samplesWritten = _audioBuffer.Render16BitStereo(volume, buffer, offset, cpcSamples.Length, reverse);

            // Verify
            Assert.AreEqual(expectedSamplesWritten, samplesWritten);
            Assert.AreEqual(expectedBuffer, buffer);
        }

        /// <summary>
        /// Test to ensure that each call to Render16BitStereo retrieves successive samples from the buffer.
        /// </summary>
        [Test]
        public void SuccessiveReads()
        {
            // Setup
            UInt16[] cpcSamples = new UInt16[] { 0x0123, 0x0789, 0x0def };
            foreach (UInt16 cpcSample in cpcSamples)
            {
                _audioBuffer.Write(cpcSample);
            }

            // Act
            byte[] buffer1 = new byte[4];
            int samplesWritten1 = _audioBuffer.Render16BitStereo(255, buffer1, 0, 1, false);
            byte[] buffer2 = new byte[4];
            int samplesWritten2 = _audioBuffer.Render16BitStereo(255, buffer2, 0, 1, false);
            byte[] buffer3 = new byte[4];
            int samplesWritten3 = _audioBuffer.Render16BitStereo(255, buffer3, 0, 1, false);

            // Verify
            Assert.AreEqual(1, samplesWritten1);
            Assert.AreEqual(new byte[] { 0x19, 0x03, 0xe0, 0x01 }, buffer1);
            Assert.AreEqual(1, samplesWritten2);
            Assert.AreEqual(new byte[] { 0x84, 0x1c, 0x41, 0x12 }, buffer2);
            Assert.AreEqual(1, samplesWritten3);
            Assert.AreEqual(new byte[] { 0x44, 0x79, 0x60, 0x60 }, buffer3);
        }

        /// <summary>
        /// Test to ensure that Overrun is set to true after being false.
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="expectedOverrun"></param>
        [TestCase(0, false)]
        [TestCase(1999, false)]
        [TestCase(2000, true)]
        public void Overrun(int samples, bool expectedOverrun)
        {
            // Setup
            for (int i = 0; i < samples; i++)
            {
                _audioBuffer.Write(0);
            }

            // Verify
            Assert.AreEqual(!expectedOverrun, _audioBuffer.WaitForUnderrun(0));
        }

        /// <summary>
        /// Test to ensure that Overrun is set to false after being true.
        /// </summary>
        [TestCase(0, true)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        public void UnderrunAfterRender(int samples, bool expectedUnderrun)
        {
            // Setup
            _audioBuffer.OverrunThreshold = 2;
            for (int i = 0; i < samples; i++)
            {
                _audioBuffer.Write(0);
            }

            // Act
            byte[] buffer = new byte[4];
            _audioBuffer.Render16BitStereo(255, buffer, 0, 1, false);

            // Verify
            Assert.AreEqual(expectedUnderrun, _audioBuffer.WaitForUnderrun(0));
        }

        [TestCase(5, 2, 3, false)]
        [TestCase(5, 2, 4, true)]
        public void Advance(int samples, int overrunThreshold, int advanceSamples, bool expectedUnderrun)
        {
            // Setup
            _audioBuffer.OverrunThreshold = overrunThreshold;
            _audioBuffer.ReadSpeed = 1;

            for (int i = 0; i < samples; i++)
            {
                _audioBuffer.Write(1);
            }

            // Act
            _audioBuffer.Advance(advanceSamples);

            // Verify
            Assert.AreEqual(expectedUnderrun, _audioBuffer.WaitForUnderrun(0));
        }

        [Test]
        public void CircularWrite()
        {
            // Setup
            byte[] expectedBuffer = new byte[] {
                0x35, 0x43, 0x75, 0x31,
                0x8c, 0x09, 0xee, 0x05,
                0x84, 0x1c, 0x41, 0x12
            };
            AudioBuffer audioBuffer = new AudioBuffer(3);

            // Act
            audioBuffer.Write(0x0123);
            audioBuffer.Write(0x0456);
            audioBuffer.Write(0x0789);
            audioBuffer.Write(0x0abc);

            // Verify
            byte[] buffer = new byte[12];
            audioBuffer.Render16BitStereo(255, buffer, 0, 3, false);
            Assert.AreEqual(expectedBuffer, buffer);
        }

        [Test]
        public void AppendWrite()
        {
            // Setup
            byte[] expectedBuffer = new byte[] {
                0x35, 0x43, 0x75, 0x31,
                0x8c, 0x09, 0xee, 0x05,
                0x84, 0x1c, 0x41, 0x12
            };
            AudioBuffer audioBuffer = new AudioBuffer(-1);

            // Act
            for (ushort i = 0; i < 50000; i++)
            {
                audioBuffer.Write(i);
            }

            // Verify
            byte[] buffer = new byte[50000 * 4];
            int samplesWritten = audioBuffer.Render16BitStereo(255, buffer, 0, 50000, false);
            Assert.AreEqual(50000, samplesWritten);
        }
    }
}
