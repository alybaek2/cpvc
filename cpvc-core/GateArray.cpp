#include "common.h"
#include "GateArray.h"

constexpr byte on   = 0xFF;
constexpr byte half = 0x7F;
constexpr byte off  = 0x00;

constexpr dword RGB(byte r, byte g, byte b)
{
    return (((dword) b) | ((dword) g << 8) | ((dword) r << 16));
}

dword colours[32] = {
    RGB(half, half, half),   //  0 - White
    RGB(half, half, half),   //  1 - White
    RGB(off,  on,   half),   //  2 - Sea Green
    RGB(on,   on,   half),   //  3 - Pastel Yellow
    RGB(off,  off,  half),   //  4 - Blue
    RGB(on,   off,  half),   //  5 - Purple
    RGB(off,  half, half),   //  6 - Cyan
    RGB(on,   half, half),   //  7 - Pink
    RGB(on,   off,  half),   //  8 - Purple
    RGB(on,   on,   half),   //  9 - Pastel Yellow
    RGB(on,   on,   off),    // 10 - Bright Yellow
    RGB(on,   on,   on),     // 11 - Bright White
    RGB(on,   off,  off),    // 12 - Bright Red
    RGB(on,   off,  on),     // 13 - Bright Magenta
    RGB(on,   half, off),    // 14 - Orange
    RGB(on,   half, on),     // 15 - Pastel Magenta
    RGB(off,  off,  half),   // 16 - Blue
    RGB(off,  on,   half),   // 17 - Sea Green
    RGB(off,  on,   off),    // 18 - Bright Green
    RGB(off,  on,   on),     // 19 - Bright Cyan
    RGB(off,  off,  off),    // 20 - Black
    RGB(off,  off,  on),     // 21 - Bright Blue
    RGB(off,  half, off),    // 22 - Green
    RGB(off,  half, on),     // 23 - Sky Blue
    RGB(half, off,  half),   // 24 - Magenta
    RGB(half, on,   half),   // 25 - Pastel Green
    RGB(half, on,   off),    // 26 - Lime
    RGB(half, on,   on),     // 27 - Pastel Cyan
    RGB(half, off,  off),    // 28 - Red
    RGB(half, off,  on),     // 29 - Mauve
    RGB(half, half, off),    // 30 - Yellow
    RGB(half, half, on)      // 31 - Pastel Blue
};

inline byte Nibble(bool b3, bool b2, bool b1, bool b0)
{
    return
        (b3 ? 8 : 0) |
        (b2 ? 4 : 0) |
        (b1 ? 2 : 0) |
        (b0 ? 1 : 0);
}

void Mode0(dword (&pixels)[8], byte* pens, byte b)
{
    byte pen0 = Nibble(Bit(b, 1), Bit(b, 5), Bit(b, 3), Bit(b, 7));
    byte pen1 = Nibble(Bit(b, 0), Bit(b, 4), Bit(b, 2), Bit(b, 6));

    pixels[0] = colours[pens[pen0]];
    pixels[1] = pixels[0];
    pixels[2] = pixels[0];
    pixels[3] = pixels[0];
    pixels[4] = colours[pens[pen1]];
    pixels[5] = pixels[4];
    pixels[6] = pixels[4];
    pixels[7] = pixels[4];
}

void Mode1(dword(&pixels)[8], byte* pens, byte b)
{
    byte pen0 = Nibble(false, false, Bit(b, 3), Bit(b, 7));
    byte pen1 = Nibble(false, false, Bit(b, 2), Bit(b, 6));
    byte pen2 = Nibble(false, false, Bit(b, 1), Bit(b, 5));
    byte pen3 = Nibble(false, false, Bit(b, 0), Bit(b, 4));

    pixels[0] = colours[pens[pen0]];
    pixels[1] = pixels[0];
    pixels[2] = colours[pens[pen1]];
    pixels[3] = pixels[2];
    pixels[4] = colours[pens[pen2]];
    pixels[5] = pixels[4];
    pixels[6] = colours[pens[pen3]];
    pixels[7] = pixels[6];
}

void Mode2(dword(&pixels)[8], byte* pens, byte b)
{
    byte pen0 = Nibble(false, false, false, Bit(b, 7));
    byte pen1 = Nibble(false, false, false, Bit(b, 6));
    byte pen2 = Nibble(false, false, false, Bit(b, 5));
    byte pen3 = Nibble(false, false, false, Bit(b, 4));
    byte pen4 = Nibble(false, false, false, Bit(b, 3));
    byte pen5 = Nibble(false, false, false, Bit(b, 2));
    byte pen6 = Nibble(false, false, false, Bit(b, 1));
    byte pen7 = Nibble(false, false, false, Bit(b, 0));

    pixels[0] = colours[pens[pen0]];
    pixels[1] = colours[pens[pen1]];
    pixels[2] = colours[pens[pen2]];
    pixels[3] = colours[pens[pen3]];
    pixels[4] = colours[pens[pen4]];
    pixels[5] = colours[pens[pen5]];
    pixels[6] = colours[pens[pen6]];
    pixels[7] = colours[pens[pen7]];
}

void Border(dword(&pixels)[8], byte border)
{
    pixels[0] = colours[border];
    pixels[1] = pixels[0];
    pixels[2] = pixels[0];
    pixels[3] = pixels[0];
    pixels[4] = pixels[0];
    pixels[5] = pixels[0];
    pixels[6] = pixels[0];
    pixels[7] = pixels[0];
}

GateArray::GateArray(Memory& memory, bool& pInterruptRequested, byte& pScanLineCount) : _memory(memory), _interruptRequested(pInterruptRequested), _scanLineCount(pScanLineCount)
{
    _selectedPen = 0;
    memset(_pen, 0, 16);
    _border = 0;
    _mode = 0;
};

GateArray::~GateArray()
{
};

void GateArray::Reset()
{
    _selectedPen = 0;
    _border = 0;
    _mode = 0;
    memset(_pen, 0, sizeof(_pen[0]));

    RenderBorder();
    RenderPens();
}

byte GateArray::Read()
{
    return 0;
}

void GateArray::RenderBorder()
{
    Border(_renderedBorderBytes, _border);
}

void GateArray::RenderPens()
{
    for (int b = 0; b < 256; b++)
    {
        Mode0(_renderedPenBytes[0][b], _pen, b);
        Mode1(_renderedPenBytes[1][b], _pen, b);
        Mode2(_renderedPenBytes[2][b], _pen, b);
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

            RenderBorder();
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

    gateArray.RenderBorder();
    gateArray.RenderPens();

    return s;
}
