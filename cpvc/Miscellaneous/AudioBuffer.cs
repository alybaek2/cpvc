using System;
using System.Collections.Generic;
using System.Linq;

namespace CPvC
{
    public class AudioBuffer : BaseAudioBuffer
    {
        private List<UInt16> _buffer;
        private int _writePosition;
        private int _readPosition;

        public AudioBuffer(int capacity)
        {
            _buffer = new List<UInt16>(capacity);

            _writePosition = 0;
            _readPosition = 0;
        }

        protected override bool ReadFront(out UInt16 sample)
        {
            return Read(out sample, false);
        }

        protected override bool ReadBack(out UInt16 sample)
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
                sample = _buffer[_writePosition];
            }
            else
            {
                sample = _buffer[_readPosition];
                _readPosition++;
            }

            return true;
        }

        public void Write(IEnumerable<UInt16> samples)
        {
            _buffer.AddRange(samples);

            _writePosition += samples.Count();
        }

    }
}
