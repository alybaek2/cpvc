#include "Tape.h"

Tape::Tape()
{
    _pBuffer = nullptr;
    _size = 0;

    _motor = false;
    _level = false;
    _tickPos = 0;
    _ticksToNextLevelChange = 0;
    _playing = false;
}

Tape::~Tape()
{
}

bool Tape::Load(const bytevector& buffer)
{
    byte sig[] = { 'Z', 'X', 'T', 'a', 'p', 'e', '!', 0x1A };

    if (buffer.size() < 10 || (memcmp(sig, buffer.data(), sizeof(sig)) != 0))
    {
        return false;
    }

    _buffer = buffer;

    _pBuffer = _buffer.data();
    _size = (int) _buffer.size();

    _playing = true;

    Rewind();

    return true;
}

void Tape::Rewind()
{
    _phase = BlockPhase::Start;
    _blockIndex = 0;
    _currentBlockIndex = 10;

    _level = true;
    _tickPos = 0;
    _ticksToNextLevelChange = TicksToNextLevelChange();

    if (_ticksToNextLevelChange == -1)
    {
        _ticksToNextLevelChange = 0;
        _playing = false;
    }
}

void Tape::Eject()
{
    _pBuffer = nullptr;
    _buffer.resize(0);

    _size = 0;
}

void Tape::Tick()
{
    for (int i = 0; i < 4; i++)
    {
        if (!_playing || !_motor)
        {
            return;
        }

        if (_ticksToNextLevelChange <= 1)
        {
            qword ticks = 0;

            while (ticks == 0)
            {
                ticks = TicksToNextLevelChange();
            }

            if (ticks == -1)
            {
                _playing = false;
                _ticksToNextLevelChange = 0;
            }
            else
            {
                _ticksToNextLevelChange -= 1;
                _ticksToNextLevelChange += ticks;
            }
        }
        else
        {
            _ticksToNextLevelChange -= 1;
        }
    }
}

qword Tape::TicksToNextLevelChange()
{
    qword ticks = 0;

    while (ticks == 0)
    {
        if (_currentBlockIndex >= _size)
        {
            // End of the tape!
            return -1;
        }

        byte id = BlockByte(_pBuffer, _currentBlockIndex);

        switch (id)
        {
        case 0x10:  ticks = StepID10();  break;
        case 0x11:  ticks = StepID11();  break;
        case 0x12:  ticks = StepID12();  break;
        case 0x13:  ticks = StepID13();  break;
        case 0x14:  ticks = StepID14();  break;
        case 0x15:  ticks = StepID15();  break;
        case 0x20:  ticks = StepID20();  break;
        case 0x21:
        case 0x22:
        case 0x31:
        case 0x32:
        case 0x33:
            EndPhase();
            break;
        case 0x23:
        case 0x24:
        case 0x25:
        case 0x26:
        case 0x27:
        case 0x28:
        case 0x2A:
        case 0x2B:
        case 0x30:
        case 0x35:
        case 0x5A:
            throw "Not implemented!";
        default:
            throw id;
        }
    }

    return ticks;
}

dword Tape::BlockSize(byte* p)
{
    dword size = 0;

    byte id = BlockByte(p, 0);

    switch (id)
    {
    case 0x10:    size = 0x05 + BlockWord(p + 3, 0);           break;
    case 0x11:    size = 0x13 + BlockTripleByte(p + 16, 0);    break;
    case 0x12:    size = 0x05;                                 break;
    case 0x13:    size = 0x02 + BlockByte(p + 1, 0) * 2;       break;
    case 0x14:    size = 0x0B + BlockTripleByte(p + 8, 0);     break;
    case 0x15:    size = 0x09 + BlockTripleByte(p + 8, 0);     break;
    case 0x20:    size = 0x03;                                 break;
    case 0x21:    size = 0x02 + BlockByte(p + 1, 0);           break;
    case 0x22:    size = 0x01;                                 break;
    case 0x31:    size = 0x03 + BlockByte(p + 2, 0);           break;
    case 0x32:    size = 0x03 + BlockWord(p + 1, 0);           break;
    case 0x33:    size = 0x02 + BlockByte(p + 1, 0) * 3;       break;
    default:
        throw id;
    }

    return size;
}

