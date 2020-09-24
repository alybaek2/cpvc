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
            _audioBuffer = new AudioBuffer();
        }

        // CPC sample    16-bit left   16-bit right
        // 0x0123        0x19, 0x03    0xe0, 0x01
        // 0x0456        0x8c, 0x09    0xee, 0x05
        // 0x0789        0x84, 0x1c    0x41, 0x12
        // 0x0abc        0x35, 0x43    0x75, 0x31
        // 0x0def        0x44, 0x79    0x60, 0x60

        [TestCase(0,   new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00,  0x00, 0x00, 0x00, 0x00 }, 3, false)]

        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00 }, 0, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00, 0x00 }, 0, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x00, 0x00, 0x00 }, 0, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x19, 0x03, 0xe0, 0x01 }, 1, false)]

        [TestCase(101, new UInt16[] { 0x0123 }, 0, new byte[] { 0x31, 0x00, 0x1d, 0x00 }, 1, false)]

        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x19, 0x03, 0xe0, 0x01,  0x84, 0x1c, 0x41, 0x12 }, 2, false)]

        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 0, new byte[] { 0x19, 0x03, 0xe0, 0x01,  0x84, 0x1c, 0x41, 0x12,  0x44, 0x79, 0x60, 0x60 }, 3, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 1, new byte[] { 0x00, 0x19, 0x03, 0xe0,  0x01, 0x84, 0x1c, 0x41,  0x12, 0x00, 0x00, 0x00 }, 2, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 2, new byte[] { 0x00, 0x00, 0x19, 0x03,  0xe0, 0x01, 0x84, 0x1c,  0x41, 0x12, 0x00, 0x00 }, 2, false)]
        [TestCase(255, new UInt16[] { 0x0123, 0x0789, 0x0def }, 3, new byte[] { 0x00, 0x00, 0x00, 0x19,  0x03, 0xe0, 0x01, 0x84,  0x1c, 0x41, 0x12, 0x00 }, 2, false)]
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

        [TestCase(0, false)]
        [TestCase(2000, false)]
        [TestCase(2001, true)]
        public void Overrun(int samples, bool expectedOverrun)
        {
            // Setup
            for (int i = 0; i < samples; i++)
            {
                _audioBuffer.Write(0);
            }

            // Verify
            Assert.AreEqual(expectedOverrun, _audioBuffer.Overrun());
            Assert.AreEqual(!expectedOverrun, _audioBuffer.WaitForUnderrun(0));
        }

        [TestCase(0, 2)]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        [TestCase(3, 0)]
        public void Advance(int advanceSamples, int expectedSamplesWritten)
        {
            // Setup
            _audioBuffer.Write(1);
            _audioBuffer.Write(2);

            // Act
            _audioBuffer.Advance(advanceSamples);

            // Verify
            byte[] buffer = new byte[8];
            int samplesWritten = _audioBuffer.Render16BitStereo(255, buffer, 0, 2, false);
            Assert.AreEqual(expectedSamplesWritten, samplesWritten);
        }
    }
}
