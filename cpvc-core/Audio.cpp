#include "Audio.h"

Audio::Audio()
{
    for (int i = 0; i < 3; i++)
    {
        memset(_channel[i], 0, _audioBufferSize);
    }
}

Audio::~Audio()
{
}

void Audio::WriteSample(byte(&amplitudes)[3])
{
    qword wrappedPosition = _writePosition % _audioBufferSize;

    _channel[0][wrappedPosition] = amplitudes[0];
    _channel[1][wrappedPosition] = amplitudes[1];
    _channel[2][wrappedPosition] = amplitudes[2];

    _writePosition++;
}

bool Audio::Overrun()
{
    return ((_readPosition < _writePosition) && ((_writePosition - _readPosition) >= _audioBufferSize));
}

int Audio::GetBuffers(int numSamples, byte* (&pChannels)[3])
{
    int samples = 0;

    while (samples < numSamples && _readPosition < _writePosition)
    {
        int p = _readPosition % _audioBufferSize;

        for (byte c = 0; c < 3; c++)
        {
            if (pChannels[c] != nullptr)
            {
                pChannels[c][samples] = _channel[c][p];
            }
        }

        _readPosition++;

        samples++;
    }

    return samples;
}

