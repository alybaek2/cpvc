using System;
using System.Collections;
using System.Collections.Generic;
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

        public bool Pop(out UInt16 sample)
        {
            if (_writePosition <= _readPosition)
            {
                sample = 0;
                return false;
            }

            sample = _buffer[_readPosition % _buffer.Length];
            _readPosition++;

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

        public void Push(UInt16 sample)
        {
            _buffer[_writePosition % _buffer.Length] = sample;
            _writePosition++;
        }

        public bool Overrun()
        {
            return (_writePosition - _readPosition) > 2000;
        }

        public void Reverse()
        {
            Stack<UInt16> temp = new Stack<ushort>();

            UInt16 sample = 0;
            while (Pop(out sample))
            {
                temp.Push(sample);
            }

            while (temp.Count > 0)
            {
                Push(temp.Pop());
            }
        }
    }
}
