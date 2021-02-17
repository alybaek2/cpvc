using System;
using System.Threading;

namespace CPvC
{
    public class CircularAudioBuffer : BaseAudioBuffer
    {
        private UInt16[] _buffer;
        private int _writePosition;
        private int _readPosition;
        private AutoResetEvent _underrunEvent;

        public CircularAudioBuffer()
        {
            _buffer = new UInt16[48000];

            _writePosition = 0;
            _readPosition = 0;
            _underrunEvent = new AutoResetEvent(true);
        }

        protected override bool ReadFront(out UInt16 sample)
        {
            return Read(out sample, false);
        }

        protected override bool ReadBack(out UInt16 sample)
        {
            return Read(out sample, true);
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
        }

        private bool Read(out UInt16 sample, bool back)
        {
            if (_writePosition <= _readPosition)
            {
                sample = 0;
                return false;
            }

            bool overrunBefore = Overrun();

            if (back)
            {
                _writePosition--;
                sample = _buffer[_writePosition % _buffer.Length];
            }
            else
            {
                sample = _buffer[_readPosition % _buffer.Length];
                _readPosition++;
            }

            if (overrunBefore && !Overrun())
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
            return (_writePosition - _readPosition) > 2000;
        }
    }
}