qword Tape::DataPhase(byte* pData)
{
    if (_dataIndex == _dataBlock._length)
    {
        _pause = _dataBlock._pause;
        _phase = BlockPhase::Pause;

        return 0;
    }

    _level = !_level;
    qword ticks = ((_dataByte & 0x80) != 0) ? _dataBlock._oneLength : _dataBlock._zeroLength;

    if (!_levelChanged)
    {
        _levelChanged = true;
    }
    else
    {
        _dataByte = _dataByte << 1;
        _remainingBits--;

        if (_remainingBits == 0)
        {
            _dataIndex++;
            _remainingBits = ((_dataIndex + 1) == _dataBlock._length) ? _dataBlock._usedBitsLastByte : 8;
            _dataByte = pData[_dataIndex];
        }

        _levelChanged = false;
    }

    return ticks;
}

qword Tape::StepSpeedDataBlock(byte* pData)
{
    qword ticks = 0;

    switch (_phase)
    {
    case BlockPhase::Start:
        _phase = BlockPhase::Pilot;
        _pulsesRemaining = _speedBlock._pilotPulseCount;
        break;
    case BlockPhase::Pilot:
        if (_pulsesRemaining <= 0)
        {
            _phase = BlockPhase::SyncOne;

            return 0;
        }
        else
        {
            _level = !_level;
            ticks = AdjustTicks(_speedBlock._pilotPulseLength);
            _pulsesRemaining--;
        }

        return ticks;
    case BlockPhase::SyncOne:
        _level = !_level;
        ticks = AdjustTicks(_speedBlock._sync1Length);
        _phase = BlockPhase::SyncTwo;

        return ticks;
    case BlockPhase::SyncTwo:
        _level = !_level;
        ticks = AdjustTicks(_speedBlock._sync2Length);

        _phase = BlockPhase::Data;
        _dataIndex = 0;
        _levelChanged = false;
        _dataByte = pData[0];

        _remainingBits = ((_dataIndex + 1) == _dataBlock._length) ? _dataBlock._usedBitsLastByte : 8;

        return ticks;
    case BlockPhase::Data:
        return DataPhase(pData);
    case BlockPhase::Pause:
        if (_pause >= 1)
        {
            _level = !_level;
            ticks = 4000;

            _phase = BlockPhase::PauseZero;

            return ticks;
        }

        _phase = BlockPhase::End;

        return 0;
    case BlockPhase::PauseZero:
        _level = false;
        ticks = 4000 * ((qword) _pause);

        _phase = BlockPhase::End;

        return ticks;
    case BlockPhase::End:
        EndPhase();
        break;
    default:
        throw _phase;
    }

    return 0;
}

qword Tape::StepID10()
{
    byte* pBlock = _pBuffer + _currentBlockIndex;

    byte* pData = pBlock + 5;

    if (_phase == BlockPhase::Start)
    {
        _speedBlock._pilotPulseLength = 2168;
        _speedBlock._sync1Length = 667;
        _speedBlock._sync2Length = 735;
        _speedBlock._pilotPulseCount = (pData[0] & 0x80) ? 3223 : 8063;

        _dataBlock._zeroLength = AdjustTicks(855);
        _dataBlock._oneLength = AdjustTicks(1710);
        _dataBlock._usedBitsLastByte = 8;
        _dataBlock._pause = BlockWord(pBlock + 1, 0);
        _dataBlock._length = BlockWord(pBlock + 3, 0);
    }

    return StepSpeedDataBlock(pData);
}

qword Tape::StepID11()
{
    byte* pBlock = _pBuffer + _currentBlockIndex;

    if (_phase == BlockPhase::Start)
    {
        _speedBlock._pilotPulseLength = BlockWord(pBlock + 1, 0);
        _speedBlock._sync1Length = BlockWord(pBlock + 3, 0);
        _speedBlock._sync2Length = BlockWord(pBlock + 5, 0);
        _speedBlock._pilotPulseCount = BlockWord(pBlock + 11, 0);

        _dataBlock._zeroLength = AdjustTicks(BlockWord(pBlock + 7, 0));
        _dataBlock._oneLength = AdjustTicks(BlockWord(pBlock + 9, 0));
        _dataBlock._usedBitsLastByte = BlockByte(pBlock, 13);
        _dataBlock._pause = BlockWord(pBlock + 14, 0);
        _dataBlock._length = BlockTripleByte(pBlock + 16, 0);
    }

    return StepSpeedDataBlock(pBlock + 19);
}

