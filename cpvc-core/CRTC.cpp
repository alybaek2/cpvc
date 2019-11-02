#include "CRTC.h"

#include "StreamReader.h"
#include "StreamWriter.h"

CRTC::CRTC(bool& requestInterrupt) : _requestInterrupt(requestInterrupt)
{
    Reset();
}

CRTC::~CRTC()
{
}

void CRTC::Reset()
{
    _x = 0;
    _y = 0;
    _raster = 0;

    _hCount = 0;
    _vCount = 0;
    _inHSync = false;
    _hSyncCount = 0;
    _inVSync = false;
    _vSyncCount = 0;
    _inVTotalAdjust = false;
    _vTotalAdjustCount = 0;

    _scanLineCount = 0;
    _vSyncDelay = 0;

    _memoryAddress = 0x0000;

    _selectedRegister = 0;

    _register[0] = 0x3F;
    _register[1] = 0x28;
    _register[2] = 0x2E;
    _register[3] = 0x8E;
    _register[4] = 0x26;
    _register[5] = 0x00;
    _register[6] = 0x19;
    _register[7] = 0x1E;
    _register[8] = 0x00;
    _register[9] = 0x07;
    _register[10] = 0x00;
    _register[11] = 0x00;
    _register[12] = 0x30;
    _register[13] = 0x00;
    _register[14] = 0xC0;
    _register[15] = 0x00;
    _register[16] = 0x00;
    _register[17] = 0x00;
}

byte CRTC::ReadRegister()
{
    if (_selectedRegister >= 12 && _selectedRegister <= 17)
    {
        return _register[_selectedRegister];
    }

    return 0x00;
}

void CRTC::WriteRegister(byte b)
{
    if (_selectedRegister >= 0 && _selectedRegister <= 15)
    {
        // Mask off any unused bits first...
        switch (_selectedRegister)
        {
        case 4:
        case 6:
        case 7:
        case 10:
            b &= 0x7f;
            break;
        case 5:
        case 9:
        case 11:
            b &= 0x1f;
            break;
        case 8:
            b &= 0x03;
            break;
        case 12:
        case 14:
            b &= 0x3f;
            break;
        }

        _register[_selectedRegister] = b;
    }
}

byte CRTC::Read(word addr)
{
    switch (addr & 0x0300)
    {
    case 0x0300:
        return ReadRegister();
        break;
    }

    return 0;
}

void CRTC::Write(word addr, byte b)
{
    switch (addr & 0x0300)
    {
    case 0x0000:
        _selectedRegister = b;
        break;
    case 0x0100:
        WriteRegister(b);
        break;
    case 0x0200:
        break;
    case 0x0300:
        break;
    }
}

void CRTC::VSyncStart()
{
    _vSyncDelay = 2;
}

void CRTC::HSyncEnd()
{
    _scanLineCount++;

    bool raiseInterrupt = false;
    if (_scanLineCount == 52)
    {
        _scanLineCount = 0;
        raiseInterrupt = true;
    }

    if (_vSyncDelay != 0)
    {
        _vSyncDelay--;
        if (_vSyncDelay == 0)
        {
            if (_scanLineCount >= 32)
            {
                raiseInterrupt = true;
            }

            _scanLineCount = 0;
        }
    }

    if (raiseInterrupt)
    {
        _requestInterrupt = true;
    }

}

void CRTC::Tick()
{
    bool newFrame = false;

    _x++;
    _hCount++;
    if (_inHSync)
    {
        _hSyncCount++;
        _hSyncCount &= 0x0f;
        if (_hSyncCount == (_horizontalAndVerticalSyncWidth & 0x0F))
        {
            _inHSync = false;
            _x = 0;
            _y++;

            HSyncEnd();
        }
    }
    else if (_hCount == _horizontalSyncPosition)
    {
        _inHSync = true;
        _hSyncCount = 0;
    }

    // Note that the value written to the Horizontal Total register is one less that the specified value.
    if (_hCount == (_horizontalTotal + 1))
    {
        _hCount = 0;
        _raster++;
        _raster &= 0x1f;

        if (_inVTotalAdjust)
        {
            _vTotalAdjustCount++;
            if (_vTotalAdjustCount == _verticalTotalAdjust)
            {
                _inVTotalAdjust = false;
                newFrame = true;
            }
        }
        else if (_inVSync)
        {
            _vSyncCount++;

            _vSyncCount &= 0x0f;

            if (_vSyncCount == (_horizontalAndVerticalSyncWidth >> 4))
            {
                _y = 0;
                _inVSync = false;
            }
        }

        if (!_inVTotalAdjust && !newFrame && _raster == (_maximumRasterAddress + 1))
        {
            _raster = 0;
            _vCount++;

            _vCount &= 0x7f;

            _memoryAddress += _horizontalDisplayed;

            if (_vCount == (_verticalSyncPosition + 0))
            {
                _inVSync = true;
                _vSyncCount = 0;

                VSyncStart();
            }

            if (!_inVSync && (_vCount == (_verticalTotal + 1)))
            {
                if (_verticalTotalAdjust == 0)
                {
                    newFrame = true;
                }
                else
                {
                    _inVTotalAdjust = true;
                    _vTotalAdjustCount = 0;
                }
            }
        }
    }

    if (newFrame)
    {
        _vCount = 0;
        _raster = 0;
        _memoryAddress = MakeWord(_displayStartAddressHigh, _displayStartAddressLow);
    }
}

StreamWriter& operator<<(StreamWriter& s, const CRTC& crtc)
{
    s << crtc._x;
    s << crtc._y;
    s << crtc._hCount;
    s << crtc._vCount;
    s << crtc._raster;
    s << crtc._inHSync;
    s << crtc._hSyncCount;
    s << crtc._inVSync;
    s << crtc._vSyncCount;
    s << crtc._inVTotalAdjust;
    s << crtc._vTotalAdjustCount;
    s << crtc._scanLineCount;
    s << crtc._vSyncDelay;
    s << crtc._memoryAddress;
    s << crtc._register;
    s << crtc._selectedRegister;

    return s;
}

StreamReader& operator>>(StreamReader& s, CRTC& crtc)
{
    s >> crtc._x;
    s >> crtc._y;
    s >> crtc._hCount;
    s >> crtc._vCount;
    s >> crtc._raster;
    s >> crtc._inHSync;
    s >> crtc._hSyncCount;
    s >> crtc._inVSync;
    s >> crtc._vSyncCount;
    s >> crtc._inVTotalAdjust;
    s >> crtc._vTotalAdjustCount;
    s >> crtc._scanLineCount;
    s >> crtc._vSyncDelay;
    s >> crtc._memoryAddress;
    s >> crtc._register;
    s >> crtc._selectedRegister;

    return s;
}
