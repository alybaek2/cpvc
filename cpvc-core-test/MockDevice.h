#pragma once

#include "..\cpvc-core\IBus.h"

class MockDevice : public IBus, public IBusNoAddressWriteOnly
{
public:
    MockDevice()
    {
        Reset();
    }

    ~MockDevice()
    {
    }

    bool _writeCalled;
    word _writeAddress;
    byte _writeByte;

    bool _readCalled = false;
    word _readAddress = 0x0000;
    byte _readByte = 0x00;

    void Reset()
    {
        _writeCalled = false;
        _writeAddress = 0x0000;
        _writeByte = 0x00;

        _readCalled = false;
        _readAddress = 0x0000;
        _readByte = 0x00;
    }

    byte Read(word addr)
    {
        _readCalled = true;
        _readAddress = addr;
        return _readByte;
    }

    void Write(word addr, byte b)
    {
        _writeCalled = true;
        _writeAddress = addr;
        _writeByte = b;
    }

    void Write(byte b)
    {
        _writeCalled = true;
        _writeByte = b;
    }
};