qword Tape::StepID12()
{
    byte* pBlock = _pBuffer + _currentBlockIndex;

    qword ticks = 0;

    switch (_phase)
    {
    case BlockPhase::Start:
        _phase = BlockPhase::Pilot;
        _pulsesRemaining = BlockWord(pBlock + 3, 0);

        return 0;
    case BlockPhase::Pilot:
        if (_pulsesRemaining <= 0)
        {
            _phase = BlockPhase::End;
            return 0;
        }

		_level = !_level;
        _pulsesRemaining--;

        return AdjustTicks(BlockWord(pBlock + 1, 0));
    case BlockPhase::End:
        EndPhase();
        return 0;
    default:
        throw "Unexpected phase!";
    }
}

qword Tape::StepID13()
{
	byte* pBlock = _pBuffer + _currentBlockIndex;

    qword ticks = 0;

    switch (_phase)
    {
    case BlockPhase::Start:
        _phase = BlockPhase::Data;
        _pulseIndex = 0;
        return 0;
    case BlockPhase::Data:
        if (_pulseIndex >= BlockByte(pBlock, 1))
        {
            _phase = BlockPhase::End;
            return 0;
        }

        _level = !_level;
        ticks = AdjustTicks(BlockWord(pBlock + 2, _pulseIndex));
        _pulseIndex++;

        return ticks;
    case BlockPhase::End:
        EndPhase();
        return 0;
    default:
        throw "Unexpected phase!";
    }
}

qword Tape::StepID14()
{
	byte* pBlock = _pBuffer + _currentBlockIndex;

    qword ticks = 0;

    switch (_phase)
    {
    case BlockPhase::Start:
        _phase = BlockPhase::Data;
        _dataIndex = 0;
        _levelChanged = false;
        _dataByte = BlockByte(pBlock, 11);

        _dataBlock._zeroLength = AdjustTicks(BlockWord(pBlock + 1, 0));
        _dataBlock._oneLength = AdjustTicks(BlockWord(pBlock + 3, 0));
        _dataBlock._usedBitsLastByte = BlockByte(pBlock, 5);
        _dataBlock._pause = BlockWord(pBlock + 6, 0);
        _dataBlock._length = BlockTripleByte(pBlock + 8, 0);

        _remainingBits = ((_dataIndex + 1) == _dataBlock._length) ? _dataBlock._usedBitsLastByte : 8;
        break;
    case BlockPhase::Data:
        return DataPhase(pBlock + 11);
    case BlockPhase::Pause:
        if (_pause < 1)
        {
            _phase = BlockPhase::End;

            return 0;
        }

        _level = !_level;
        ticks = 4000;

        _phase = BlockPhase::PauseZero;

        return ticks;
    case BlockPhase::PauseZero:
        _level = false;
        ticks = 4000 * (qword)_pause;

        _phase = BlockPhase::End;

        return ticks;
    case BlockPhase::End:
        EndPhase();
        break;
    default:
        throw _phase;
    }

    return 0;
}

qword Tape::StepID15()
{
	byte* pBlock = _pBuffer + _currentBlockIndex;

    qword ticks = 0;

    byte usedBitsLastByte = BlockByte(pBlock, 5);
    dword length = BlockTripleByte(pBlock + 6, 0);

    switch (_phase)
    {
    case BlockPhase::Start:
        _phase = BlockPhase::Data;

        _dataIndex = 0;
        _dataByte = BlockByte(pBlock, 9);
        _remainingBits = ((_dataIndex + 1) == length) ? usedBitsLastByte : 8;
        break;
    case BlockPhase::Data:
        if (_dataIndex == length)
        {
            _phase = BlockPhase::Pause;
            _pause = BlockWord(pBlock + 3, 0);
            return 0;
        }

        _level = ((_dataByte & 0x80) != 0);
        ticks = AdjustTicks(BlockWord(pBlock + 1, 0));

        _remainingBits--;
        if (_remainingBits == 0)
        {
            _dataIndex++;
            _dataByte = BlockByte(pBlock, 9 + _dataIndex);
            _remainingBits = ((_dataIndex + 1) == length) ? usedBitsLastByte : 8;
        }
        else
        {
            _dataByte = _dataByte << 1;
        }

        return ticks;
    case BlockPhase::Pause:
        if (_pause >= 1)
        {
            _level = !_level;
            ticks = 4000;

            _phase = BlockPhase::PauseZero;

            return ticks;
        }

        _phase = BlockPhase::End;

        return 0;
    case BlockPhase::PauseZero:
        _level = false;
        ticks = 4000 * (qword) _pause;

        _phase = BlockPhase::End;

        return ticks;
    case BlockPhase::End:
        EndPhase();
        break;
    default:
        throw _phase;
    }

    return 0;
}

