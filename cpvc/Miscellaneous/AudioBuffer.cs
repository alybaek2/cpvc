using System;
using System.Collections.Generic;
using System.Threading;

namespace CPvC
{
    public class AudioBuffer
    {
        // AY volume table (c) by Hacker KAY (http://kay27.narod.ru/ay.html)
        private static readonly ushort[] _amplitudes = {
            0, 836, 1212, 1773, 2619, 3875, 5397, 8823,
            10392, 16706, 23339, 29292, 36969, 46421, 55195, 65535
        };

        private readonly List<UInt16> _buffer;
        private int _writePosition;
        private int _readPosition;
        private readonly ManualResetEvent _underrunEvent;

        private readonly int _maxSize;

        public int OverrunThreshold { get; set; }
        public byte ReadSpeed { get; set; }

        public AudioBuffer(int maxSize)
        {
            _buffer = new List<UInt16>();

            _writePosition = 0;
            _readPosition = 0;

            _underrunEvent = new ManualResetEvent(true);

            _maxSize = maxSize;

            OverrunThreshold = 2000;
            ReadSpeed = 1;
        }

        /// <summary>
        /// Converts three-channel CPC audio samples to 16-bit signed stereo samples.
        /// </summary>
        /// <param name="volume">Volume at which audio should be rendered. 0 is muted, 255 is </param>
        /// <param name="buffer">Buffer to write audio samples to.</param>
        /// <param name="offset">Offset to begin writing at.</param>
        /// <param name="samplesRequested">The maximum number of samples to write.</param>
        /// <param name="samples">The buffer containing the CPC audio samples.</param>
        /// <param name="reverse">Indicates if the CPC audio samples should be read in reverse.</param>
        /// <returns>The number of samples written to <c>buffer</c>.</returns>
        public int Render16BitStereo(byte volume, byte[] buffer, int offset, int samplesRequested, bool reverse)
        {
            // Each sample requires four bytes, so take the size of the buffer to be the largest multiple
            // of 4 less than or equal to the length of the buffer.
            int bufferSize = 4 * (buffer.Length / 4);

            // Skew the volume factor so the volume control presents a more balanced range of volumes.
            double volumeFactor = Math.Pow(volume / 255.0, 3);

            int samplesWritten = 0;

            while (samplesWritten < samplesRequested && (offset + 3) < bufferSize)
            {
                if (!Read(reverse, out UInt16 sample))
                {
                    break;
                }

                // Samples are encoded as a 16-bit integer, with 4 bits per channel.
                byte channelA = (byte)(sample & 0x000f);
                byte channelB = (byte)((sample & 0x00f0) >> 4);
                byte channelC = (byte)((sample & 0x0f00) >> 8);

                // Treat Channel A as "Left", Channel B as "Centre", and Channel C as "Right".
                UInt32 left = (UInt32)(((2 * _amplitudes[channelA]) + _amplitudes[channelB]) * volumeFactor) / 3;
                UInt32 right = (UInt32)(((2 * _amplitudes[channelC]) + _amplitudes[channelB]) * volumeFactor) / 3;

                // Divide by two to deal with the fact NAudio requires signed 16-bit samples.
                left = (UInt16)(left / 2);
                right = (UInt16)(right / 2);

                buffer[offset] = (byte)(left & 0xFF);
                buffer[offset + 1] = (byte)(left >> 8);
                buffer[offset + 2] = (byte)(right & 0xFF);
                buffer[offset + 3] = (byte)(right >> 8);

                offset += 4;
                samplesWritten++;
            }

            return samplesWritten;
        }

        public void Advance(int samples)
        {
            lock (_buffer)
            {
                int maxSamples = _writePosition - _readPosition;
                if (samples > maxSamples)
                {
                    samples = maxSamples;
                }

                _readPosition += samples;

                if (!Overrun())
                {
                    _underrunEvent.Set();
                }
            }
        }

        private int BufferPos(int position)
        {
            if (_maxSize == -1)
            {
                return position;
            }
            else
            {
                return position % _maxSize;
            }
        }

        private bool Read(bool back, out UInt16 sample)
        {
            lock (_buffer)
            {
                if ((_writePosition - _readPosition) < ReadSpeed)
                {
                    sample = 0;
                    return false;
                }

                if (back)
                {
                    _writePosition -= ReadSpeed;
                    sample = _buffer[BufferPos(_writePosition)];
                }
                else
                {
                    sample = _buffer[BufferPos(_readPosition)];
                    _readPosition += ReadSpeed;
                }

                if (!Overrun())
                {
                    _underrunEvent.Set();
                }

                return true;
            }
        }

        private bool Overrun()
        {
            return (_writePosition - _readPosition) >= (OverrunThreshold * ReadSpeed);
        }

        public bool WaitForUnderrun(int timeout)
        {
            return _underrunEvent.WaitOne(timeout);
        }

        public void Write(UInt16 sample)
        {
            lock (_buffer)
            {
                if ((_maxSize == -1) || (_writePosition < _maxSize))
                {
                    _buffer.Add(sample);
                }
                else
                {
                    _buffer[BufferPos(_writePosition)] = sample;
                }

                _writePosition++;

                if (Overrun())
                {
                    _underrunEvent.Reset();
                }
            }
        }

        public void Write(IEnumerable<UInt16> samples)
        {
            foreach (UInt16 sample in samples)
            {
                Write(sample);
            }
        }
    }
}
