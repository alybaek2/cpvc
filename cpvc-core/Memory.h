#pragma once

#include <map>
#include <memory>
#include "common.h"
#include "StreamReader.h"
#include "StreamWriter.h"
#include "Blob.h"

typedef Blob<byte, 0x4000> Mem16k;


class Memory
{
public:
    Memory()
    {
        Reset();
    };

    ~Memory() {};

private:
    Mem16k _banks[8];
    byte* _readRAM[4];
    byte* _writeRAM[4];

    byte _ramConfig;
    byte _ramConfigs[8][4] = {
        { 0, 1, 2, 3 },
        { 0, 1, 2, 7 },
        { 4, 5, 6, 7 },
        { 0, 3, 2, 7 },
        { 0, 4, 2, 3 },
        { 0, 5, 2, 3 },
        { 0, 6, 2, 3 },
        { 0, 7, 2, 3 }
    };

    bool _lowerRomEnabled;
    Mem16k _lowerRom;

    bool _upperRomEnabled;
    Mem16k _upperRom;
    byte _selectedUpperRom;
    std::map<byte, Mem16k> _roms;

public:
    void Reset()
    {
        for (Mem16k& mem : _banks)
        {
            mem.Fill(0);
        }

        _lowerRomEnabled = true;
        _upperRomEnabled = true;
        _selectedUpperRom = 0;

        SetRAMConfig(0);
    }

    byte VideoRead(const word& addr)
    {
        byte bankIndex = addr >> 14;
        return _writeRAM[bankIndex][addr & 0x3FFF];
    }

    byte Read(const word& addr)
    {
        byte bankIndex = addr >> 14;
        return _readRAM[bankIndex][addr & 0x3FFF];
    }

    void Write(const word& addr, byte b)
    {
        byte bankIndex = addr >> 14;
        _writeRAM[bankIndex][addr & 0x3FFF] = b;
    }

    void SetLowerROM(const Mem16k& lowerRom)
    {
        _lowerRom = lowerRom;
    }

    void EnableLowerROM(bool enable)
    {
        _lowerRomEnabled = enable;
    }

    void AddUpperRom(byte slot, const Mem16k& rom)
    {
        _roms[slot] = rom;
    }

    void RemoveUpperRom(byte slot)
    {
        _roms.erase(slot);
    }

    void EnableUpperRom(bool enable)
    {
        _upperRomEnabled = enable;
    }

    void SelectROM(byte rom)
    {
        if (_roms.find(rom) == _roms.end())
        {
            rom = 0;
        }

        _selectedUpperRom = rom;
        _upperRom = _roms[rom];

        ConfigureRAM();
    }

    void SetRAMConfig(byte config)
    {
        _ramConfig = config & 0x07;
        ConfigureRAM();
    }

    void ConfigureRAM()
    {
        for (byte b = 0; b < 4; b++)
        {
            _readRAM[b] = _writeRAM[b] = _banks[_ramConfigs[_ramConfig][b]];
        }

        if (_lowerRomEnabled)
        {
            _readRAM[0] = _lowerRom;
        }

        if (_upperRomEnabled)
        {
            _readRAM[3] = _upperRom;
        }
    }

    friend StreamWriter& operator<<(StreamWriter& s, const Memory& memory)
    {
        s << memory._banks;
        s << memory._ramConfig;
        s << memory._lowerRomEnabled;
        s << memory._upperRomEnabled;
        s << memory._selectedUpperRom;
        s << memory._lowerRom;
        s << memory._roms;

        return s;
    }

    friend StreamReader& operator>>(StreamReader& s, Memory& memory)
    {
        s >> memory._banks;
        s >> memory._ramConfig;
        s >> memory._lowerRomEnabled;
        s >> memory._upperRomEnabled;
        s >> memory._selectedUpperRom;
        s >> memory._lowerRom;

        s >> memory._roms;

        // Probably more consistent to serialize each read and write bank separately, as it's not
        // guaranteed that they will be in sync with _ramConfig, even though they should be!
        memory.ConfigureRAM();

        // Ensure the upper rom is copied to _upperRom...
        memory.SelectROM(memory._selectedUpperRom);

        return s;
    }
};

