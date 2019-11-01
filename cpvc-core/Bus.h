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
    Bus(Memory& memory, IBusNoAddress& gateArray, IBus& ppi, IBus& crtc, FDC& fdc);
    ~Bus();

    Memory& _memory;
    IBus& _ppi;
    IBusNoAddress& _gateArray;
    IBus& _crtc;

    FDC& _fdc;

    byte Read(word addr);
    void Write(word addr, byte b);
};

