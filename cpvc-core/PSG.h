#pragma once

#include "common.h"
#include "Keyboard.h"
#include "IPSG.h"

class PSG : public IPSG
{
public:
    PSG(Keyboard& keyboard);
    ~PSG();

    void Reset();
    void Amplitudes(byte (&amp)[3]);
    void Tick();

    byte Read();
    void Write(byte b);
    void SetControl(bool bdir, bool bc1);
    bool Bc1();
    bool Bdir();

private:
    bool _bdir;
    bool _bc1;
    byte _selectedRegister;
    byte _register[16];

    word& _toneA = *((word*)&_register[0]);
    word& _toneB = *((word*)&_register[2]);
    word& _toneC = *((word*)&_register[4]);
    byte& _noisePeriod = _register[6];
    byte& _mixer = _register[7];
    byte& _amplitudeA = _register[8];
    byte& _amplitudeB = _register[9];
    byte& _amplitudeC = _register[10];
    word& _envelopePeriod = *((word*)&_register[11]);
    byte& _envelopeShape = _register[13];
    byte& _ioA = _register[14];
    byte& _ioB = _register[15];

    word _toneTicks[3];
    bool _state[3];

    word _noiseTicks;
    bool _noiseAmplitude;
    word _noiseRandom;

    word _envelopeTickCounter;
    byte _envelopeStepCount;
    word _envelopePeriodCount;
    byte _envelopeState;
    word _noiseTickCounter;
    byte _envelopeStepState;

    Keyboard& _keyboard;

    void TickChannelState(word& tone, word& ticksCounter, bool& state);

    bool ToneEnableA() { return ((_mixer & 0x01) == 0); };
    bool ToneEnableB() { return ((_mixer & 0x02) == 0); };
    bool ToneEnableC() { return ((_mixer & 0x04) == 0); };
    bool NoiseEnableA() { return ((_mixer & 0x08) == 0); };
    bool NoiseEnableB() { return ((_mixer & 0x10) == 0); };
    bool NoiseEnableC() { return ((_mixer & 0x20) == 0); };

    byte ChannelAmplitude(byte amp, bool toneEnable, bool noiseEnable, bool state);

    void WriteRegister(byte b);

    byte ReadRegister();

    friend StreamWriter& operator<<(StreamWriter& s, const PSG& psg);
    friend StreamReader& operator>>(StreamReader& s, PSG& psg);
};

