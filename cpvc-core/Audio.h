#pragma once
#include "common.h"

// Encapsulates the audio data output by the PSG.
class Audio
{
public:
    Audio();
    ~Audio();

    // Writes a single sample to each of the three audio channels.
    void WriteSample(byte (&amplitudes)[3]);

    // Indicates if the audio buffer is full.
    bool Overrun();

    // Retreieves data from the audio buffer and advances the read position.
    int GetBuffers(int numSamples, byte* (&pChannels)[3]);

private:
    constexpr static word _audioBufferSize = 4800 * 2;

    // The read and write positions of the buffer are always increasing integers; when reading or writing, these positions
    // are wrapped to the size of the buffer by using the modulo operator.
    qword _writePosition = 0;
    qword _readPosition = 0;
    byte _channel[3][_audioBufferSize];
};
