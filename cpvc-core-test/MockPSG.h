#pragma once

#include "..\cpvc-core\IPSG.h"
#include "..\cpvc-core\Keyboard.h"

class MockPSG : public IPSG
{
public:
    MockPSG(Keyboard keyboard)
    {
        Reset();
    }

    ~MockPSG()
    {
    }

    bool _bdir;
    bool _bc1;

    bool _writeCalled;
    byte _writeByte;

    bool _readCalled;
    byte _readByte;

    void Reset()
    {
        _bdir = false;
        _bc1 = false;
        _writeCalled = false;
        _writeByte = 0x00;
        _readCalled = false;
        _readByte = 0x00;
    }

    virtual bool Bc1()
    {
        return _bc1;
    }

    virtual bool Bdir()
    {
        return _bdir;
    }

    void SetControl(bool bdir, bool bc1)
    {
        _bdir = bdir;
        _bc1 = bc1;
    }

    byte Read()
    {
        _readCalled = true;
        return _readByte;
    }

    void Write(byte b)
    {
        _writeCalled = true;
        _writeByte = b;
    }
};

