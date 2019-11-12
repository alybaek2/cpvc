#include "common.h"
#include "GateArray.h"

inline byte Nibble(bool b3, bool b2, bool b1, bool b0)
{
    return
        (b3 ? 8 : 0) |
        (b2 ? 4 : 0) |
        (b1 ? 2 : 0) |
        (b0 ? 1 : 0);
}

void Mode0(byte (&pixels)[8], byte (&pens)[16], byte b)
{
    byte pen0 = Nibble(Bit(b, 1), Bit(b, 5), Bit(b, 3), Bit(b, 7));
    byte pen1 = Nibble(Bit(b, 0), Bit(b, 4), Bit(b, 2), Bit(b, 6));

    pixels[0] = pens[pen0];
    pixels[1] = pens[pen0];
    pixels[2] = pens[pen0];
    pixels[3] = pens[pen0];
    pixels[4] = pens[pen1];
    pixels[5] = pens[pen1];
    pixels[6] = pens[pen1];
    pixels[7] = pens[pen1];
}

void Mode1(byte(&pixels)[8], byte(&pens)[16], byte b)
{
    byte pen0 = Nibble(false, false, Bit(b, 3), Bit(b, 7));
    byte pen1 = Nibble(false, false, Bit(b, 2), Bit(b, 6));
    byte pen2 = Nibble(false, false, Bit(b, 1), Bit(b, 5));
    byte pen3 = Nibble(false, false, Bit(b, 0), Bit(b, 4));

    pixels[0] = pens[pen0];
    pixels[1] = pens[pen0];
    pixels[2] = pens[pen1];
    pixels[3] = pens[pen1];
    pixels[4] = pens[pen2];
    pixels[5] = pens[pen2];
    pixels[6] = pens[pen3];
    pixels[7] = pens[pen3];
}

void Mode2(byte(&pixels)[8], byte(&pens)[16], byte b)
{
    byte pen0 = Nibble(false, false, false, Bit(b, 7));
    byte pen1 = Nibble(false, false, false, Bit(b, 6));
    byte pen2 = Nibble(false, false, false, Bit(b, 5));
    byte pen3 = Nibble(false, false, false, Bit(b, 4));
    byte pen4 = Nibble(false, false, false, Bit(b, 3));
    byte pen5 = Nibble(false, false, false, Bit(b, 2));
    byte pen6 = Nibble(false, false, false, Bit(b, 1));
    byte pen7 = Nibble(false, false, false, Bit(b, 0));

    pixels[0] = pens[pen0];
    pixels[1] = pens[pen1];
    pixels[2] = pens[pen2];
    pixels[3] = pens[pen3];
    pixels[4] = pens[pen4];
    pixels[5] = pens[pen5];
    pixels[6] = pens[pen6];
    pixels[7] = pens[pen7];
}

// Mode 3 is not an official screen mode, but is just a quirk of the hardware (given that the gate array
// uses two bits for the screen mode). It's the same as Mode 0, but bits 0, 1, 4, and 5 are unused.
void Mode3(byte(&pixels)[8], byte(&pens)[16], byte b)
{
    byte pen0 = Nibble(false, false, Bit(b, 3), Bit(b, 7));
    byte pen1 = Nibble(false, false, Bit(b, 2), Bit(b, 6));

    pixels[0] = pens[pen0];
    pixels[1] = pens[pen0];
    pixels[2] = pens[pen0];
    pixels[3] = pens[pen0];
    pixels[4] = pens[pen1];
    pixels[5] = pens[pen1];
    pixels[6] = pens[pen1];
    pixels[7] = pens[pen1];
}

GateArray::GateArray(Memory& memory, bool& pInterruptRequested, byte& pScanLineCount) : _memory(memory), _interruptRequested(pInterruptRequested), _scanLineCount(pScanLineCount)
{
    Reset();
};

GateArray::~GateArray()
{
};

void GateArray::Reset()
{
    _selectedPen = 0;
    _border = 0;
    _mode = 0;
    memset(_pen, 0, sizeof(_pen) / sizeof(_pen[0]));

    RenderPens();
}

byte GateArray::Read()
{
    return 0;
}

void GateArray::RenderPens()
{
    for (int b = 0; b < 256; b++)
    {
        Mode0(_renderedPenBytes[0][b], _pen, b);
        Mode1(_renderedPenBytes[1][b], _pen, b);
        Mode2(_renderedPenBytes[2][b], _pen, b);
        Mode3(_renderedPenBytes[3][b], _pen, b);
    }
}

void GateArray::Write(byte b)
{
    switch (b & 0xC0)
    {
    case 0x00:
        _selectedPen = b & 0x1F;
        break;
    case 0x40:
        if (Bit(_selectedPen, 4))
        {
            _border = b & 0x1F;
        }
        else
        {
            _pen[_selectedPen & 0x0F] = b & 0x1F;

            RenderPens();
        }
        break;
    case 0x80:
        if (Bit(b, 4))
        {
            _scanLineCount = 0;
            _interruptRequested = false;
        }

        _mode = b & 0x03;
        _memory.EnableLowerROM((b & 0x04) == 0);
        _memory.EnableUpperRom((b & 0x08) == 0);
        _memory.ConfigureRAM();
        break;
    }
}

StreamWriter& operator<<(StreamWriter& s, const GateArray& gateArray)
{
    s << gateArray._selectedPen;
    s << gateArray._pen;
    s << gateArray._border;
    s << gateArray._mode;

    return s;
}

StreamReader& operator>>(StreamReader& s, GateArray& gateArray)
{
    s >> gateArray._selectedPen;
    s >> gateArray._pen;
    s >> gateArray._border;
    s >> gateArray._mode;

    gateArray.RenderPens();

    return s;
}
