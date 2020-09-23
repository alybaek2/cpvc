using System;
using System.Threading;

namespace CPvC
{
    public class AudioBuffer
    {
        private UInt16[] _buffer;
        private int _writePosition;
        private int _readPosition;
        private AutoResetEvent _underrunEvent;

        public AudioBuffer()
        {
            _buffer = new UInt16[48000];
            _writePosition = 0;
            _readPosition = 0;
            _underrunEvent = new AutoResetEvent(true);
        }

        public bool ReadFront(out UInt16 sample)
        {
            return Read(out sample, false);
        }

        public bool ReadBack(out UInt16 sample)
        {
            return Read(out sample, true);
        }

        private bool Read(out UInt16 sample, bool back)
        {
            if (_writePosition <= _readPosition)
            {
                sample = 0;
                return false;
            }

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
        }

        public bool Overrun()
        {
            return (_writePosition - _readPosition) > 2000;
        }
    }
}
