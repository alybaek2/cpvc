#pragma once

#include "common.h"
#include "Memory.h"
#include "GateArray.h"
#include "FDC.h"

#include "IBus.h"

// Represents the IO bus.
class Bus : public IBus
{
public:
    Bus(Memory& memory, IBusNoAddressWriteOnly& gateArray, IBus& ppi, IBus& crtc, IBus& fdc);
    ~Bus();

    Memory& _memory;
    IBus& _ppi;
    IBusNoAddressWriteOnly& _gateArray;
    IBus& _crtc;

    IBus& _fdc;

    byte Read(word addr);
    void Write(word addr, byte b);
};

