using System;
using System.Threading;

namespace CPvC
{
    public class CircularAudioBuffer : BaseAudioBuffer
    {
        private UInt16[] _buffer;
        private int _writePosition;
        private int _readPosition;
        private ManualResetEvent _underrunEvent;

        public int OverrunThreshold { get; set; }
        public byte Step { get; set; }

        public CircularAudioBuffer()
        {
            _buffer = new UInt16[48000];

            _writePosition = 0;
            _readPosition = 0;
            _underrunEvent = new ManualResetEvent(true);
            OverrunThreshold = 2000;
            Step = 1;
        }

        protected override bool ReadFront(out UInt16 sample)
        {
            return Read(false, out sample);
        }

        protected override bool ReadBack(out UInt16 sample)
        {
            return Read(true, out sample);
        }

        public void Advance(int samples)
        {
            if ((_readPosition + samples) > _writePosition)
            {
                _readPosition = _writePosition;
            }
            else
            {
                _readPosition += samples;
            }

            if (!Overrun())
            {
                _underrunEvent.Set();
            }
        }

        private bool Read(bool back, out UInt16 sample)
        {
            if ((_writePosition - _readPosition) < Step)
            {
                sample = 0;
                return false;
            }

            if (back)
            {
                _writePosition -= Step;
                sample = _buffer[_writePosition % _buffer.Length];
            }
            else
            {
                sample = _buffer[_readPosition % _buffer.Length];
                _readPosition += Step;
            }

            if (!Overrun())
            {
                _underrunEvent.Set();
            }

            return true;
        }

        public bool WaitForUnderrun(int timeout)
        {
            return _underrunEvent.WaitOne(timeout);
        }

        public void Write(UInt16 sample)
        {
            _buffer[_writePosition % _buffer.Length] = sample;

            _writePosition++;

            if (Overrun())
            {
                _underrunEvent.Reset();
            }
        }

        public bool Overrun()
        {
            return (_writePosition - _readPosition) > (OverrunThreshold * Step);
        }
    }
}
