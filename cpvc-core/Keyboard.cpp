#include "Keyboard.h"

Keyboard::Keyboard()
{
    Reset();
}

Keyboard::~Keyboard()
{
}

void Keyboard::Reset()
{
    for (int m = 0; m < _lineCount; m++)
    {
        _matrix[m] = 0xFF;
        _matrixClash[m] = 0xFF;
    }

    _selectedLine = 0;
}

// Emulates the CPC's keyboard clash.
void Keyboard::Clash()
{
    for (byte& line : _matrixClash)
    {
        line = 0xFF;
    }

    for (byte line0 = 0; line0 < _lineCount; line0++)
    {
        for (byte line1 = line0 + 1; line1 < _lineCount; line1++)
        {
            for (byte bit0 = 0; bit0 < 8; bit0++)
            {
                for (byte bit1 = bit0 + 1; bit1 < 8; bit1++)
                {
                    byte matrixLine0 = _matrix[line0];
                    byte matrixLine1 = _matrix[line1];
                    bool key00 = !Bit(matrixLine0, bit0);
                    bool key01 = !Bit(matrixLine0, bit1);
                    bool key10 = !Bit(matrixLine1, bit0);
                    bool key11 = !Bit(matrixLine1, bit1);

                    if (key00 && key01 && key10)
                    {
                        SetLineState(_matrixClash, line1, bit1, true);
                    }

                    if (key00 && key01 && key11)
                    {
                        SetLineState(_matrixClash, line1, bit0, true);
                    }

                    if (key00 && key10 && key11)
                    {
                        SetLineState(_matrixClash, line0, bit1, true);
                    }

                    if (key01 && key10 && key11)
                    {
                        SetLineState(_matrixClash, line0, bit0, true);
                    }
                }
            }
        }
    }
}

byte Keyboard::SetLineState(byte(&matrix)[_lineCount], byte line, byte bit, bool state)
{
    byte mask = 1 << bit;
    byte before = matrix[line];
    if (state)
    {
        matrix[line] &= (~mask);
    }
    else
    {
        matrix[line] |= mask;
    }

    return (before ^ matrix[line]);
}

bool Keyboard::KeyPress(byte line, byte bit, bool down)
{
    if (line >= _lineCount || bit >= 8)
    {
        // Invalid line or bit.
        return false;
    }

    byte changed = SetLineState(_matrix, line, bit, down);

    Clash();

    return changed != 0;
}

byte Keyboard::ReadSelectedLine()
{
    if (_selectedLine >= _lineCount)
    {
        // Invalid line selected... return a line of unpressed keys.
        return 0xFF;
    }

    return _matrix[_selectedLine] & _matrixClash[_selectedLine];
}

void Keyboard::SelectLine(byte line)
{
    _selectedLine = line;
}

byte Keyboard::SelectedLine()
{
    return _selectedLine;
}

StreamWriter& operator<<(StreamWriter& s, const Keyboard& keyboard)
{
    s << keyboard._matrix;
    s << keyboard._matrixClash;
    s << keyboard._selectedLine;

    return s;
}

StreamReader& operator>>(StreamReader& s, Keyboard& keyboard)
{
    s >> keyboard._matrix;
    s >> keyboard._matrixClash;
    s >> keyboard._selectedLine;

    return s;
}
