#pragma once

#include "common.h"

#include "Memory.h"
#include "IBus.h"


class GateArray : public IBusNoAddress
{
public:
    GateArray(Memory& memory, bool& interruptRequested, byte& scanLineCount);
    ~GateArray();

    Memory& _memory;
    byte _selectedPen;
    byte _pen[16];
    byte _border;
    byte _mode;

    byte& _scanLineCount;
    bool& _interruptRequested;

    byte _renderedPenBytes[4][256][8];

    void Reset();

    byte Read();
    void Write(byte b);

    void RenderPens();

    friend StreamWriter& operator<<(StreamWriter& s, const GateArray& gateArray);
    friend StreamReader& operator>>(StreamReader& s, GateArray& gateArray);
};