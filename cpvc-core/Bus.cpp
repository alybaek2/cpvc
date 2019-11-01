#include "Bus.h"

Bus::Bus(Memory& memory, IBusNoAddress& gateArray, IBus& ppi, IBus& crtc, FDC& fdc) : _memory(memory), _gateArray(gateArray), _ppi(ppi), _crtc(crtc), _fdc(fdc)
{
}

Bus::~Bus()
{
}

byte Bus::Read(word addr)
{
    if (!Bit(addr, 11))
    {
        return _ppi.Read(addr);
    }

    if (!Bit(addr, 10))
    {
        // Expansion peripherals
        if (!Bit(addr, 7))
        {
            return _fdc.Read(addr);
        }
    }

    return 0;
}

void Bus::Write(word addr, byte b)
{
    if (!Bit(addr, 11))
    {
        _ppi.Write(addr, b);
    }

    if ((addr & 0xC000) == 0x4000)
    {
        _gateArray.Write(b);
    }

    if ((addr & 0x8000) == 0x0000)
    {
        if ((b & 0xC0) == 0xC0)
        {
            _memory.SetRAMConfig(b);
        }
    }

    if (!Bit(addr, 14))
    {
        _crtc.Write(addr, b);
    }

    if (!Bit(addr, 13))
    {
        _memory.SelectROM(b);
    }

    if (!Bit(addr, 10))
    {
        // Expansion peripherals
        if (!Bit(addr, 7))
        {
            _fdc.Write(addr, b);
        }
    }
}
