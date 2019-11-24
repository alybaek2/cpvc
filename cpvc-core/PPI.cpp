#include "PPI.h"

PPI::PPI(IPSG& psg, Keyboard& keyboard, bool* pVSync, bool* pTapeMotor, bool* pTapeLevel) : _psg(psg), _keyboard(keyboard), _pVSync(pVSync), _tapeMotor(*pTapeMotor), _tapeLevel(*pTapeLevel)
{
}

PPI::~PPI()
{
}

void PPI::Reset()
{
    _tapeLevel = false;
    _printerReady = false;
    _exp = false;
    _refreshRate = true;  // true for 50Hz, false for 60Hz.
    _manufacturer = 0x07; // "Amstrad" vendor name (as seen when the CPC boots).

    _tapeWriteData = false;
    _tapeMotor = false;

    _portA = 0;
    _portB = 0;
    _portC = 0;
    _control = 0;
}

byte PPI::Read(word addr)
{
    switch (addr & 0x0300)
    {
    case 0x0000:
    {
        if (PortAIO() == Input)
        {
            return _psg.Read();
        }
        else
        {
            return _portA;
        }
    }
    break;
    case 0x0100:
    {
        if (PortBIO() == Input)
        {
            // Input
            byte b =
                (_tapeLevel ? 0x80 : 0x00) |    // Cassette read data
                (_printerReady ? 0x40 : 0x00) | // Printer Ready
                (_exp ? 0x20 : 0x00) |          // /EXP
                (_refreshRate ? 0x10 : 0x00) |  // 1 for 50Hz, 0 for 60Hz
                (_manufacturer << 1) |          // Manufacturer code
                (*_pVSync ? 0x01 : 0x00);       // Vsync signal

            return b;
        }
        else
        {
            return _portB;
        }
    }
    break;
    case 0x0200:
    {
        byte b = _portC;

        if (PortCHighIO() == Input)
        {
            b = (b & 0x0F) |
                (_psg.Bdir() ? 0x80 : 0x00) |
                (_psg.Bc1() ? 0x40 : 0x00) |
                (_tapeWriteData ? 0x20 : 0x00) |
                (_tapeMotor ? 0x10 : 0x00);
        }

        if (PortCLowIO() == Input)
        {
            b = (b & 0xF0) |
                (_keyboard.SelectedLine() & 0x0F);
        }

        return _portC;
    }
    break;
    case 0x0300:
    {
        // Control is write-only
    }
    break;
    }

    return 0;
}

void PPI::WritePortC()
{
    if (PortCLowIO() == Output)
    {
        _keyboard.SelectLine(_portC & 0x0F);
    }

    if (PortCHighIO() == Output)
    {
        _tapeMotor = Bit(_portC, 4);
        _tapeWriteData = Bit(_portC, 5);
        _psg.SetControl(Bit(_portC, 7), Bit(_portC, 6));

        _psg.Write(_portA);
    }
}

void PPI::Write(word addr, byte b)
{
    switch (addr & 0x0300)
    {
    case 0x0000:
    {
        _portA = b;
        if (PPI::PortAIO() == Output)
        {
            _psg.Write(_portA);
        }
    }
    break;
    case 0x0100:
    {
        _portB = b;
    }
    break;
    case 0x0200:
    {
        _portC = b;

        WritePortC();
    }
    break;
    case 0x0300:
    {
        if (Bit(b, 7))
        {
            // Set the control
            _control = b;

            _portA = 0x00;
            _portB = 0x00;
            _portC = 0x00;
        }
        else if (PortCHighIO() == Output && PortCLowIO() == Output)
        {
            // Bit set/reset
            byte bit = (b & 0x0E) >> 1;

            _portC &= (~(1 << bit));
            _portC |= (b & 0x01) << bit;

            // Retrigger the code for modifying Port C
            WritePortC();
        }
    }
    break;
    }
}

StreamWriter& operator<<(StreamWriter& s, const PPI& ppi)
{
    s << ppi._printerReady;
    s << ppi._exp;
    s << ppi._refreshRate;
    s << ppi._manufacturer;
    s << ppi._tapeWriteData;
    s << ppi._portA;
    s << ppi._portB;
    s << ppi._portC;
    s << ppi._control;

    return s;
}

StreamReader& operator>>(StreamReader& s, PPI& ppi)
{
    s >> ppi._printerReady;
    s >> ppi._exp;
    s >> ppi._refreshRate;
    s >> ppi._manufacturer;
    s >> ppi._tapeWriteData;
    s >> ppi._portA;
    s >> ppi._portB;
    s >> ppi._portC;
    s >> ppi._control;

    return s;
}
