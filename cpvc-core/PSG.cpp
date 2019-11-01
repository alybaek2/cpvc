#include "PSG.h"

// Envelope phases
constexpr byte envSustainLow = 0;
constexpr byte envSustainHigh = 1;
constexpr byte envAttack = 2;
constexpr byte envRelease = 3;

PSG::PSG(Keyboard& keyboard) : _keyboard(keyboard)
{
}

PSG::~PSG()
{
}

void PSG::Reset()
{
    _bdir = false;
    _bc1 = false;
    _selectedRegister = 0;

    for (byte& r : _register)
    {
        r = 0;
    }

    _keyboard.Reset();

    for (int c = 0; c < 3; c++)
    {
        _toneTicks[c] = 0;
        _state[c] = false;
    }

    _noiseTicks = 0;
    _noiseAmplitude = false;
    _noiseRandom = 0;

    _envelopeTickCounter = 0;
    _envelopeStepCount = 0;
    _envelopePeriodCount = 0;
    _envelopeState = envSustainLow;
    _noiseTickCounter = 0;
    _envelopeStepState = 0;
}

byte PSG::ChannelAmplitude(byte amp, bool toneEnable, bool noiseEnable, bool state)
{
    bool s = true;
    if (toneEnable)
    {
        s &= state;
    }

    if (noiseEnable)
    {
        s &= _noiseAmplitude;
    }

    if (s)
    {
        if ((amp & 0x10) != 0)
        {
            // Variable amplitude
            amp = _envelopeStepState;
        }
        else
        {
            // Fixed amplitude
            amp &= 0x0f;
        }
    }

    return s ? amp : 0;
}

void PSG::Amplitudes(byte(&amp)[3])
{
    amp[0] = ChannelAmplitude(_amplitudeA, ToneEnableA(), NoiseEnableA(), _state[0]);
    amp[1] = ChannelAmplitude(_amplitudeB, ToneEnableB(), NoiseEnableB(), _state[1]);
    amp[2] = ChannelAmplitude(_amplitudeC, ToneEnableC(), NoiseEnableC(), _state[2]);
}

void PSG::TickChannelState(word& tone, word& ticksCounter, bool& state)
{
    ticksCounter++;

    if (ticksCounter >= (tone * 16 / 2))
    {
        ticksCounter = 0;
        state = !state;
    }
}

void PSG::Tick()
{
    TickChannelState(_toneA, _toneTicks[0], _state[0]);
    TickChannelState(_toneB, _toneTicks[1], _state[1]);
    TickChannelState(_toneC, _toneTicks[2], _state[2]);

    // Noise
    _noiseTicks++;
    if (_noiseTicks >= ((_noisePeriod & 0x1f) * 16 / 2))
    {
        _noiseTicks = 0;

        word newBit = ((1 ^ (_noiseRandom >> 15) ^ (_noiseRandom >> 12)) & 0x0001);
        _noiseRandom = newBit | (_noiseRandom << 1);
        _noiseAmplitude = (newBit != 0);
    }

    // Envelope
    _envelopeTickCounter++;
    if (_envelopeTickCounter >= (_envelopePeriod * (256 / 16)))
    {
        _envelopeTickCounter = 0;
        _envelopeStepCount++;
        if (_envelopeStepCount >= 16)
        {
            _envelopeStepCount = 0;
            _envelopePeriodCount++;

            // Advance to next envelope state...
            switch (_register[13] & 0x0f)
            {
            case 0x00:
            case 0x01:
            case 0x02:
            case 0x03:
            case 0x04:
            case 0x05:
            case 0x06:
            case 0x07:
                _envelopeState = envSustainLow;
                break;
            case 0x08:
                break;
            case 0x09:
                _envelopeState = envSustainLow;
                break;
            case 0x0a:
                _envelopeState = envAttack;
                _register[13] = 0x0e;
                break;
            case 0x0b:
                _envelopeState = envSustainHigh;
                break;
            case 0x0c:
                break;
            case 0x0d:
                _envelopeState = envSustainHigh;
                break;
            case 0x0e:
                _envelopeState = envRelease;
                _register[13] = 0x0a;
                break;
            case 0x0f:
                _envelopeState = envSustainLow;
                break;
            }
        }

        switch (_envelopeState)
        {
        case envSustainLow:    _envelopeStepState = 0x00;                         break;
        case envSustainHigh:   _envelopeStepState = 0x0f;                         break;
        case envAttack:        _envelopeStepState = _envelopeStepCount;           break;
        case envRelease:       _envelopeStepState = 0x0f - _envelopeStepCount;    break;
        }
    }
}

void PSG::WriteRegister(byte b)
{
    if (_selectedRegister >= 16)
    {
        // Invalid register
        return;
    }

    _register[_selectedRegister] = b;
}

byte PSG::ReadRegister()
{
    if (_selectedRegister >= 16)
    {
        // Invalid register... return nothing.
        return 0;
    }

    byte b = 0;
    switch (_selectedRegister)
    {
    case 14:
        // IO Port A... should also check mixer bit 6? What to return if it's not set to Output?
        return _keyboard.ReadSelectedLine();
    default:
        return _register[_selectedRegister];
    }
}

StreamWriter& operator<<(StreamWriter& s, const PSG& psg)
{
    s << psg._bdir;
    s << psg._bc1;
    s << psg._selectedRegister;
    s << psg._register;
    s << psg._toneTicks;
    s << psg._state;
    s << psg._noiseTicks;
    s << psg._noiseAmplitude;
    s << psg._noiseRandom;
    s << psg._envelopeTickCounter;
    s << psg._envelopeStepCount;
    s << psg._envelopePeriodCount;
    s << psg._envelopeState;
    s << psg._noiseTickCounter;
    s << psg._envelopeStepState;

    return s;
}

StreamReader& operator>>(StreamReader& s, PSG& psg)
{
    s >> psg._bdir;
    s >> psg._bc1;
    s >> psg._selectedRegister;
    s >> psg._register;
    s >> psg._toneTicks;
    s >> psg._state;
    s >> psg._noiseTicks;
    s >> psg._noiseAmplitude;
    s >> psg._noiseRandom;
    s >> psg._envelopeTickCounter;
    s >> psg._envelopeStepCount;
    s >> psg._envelopePeriodCount;
    s >> psg._envelopeState;
    s >> psg._noiseTickCounter;
    s >> psg._envelopeStepState;

    return s;
}

byte PSG::Read()
{
    if (!_bdir && _bc1)
    {
        return ReadRegister();
    }

    return 0;
}

void PSG::Write(byte b)
{
    if (_bdir)
    {
        if (_bc1)
        {
            _selectedRegister = b;
        }
        else
        {
            WriteRegister(b);
        }
    }
}

void PSG::SetControl(bool bdir, bool bc1)
{
    _bdir = bdir;
    _bc1 = bc1;
}

bool PSG::Bc1()
{
    return _bc1;
}

bool PSG::Bdir()
{
    return _bdir;
}
