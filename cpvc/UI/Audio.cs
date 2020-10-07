using NAudio.Wave;
using System;

namespace CPvC
{
    /// <summary>
    /// Delegate for audio buffer playback.
    /// </summary>
    /// <param name="buffer">Byte array to write data into.</param>
    /// <param name="offset">Offset (in bytes) into byte array to start writing at.</param>
    /// <param name="sampleCount">Number of 16-bit stereo samples to be written.</param>
    /// <returns>The number of 16-bit stereo samples written to the buffer</returns>
    public delegate int ReadAudioDelegate(byte[] buffer, int offset, int sampleCount);

    /// <summary>
    /// A thin wrapper intended to hide the details of intiailizing NAudio.
    /// </summary>
    public class Audio : WaveStream
    {
        private const int _latency = 70;
        private readonly IWavePlayer _wavePlayer;

        private const long _size = 48000;
        private readonly WaveFormat _waveFormat = new WaveFormat(48000, 16, 2);
        private long _position;
        private readonly ReadAudioDelegate _readAudio;

        public Audio(ReadAudioDelegate readAudioDelegate)
        {
            _readAudio = readAudioDelegate;

            // Create audio device
            WaveOutEvent waveOut = new WaveOutEvent
            {
                DeviceNumber = -1,
                DesiredLatency = _latency
            };

            waveOut.Init(this);

            _wavePlayer = waveOut;
        }

        public void Start()
        {
            _wavePlayer.Play();
        }

        public void Stop()
        {
            _wavePlayer.Stop();
        }

        // WaveStream implementation
        public override WaveFormat WaveFormat
        {
            get
            {
                return _waveFormat;
            }
        }

        public override long Length
        {
            get
            {
                return _size;
            }
        }

        public override long Position
        {
            get
            {
                return _position;
            }

            set
            {
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int samplesRequested = count / 4;
            int samplesWritten = 0;

            try
            {
                samplesWritten = _readAudio(buffer, offset, samplesRequested);
            }
            catch (Exception ex)
            {
                Diagnostics.Trace("Exception thrown during audio callback: {0}.", ex.Message);
            }

            // If no samples were written, ensure to write at least one empty sample.
            // This is necessary as returning 0 from this method will cause audio
            // playback to stop.
            if (samplesWritten == 0 && count >= 4)
            {
                System.Array.Clear(buffer, offset, 4);
                samplesWritten++;
            }

            return samplesWritten * 4;
        }
    }
}