qword Tape::StepID20()
{
	byte* pBlock = _pBuffer + _currentBlockIndex;

    word pause = BlockWord(pBlock + 1, 0);
	if (pause == 0)
    {
        // As per documentation, stop the tape!
        return -1;
    }

    qword ticks = 0;
    switch (_phase)
    {
    case BlockPhase::Start:
        _pause = pause;
        _phase = BlockPhase::Pause;
        break;
    case BlockPhase::Pause:
        _level = !_level;
        ticks = 4000;

        _phase = BlockPhase::PauseZero;

        return ticks;
    case BlockPhase::PauseZero:
        _level = false;
        ticks = 4000 * (qword) _pause;

        _phase = BlockPhase::End;

        return ticks;
    case BlockPhase::End:
        EndPhase();
        break;
    default:
        throw _phase;
    }

    return ticks;
}

void Tape::EndPhase()
{
    _currentBlockIndex += BlockSize(_pBuffer + _currentBlockIndex);
    _phase = BlockPhase::Start;
}

StreamWriter& operator<<(StreamWriter& s, const Tape& tape)
{
    s << tape._currentBlockIndex;
    s << tape._blockIndex;
    s << tape._phase;
    s << tape._pulsesRemaining;
    s << tape._dataIndex;
    s << tape._levelChanged;
    s << tape._dataByte;
    s << tape._remainingBits;
    s << tape._pulseIndex;
    s << tape._pause;

    s << tape._dataBlock._zeroLength;
    s << tape._dataBlock._oneLength;
    s << tape._dataBlock._usedBitsLastByte;
    s << tape._dataBlock._pause;
    s << tape._dataBlock._length;

    s << tape._speedBlock._pilotPulseLength;
    s << tape._speedBlock._sync1Length;
    s << tape._speedBlock._sync2Length;
    s << tape._speedBlock._pilotPulseCount;

    s << tape._playing;
    s << tape._level;
    s << tape._motor;
    s << tape._tickPos;
    s << tape._ticksToNextLevelChange;

    s << tape._size;
    s << tape._buffer;

    return s;
}

StreamReader& operator>>(StreamReader& s, Tape& tape)
{
    s >> tape._currentBlockIndex;
    s >> tape._blockIndex;
    s >> tape._phase;
    s >> tape._pulsesRemaining;
    s >> tape._dataIndex;
    s >> tape._levelChanged;
    s >> tape._dataByte;
    s >> tape._remainingBits;
    s >> tape._pulseIndex;
    s >> tape._pause;

    s >> tape._dataBlock._zeroLength;
    s >> tape._dataBlock._oneLength;
    s >> tape._dataBlock._usedBitsLastByte;
    s >> tape._dataBlock._pause;
    s >> tape._dataBlock._length;

    s >> tape._speedBlock._pilotPulseLength;
    s >> tape._speedBlock._sync1Length;
    s >> tape._speedBlock._sync2Length;
    s >> tape._speedBlock._pilotPulseCount;

    s >> tape._playing;
    s >> tape._level;
    s >> tape._motor;
    s >> tape._tickPos;
    s >> tape._ticksToNextLevelChange;

    s >> tape._size;
    s >> tape._buffer;

    tape._pBuffer = tape._buffer.data();

    return s;
}

StreamWriter& operator<<(StreamWriter& s, const Tape::BlockPhase& phase)
{
    s << (int) phase;

    return s;
}

StreamReader& operator>>(StreamReader& s, Tape::BlockPhase& phase)
{
    s >> (int&) phase;

    return s;
}
