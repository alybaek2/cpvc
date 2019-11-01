#pragma once
#include "common.h"
#include "PSG.h"
#include "Keyboard.h"
#include "IBus.h"
#include "IPSG.h"

class PPI : public IBus
{
public:
    PPI(IPSG& psg, Keyboard& keyboard, bool* pVSync, bool* pTapeMotor, bool* pTapeLevel);
    ~PPI();

private:
    enum IO
    {
        Input,
        Output
    };

    enum Mode
    {
        Mode0,
        Mode1,
        Mode2
    };

    Keyboard& _keyboard;
    IPSG& _psg;
    bool* _pVSync;
    bool& _tapeLevel;

public:

    bool _printerReady;
    bool _exp;
    bool _refreshRate;
    byte _manufacturer;

    bool _tapeWriteData;
    bool& _tapeMotor;

    byte _portA;
    byte _portB;
    byte _portC;
    byte _control;

    IO PortCLowIO() { return Bit(_control, 0) ? Input : Output; }
    IO PortBIO() { return Bit(_control, 1) ? Input : Output; }
    Mode GroupBMode() { return Bit(_control, 2) ? Mode1 : Mode0; }
    IO PortCHighIO() { return Bit(_control, 3) ? Input : Output; }
    IO PortAIO() { return Bit(_control, 4) ? Input : Output; }
    Mode GroupAMode();

    void Reset();

    byte Read(word addr);
    void Write(word addr, byte b);

    void WritePortC();

    friend StreamWriter& operator<<(StreamWriter& s, const PPI& ppi);
    friend StreamReader& operator>>(StreamReader& s, PPI& ppi);
};