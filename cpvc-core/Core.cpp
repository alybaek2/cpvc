#include "Core.h"

Core::Core() : _pBus(&_bus)
{
    Init();
}

Core::Core(IBus* pBus) : _pBus(pBus)
{
    Init();
}

Core::~Core()
{
}

void Core::Reset()
{
    AF = BC = DE = HL = IR = SP = PC = 0;
    AF_ = BC_ = DE_ = HL_ = 0;
    IX = IY = 0xFFFF;
    _iff1 = _iff2 = _interruptRequested = false;
    _interruptMode = 0;
    _eiDelay = 0;
    _halted = false;

    _memory.Reset();

    _gateArray.Reset();
    _psg.Reset();
    _ppi.Reset();

    _crtc.Reset();

    _fdc.Reset();
}

void Core::Init()
{
    Reset();

    _ticks = 0;

    _pScreen = nullptr;
    _scrPitch = 0;
    _scrHeight = 0;
    _scrWidth = 0;

    _fdc.Init();
    _tape.Eject();

    _audioTickTotal = 0;
    _audioTicksToNextSample = 0;
    _audioSampleCount = 0;
}

bool Core::KeyPress(byte keycode, bool down)
{
    return _keyboard.KeyPress((byte)(keycode % 10), (byte)(keycode / 10), down);
}

// Screen methods
void Core::SetScreen(byte* pBuffer, word pitch, word height, word width)
{
    _scrPitch = pitch;
    _scrWidth = width / 16;  // _scrWidth is in CRTC chars (16 pixels per char).
    _scrHeight = height;
    _pScreen = pBuffer;
}

int Core::GetAudioBuffers(int numSamples, byte* (&pChannels)[3])
{
    return _audio.GetBuffers(numSamples, pChannels);
}

void Core::SetFrequency(dword frequency)
{
    _frequency = frequency;
}

byte Core::RunUntil(qword stopTicks, byte stopReason)
{
    while (_ticks < stopTicks)
    {
        if ((stopReason & stopAudioOverrun) != 0 && _audio.Overrun())
        {
            return stopAudioOverrun;
        }

        bool vSyncBefore = _crtc._inVSync;
        Step(stopReason);

        if ((stopReason & stopVSync) != 0 && !vSyncBefore && _crtc._inVSync)
        {
            return stopVSync;
        }
    }

    return 0;
}

// Rom methods
void Core::EnableLowerROM(bool enabled)
{
    _memory.EnableLowerROM(enabled);
}

void Core::SetLowerRom(Mem16k& lowerRom)
{
    _memory.SetLowerROM(lowerRom);
}

void Core::EnableUpperROM(bool enabled)
{
    _memory.EnableUpperROM(enabled);
}

void Core::SetUpperRom(byte slot, Mem16k& rom)
{
    _memory.SetUpperROM(slot, rom);
}

// RAM methods
byte Core::ReadRAM(const word& addr)
{
    return _memory.Read(addr);
}

void Core::WriteRAM(const word& addr, byte b)
{
    _memory.Write(addr, b);
}

qword Core::Ticks()
{
    return _ticks;
}

void Core::TickToNextMs()
{
    Tick((4 - (_ticks % 4)) % 4);
}

void Core::Tick(byte ticks)
{
    byte usBoundaries = (ticks + (_ticks % 4)) / 4;
    NonCPUTick(usBoundaries);
    _ticks += ticks;
}

void Core::NonCPUTick(byte ticks)
{
    for (byte t = 0; t < ticks; t++)
    {
        VideoRender();
        AudioRender();

        _crtc.Tick();
        _psg.Tick();
        _fdc.Tick();
        _tape.Tick();
    }
}

void Core::VideoRender()
{
    if (_pScreen == nullptr)
    {
        return;
    }

    // Ensure the current x and y coordinates don't cause us to overrun the screen buffer.
    if (_crtc._x >= _scrWidth || _crtc._y >= _scrHeight)
    {
        return;
    }

    bool inScreen = (_crtc._hCount < _crtc._horizontalDisplayed) && (_crtc._vCount < _crtc._verticalDisplay);
    bool inSync = (_crtc._inHSync || _crtc._inVSync);
    bool inBorder = !inScreen && !inSync;

    dword offset = (_scrPitch * _crtc._y) + _crtc._x * 16;
    byte* pPixel = _pScreen + offset;

    if (inSync)
    {
        return;
    }

    if (inBorder)
    {
        memset(pPixel, _gateArray._border, 16);
    }
    else
    {
        word memAddr = _crtc._memoryAddress + _crtc._hCount;
        word addr = (word)
            (((memAddr & 0x3000) << 2) |
             ((_crtc._raster & 0x07) << 11) |
             ((memAddr & 0x03FF) << 1));

        byte (&pens)[256][8] = _gateArray._renderedPenBytes[_gateArray._mode];

        memcpy(pPixel, pens[_memory.VideoRead(addr)], 8);
        memcpy(pPixel + 8, pens[_memory.VideoRead(addr + 1)], 8);
    }
}

void Core::AudioRender()
{
    if (_audioTickTotal >= _audioTicksToNextSample)
    {
        _audioSampleCount++;
        if (_audioSampleCount >= _frequency)
        {
            _audioSampleCount = 0;
            _audioTickTotal = 0;
        }

        double num = (((double)_audioSampleCount) * ((double) 1000000.0));
        double denom = (double)_frequency;
        _audioTicksToNextSample = (dword)(num / denom);

        byte amps[3];
        _psg.Amplitudes(amps);

        if (_tape._motor && (_tape._level || _ppi._tapeWriteData))
        {
            // If the tape level for either reading or writing is high, set the amplitudes to the maximum (15).
            amps[0] = amps[1] = amps[2] = 15;
        }

        _audio.WriteSample(amps);
    }

    _audioTickTotal++;
}

byte Core::BusRead(const word& addr)
{
    return _pBus->Read(addr);
}

void Core::BusWrite(const word& addr, byte b)
{
    _pBus->Write(addr, b);
}

byte Core::MemReadRequest(word addr)
{
    TickToNextMs();

    return ReadRAM(addr);
}

void Core::MemWriteRequest(word addr, byte b)
{
    TickToNextMs();

    WriteRAM(addr, b);
}

byte Core::BusReadRequest(word addr)
{
    return BusRead(addr);
}

void Core::BusWriteRequest(word addr, byte b)
{
    BusWrite(addr, b);
}

void Core::LoadTape(const byte* pBuffer, int size)
{
    if (size == 0)
    {
        _tape.Eject();
        return;
    }

    bytevector buffer((byte*)pBuffer, (byte*)(pBuffer + size));
    _tape.Load(buffer);
}

void Core::LoadDisc(byte drive, const byte* pBuffer, int size)
{
    if (size == 0)
    {
        _fdc._drives[drive].Eject();
        return;
    }

    bytevector buffer((byte*)pBuffer, (byte*)(pBuffer + size));

    Disk disk;
    disk.LoadDisk(buffer.data(), (int) buffer.size());

    _fdc._drives[drive].Load(disk);
}

void Core::Step(byte stopReason)
{
    if (_eiDelay > 0)
    {
        _eiDelay--;
        if (_eiDelay == 0)
        {
            _iff1 = true;
            _iff2 = true;
        }
    }

    if (HandleInterrupt())
    {
        return;
    }

    byte op = MemReadRequest(PC++);
    IncrementR();
    Tick(4);

    Execute(op);
}

void Core::Execute(byte op)
{
    switch (op)
    {
    case 0x10:      DJNZ();            break;
    case 0xC3:      JP(true);          break;
    case 0xC2:      JP(!Zero());       break;
    case 0xCA:      JP(Zero());        break;
    case 0xD2:      JP(!Carry());      break;
    case 0xDA:      JP(Carry());       break;
    case 0xE2:      JP(!ParityOverflow());    break;
    case 0xEA:      JP(ParityOverflow());     break;
    case 0xF2:      JP(!Sign());       break;
    case 0xFA:      JP(Sign());        break;

    case 0xF3:      DI();              break;
    case 0xFB:      EI();              break;

    case 0x76:      HALT();            break;

    case 0x27:      DAA();             break;

    case 0x18:      JR(true);          break;
    case 0x20:      JR(!Zero());       break;
    case 0x28:      JR(Zero());        break;
    case 0x30:      JR(!Carry());      break;
    case 0x38:      JR(Carry());       break;

    case 0xE9:      JP(HL);            break;

    case 0x2F:      CPL();             break;
    case 0x37:      SCF();             break;
    case 0x3F:      CCF();             break;

    case 0x07:      RLCA();            break;
    case 0x0F:      RRCA();            break;
    case 0x17:      RLA();             break;
    case 0x1F:      RRA();             break;

    case 0x80:      ADDr(B, false);    break;
    case 0x81:      ADDr(C, false);    break;
    case 0x82:      ADDr(D, false);    break;
    case 0x83:      ADDr(E, false);    break;
    case 0x84:      ADDr(H, false);    break;
    case 0x85:      ADDr(L, false);    break;
    case 0x86:      ADDHLInd(false);   break;
    case 0x87:      ADDr(A, false);    break;
    case 0xC6:      ADDn(false);       break;

    case 0x88:      ADDr(B, true);     break;
    case 0x89:      ADDr(C, true);     break;
    case 0x8A:      ADDr(D, true);     break;
    case 0x8B:      ADDr(E, true);     break;
    case 0x8C:      ADDr(H, true);     break;
    case 0x8D:      ADDr(L, true);     break;
    case 0x8E:      ADDHLInd(true);    break;
    case 0x8F:      ADDr(A, true);     break;
    case 0xCE:      ADDn(true);        break;

    case 0x90:      SUBr(B, false);    break;
    case 0x91:      SUBr(C, false);    break;
    case 0x92:      SUBr(D, false);    break;
    case 0x93:      SUBr(E, false);    break;
    case 0x94:      SUBr(H, false);    break;
    case 0x95:      SUBr(L, false);    break;
    case 0x96:      SUBHLInd(false);   break;
    case 0x97:      SUBr(A, false);    break;
    case 0xD6:      SUBn(false);       break;

    case 0x98:      SUBr(B, true);     break;
    case 0x99:      SUBr(C, true);     break;
    case 0x9A:      SUBr(D, true);     break;
    case 0x9B:      SUBr(E, true);     break;
    case 0x9C:      SUBr(H, true);     break;
    case 0x9D:      SUBr(L, true);     break;
    case 0x9E:      SUBHLInd(true);    break;
    case 0x9F:      SUBr(A, true);     break;
    case 0xDE:      SUBn(true);        break;

    case 0x06:      LDrn(B);           break;
    case 0x0E:      LDrn(C);           break;
    case 0x16:      LDrn(D);           break;
    case 0x1E:      LDrn(E);           break;
    case 0x26:      LDrn(H);           break;
    case 0x2E:      LDrn(L);           break;
    case 0x3E:      LDrn(A);           break;

    case 0x40:      LDrr(B, B);        break;
    case 0x41:      LDrr(B, C);        break;
    case 0x42:      LDrr(B, D);        break;
    case 0x43:      LDrr(B, E);        break;
    case 0x44:      LDrr(B, H);        break;
    case 0x45:      LDrr(B, L);        break;
    case 0x47:      LDrr(B, A);        break;
    case 0x48:      LDrr(C, B);        break;
    case 0x49:      LDrr(C, C);        break;
    case 0x4A:      LDrr(C, D);        break;
    case 0x4B:      LDrr(C, E);        break;
    case 0x4C:      LDrr(C, H);        break;
    case 0x4D:      LDrr(C, L);        break;
    case 0x4F:      LDrr(C, A);        break;
    case 0x50:      LDrr(D, B);        break;
    case 0x51:      LDrr(D, C);        break;
    case 0x52:      LDrr(D, D);        break;
    case 0x53:      LDrr(D, E);        break;
    case 0x54:      LDrr(D, H);        break;
    case 0x55:      LDrr(D, L);        break;
    case 0x57:      LDrr(D, A);        break;
    case 0x58:      LDrr(E, B);        break;
    case 0x59:      LDrr(E, C);        break;
    case 0x5A:      LDrr(E, D);        break;
    case 0x5B:      LDrr(E, E);        break;
    case 0x5C:      LDrr(E, H);        break;
    case 0x5D:      LDrr(E, L);        break;
    case 0x5F:      LDrr(E, A);        break;
    case 0x60:      LDrr(H, B);        break;
    case 0x61:      LDrr(H, C);        break;
    case 0x62:      LDrr(H, D);        break;
    case 0x63:      LDrr(H, E);        break;
    case 0x64:      LDrr(H, H);        break;
    case 0x65:      LDrr(H, L);        break;
    case 0x67:      LDrr(H, A);        break;
    case 0x68:      LDrr(L, B);        break;
    case 0x69:      LDrr(L, C);        break;
    case 0x6A:      LDrr(L, D);        break;
    case 0x6B:      LDrr(L, E);        break;
    case 0x6C:      LDrr(L, H);        break;
    case 0x6D:      LDrr(L, L);        break;
    case 0x6F:      LDrr(L, A);        break;
    case 0x78:      LDrr(A, B);        break;
    case 0x79:      LDrr(A, C);        break;
    case 0x7A:      LDrr(A, D);        break;
    case 0x7B:      LDrr(A, E);        break;
    case 0x7C:      LDrr(A, H);        break;
    case 0x7D:      LDrr(A, L);        break;
    case 0x7F:      LDrr(A, A);        break;

    case 0x02:      LDrrA(BC, A);      break;
    case 0x12:      LDrrA(DE, A);      break;

    case 0x0A:      LDArr(BC);         break;
    case 0x1A:      LDArr(DE);         break;

    case 0x22:      LDnnHL(HL);        break;
    case 0x2A:      LDHLnnInd(HL);     break;

    case 0x70:      LDHLr(B);          break;
    case 0x71:      LDHLr(C);          break;
    case 0x72:      LDHLr(D);          break;
    case 0x73:      LDHLr(E);          break;
    case 0x74:      LDHLr(H);          break;
    case 0x75:      LDHLr(L);          break;
    case 0x77:      LDHLr(A);          break;

    case 0x46:      LDrHL(B);          break;
    case 0x4E:      LDrHL(C);          break;
    case 0x56:      LDrHL(D);          break;
    case 0x5E:      LDrHL(E);          break;
    case 0x66:      LDrHL(H);          break;
    case 0x6E:      LDrHL(L);          break;
    case 0x7E:      LDrHL(A);          break;

    case 0x32:      LDnnA();           break;
    case 0x3A:      LDAnn();           break;
    case 0x36:      LDHLn();           break;

    case 0x09:      ADDHLdd(BC);       break;
    case 0x19:      ADDHLdd(DE);       break;
    case 0x29:      ADDHLdd(HL);       break;
    case 0x39:      ADDHLdd(SP);       break;

    case 0x01:      LDddnn(BC);        break;
    case 0x11:      LDddnn(DE);        break;
    case 0x21:      LDddnn(HL);        break;
    case 0x31:      LDddnn(SP);        break;

    case 0xF9:      LDSPHL(HL);        break;

    case 0xCD:      CALL(true);        break;
    case 0xC4:      CALL(!Zero());     break;
    case 0xCC:      CALL(Zero());      break;
    case 0xD4:      CALL(!Carry());    break;
    case 0xDC:      CALL(Carry());     break;
    case 0xE4:      CALL(!ParityOverflow());  break;
    case 0xEC:      CALL(ParityOverflow());   break;
    case 0xF4:      CALL(!Sign());     break;
    case 0xFC:      CALL(Sign());      break;

    case 0xD9:      EXX();             break;
    case 0x08:      EX(AF, AF_);       break;
    case 0xEB:      EX(DE, HL);        break;
    case 0xE3:      EXSPrr(HL);        break;

    case 0xC7:      RST(0x0000);       break;
    case 0xCF:      RST(0x0008);       break;
    case 0xD7:      RST(0x0010);       break;
    case 0xDF:      RST(0x0018);       break;
    case 0xE7:      RST(0x0020);       break;
    case 0xEF:      RST(0x0028);       break;
    case 0xF7:      RST(0x0030);       break;
    case 0xFF:      RST(0x0038);       break;

    case 0xC1:      POP(BC);           break;
    case 0xD1:      POP(DE);           break;
    case 0xE1:      POP(HL);           break;
    case 0xF1:      POP(AF);           break;

    case 0xC9:      RET();             break;
    case 0xC0:      RETcc(!Zero());    break;
    case 0xC8:      RETcc(Zero());     break;
    case 0xD0:      RETcc(!Carry());   break;
    case 0xD8:      RETcc(Carry());    break;
    case 0xE0:      RETcc(!ParityOverflow()); break;
    case 0xE8:      RETcc(ParityOverflow());  break;
    case 0xF0:      RETcc(!Sign());    break;
    case 0xF8:      RETcc(Sign());     break;

    case 0xD3:      OUTnA();           break;
    case 0xDB:      INAn();            break;

    case 0xC5:      PUSH(BC);          break;
    case 0xD5:      PUSH(DE);          break;
    case 0xE5:      PUSH(HL);          break;
    case 0xF5:      PUSH(AF);          break;

    case 0x04:      INCr(B);           break;
    case 0x0C:      INCr(C);           break;
    case 0x14:      INCr(D);           break;
    case 0x1C:      INCr(E);           break;
    case 0x24:      INCr(H);           break;
    case 0x2C:      INCr(L);           break;
    case 0x34:      INCHLInd();        break;
    case 0x3C:      INCr(A);           break;

    case 0x05:      DECr(B);           break;
    case 0x0D:      DECr(C);           break;
    case 0x15:      DECr(D);           break;
    case 0x1D:      DECr(E);           break;
    case 0x25:      DECr(H);           break;
    case 0x2D:      DECr(L);           break;
    case 0x35:      DECHLInd();        break;
    case 0x3D:      DECr(A);           break;


    case 0x03:      INCrr(BC);         break;
    case 0x13:      INCrr(DE);         break;
    case 0x23:      INCrr(HL);         break;
    case 0x33:      INCrr(SP);         break;

    case 0x0B:      DECrr(BC);         break;
    case 0x1B:      DECrr(DE);         break;
    case 0x2B:      DECrr(HL);         break;
    case 0x3B:      DECrr(SP);         break;

    case 0xA0:      ANDr(B);           break;
    case 0xA1:      ANDr(C);           break;
    case 0xA2:      ANDr(D);           break;
    case 0xA3:      ANDr(E);           break;
    case 0xA4:      ANDr(H);           break;
    case 0xA5:      ANDr(L);           break;
    case 0xA6:      ANDHLInd();        break;
    case 0xA7:      ANDr(A);           break;
    case 0xE6:      ANDn();            break;

    case 0xA8:      XORr(B);           break;
    case 0xA9:      XORr(C);           break;
    case 0xAA:      XORr(D);           break;
    case 0xAB:      XORr(E);           break;
    case 0xAC:      XORr(H);           break;
    case 0xAD:      XORr(L);           break;
    case 0xAE:      XORHLInd();        break;
    case 0xAF:      XORr(A);           break;
    case 0xEE:      XORn();            break;

    case 0xB0:      ORr(B);            break;
    case 0xB1:      ORr(C);            break;
    case 0xB2:      ORr(D);            break;
    case 0xB3:      ORr(E);            break;
    case 0xB4:      ORr(H);            break;
    case 0xB5:      ORr(L);            break;
    case 0xB6:      ORHLInd();         break;
    case 0xB7:      ORr(A);            break;
    case 0xF6:      ORn();             break;

    case 0xB8:      CPr(B);            break;
    case 0xB9:      CPr(C);            break;
    case 0xBA:      CPr(D);            break;
    case 0xBB:      CPr(E);            break;
    case 0xBC:      CPr(H);            break;
    case 0xBD:      CPr(L);            break;
    case 0xBE:      CPHLInd();         break;
    case 0xBF:      CPr(A);            break;
    case 0xFE:      CPn();             break;

    case 0xCB:      ExecuteCB();       break;
    case 0xED:      ExecuteED();       break;
    case 0xDD:      ExecuteDDFD(IX);   break;
    case 0xFD:      ExecuteDDFD(IY);   break;
    }
}

void Core::ExecuteCB()
{
    // M2
    byte op = _memory.Read(PC++);
    IncrementR();
    Tick(4);

    switch (op)
    {
    case 0x00:      RLCr(B);         break;
    case 0x01:      RLCr(C);         break;
    case 0x02:      RLCr(D);         break;
    case 0x03:      RLCr(E);         break;
    case 0x04:      RLCr(H);         break;
    case 0x05:      RLCr(L);         break;
    case 0x06:      RLCHLInd(HL);    break;
    case 0x07:      RLCr(A);         break;

    case 0x08:      RRCr(B);         break;
    case 0x09:      RRCr(C);         break;
    case 0x0A:      RRCr(D);         break;
    case 0x0B:      RRCr(E);         break;
    case 0x0C:      RRCr(H);         break;
    case 0x0D:      RRCr(L);         break;
    case 0x0E:      RRCHLInd(HL);    break;
    case 0x0F:      RRCr(A);         break;

    case 0x10:      RLr(B);          break;
    case 0x11:      RLr(C);          break;
    case 0x12:      RLr(D);          break;
    case 0x13:      RLr(E);          break;
    case 0x14:      RLr(H);          break;
    case 0x15:      RLr(L);          break;
    case 0x16:      RLHLInd(HL);     break;
    case 0x17:      RLr(A);          break;

    case 0x18:      RRr(B);          break;
    case 0x19:      RRr(C);          break;
    case 0x1A:      RRr(D);          break;
    case 0x1B:      RRr(E);          break;
    case 0x1C:      RRr(H);          break;
    case 0x1D:      RRr(L);          break;
    case 0x1E:      RRHLInd(HL);     break;
    case 0x1F:      RRr(A);          break;

    case 0x20:      SLAr(B);         break;
    case 0x21:      SLAr(C);         break;
    case 0x22:      SLAr(D);         break;
    case 0x23:      SLAr(E);         break;
    case 0x24:      SLAr(H);         break;
    case 0x25:      SLAr(L);         break;
    case 0x26:      SLAHLInd();      break;
    case 0x27:      SLAr(A);         break;

    case 0x28:      SRAr(B);         break;
    case 0x29:      SRAr(C);         break;
    case 0x2A:      SRAr(D);         break;
    case 0x2B:      SRAr(E);         break;
    case 0x2C:      SRAr(H);         break;
    case 0x2D:      SRAr(L);         break;
    case 0x2E:      SRAHLInd();      break;
    case 0x2F:      SRAr(A);         break;

    case 0x30:      SLLr(B);         break;
    case 0x31:      SLLr(C);         break;
    case 0x32:      SLLr(D);         break;
    case 0x33:      SLLr(E);         break;
    case 0x34:      SLLr(H);         break;
    case 0x35:      SLLr(L);         break;
    case 0x36:      SLLHLInd();      break;
    case 0x37:      SLLr(A);         break;

    case 0x38:      SRLr(B);         break;
    case 0x39:      SRLr(C);         break;
    case 0x3A:      SRLr(D);         break;
    case 0x3B:      SRLr(E);         break;
    case 0x3C:      SRLr(H);         break;
    case 0x3D:      SRLr(L);         break;
    case 0x3E:      SRLHLInd();      break;
    case 0x3F:      SRLr(A);         break;

    case 0x40:      BITbr(0, B);     break;
    case 0x41:      BITbr(0, C);     break;
    case 0x42:      BITbr(0, D);     break;
    case 0x43:      BITbr(0, E);     break;
    case 0x44:      BITbr(0, H);     break;
    case 0x45:      BITbr(0, L);     break;
    case 0x46:      BITbHLInd(0);    break;
    case 0x47:      BITbr(0, A);     break;

    case 0x48:      BITbr(1, B);     break;
    case 0x49:      BITbr(1, C);     break;
    case 0x4A:      BITbr(1, D);     break;
    case 0x4B:      BITbr(1, E);     break;
    case 0x4C:      BITbr(1, H);     break;
    case 0x4D:      BITbr(1, L);     break;
    case 0x4E:      BITbHLInd(1);    break;
    case 0x4F:      BITbr(1, A);     break;

    case 0x50:      BITbr(2, B);     break;
    case 0x51:      BITbr(2, C);     break;
    case 0x52:      BITbr(2, D);     break;
    case 0x53:      BITbr(2, E);     break;
    case 0x54:      BITbr(2, H);     break;
    case 0x55:      BITbr(2, L);     break;
    case 0x56:      BITbHLInd(2);    break;
    case 0x57:      BITbr(2, A);     break;

    case 0x58:      BITbr(3, B);     break;
    case 0x59:      BITbr(3, C);     break;
    case 0x5A:      BITbr(3, D);     break;
    case 0x5B:      BITbr(3, E);     break;
    case 0x5C:      BITbr(3, H);     break;
    case 0x5D:      BITbr(3, L);     break;
    case 0x5E:      BITbHLInd(3);    break;
    case 0x5F:      BITbr(3, A);     break;

    case 0x60:      BITbr(4, B);     break;
    case 0x61:      BITbr(4, C);     break;
    case 0x62:      BITbr(4, D);     break;
    case 0x63:      BITbr(4, E);     break;
    case 0x64:      BITbr(4, H);     break;
    case 0x65:      BITbr(4, L);     break;
    case 0x66:      BITbHLInd(4);    break;
    case 0x67:      BITbr(4, A);     break;

    case 0x68:      BITbr(5, B);     break;
    case 0x69:      BITbr(5, C);     break;
    case 0x6A:      BITbr(5, D);     break;
    case 0x6B:      BITbr(5, E);     break;
    case 0x6C:      BITbr(5, H);     break;
    case 0x6D:      BITbr(5, L);     break;
    case 0x6E:      BITbHLInd(5);    break;
    case 0x6F:      BITbr(5, A);     break;

    case 0x70:      BITbr(6, B);     break;
    case 0x71:      BITbr(6, C);     break;
    case 0x72:      BITbr(6, D);     break;
    case 0x73:      BITbr(6, E);     break;
    case 0x74:      BITbr(6, H);     break;
    case 0x75:      BITbr(6, L);     break;
    case 0x76:      BITbHLInd(6);    break;
    case 0x77:      BITbr(6, A);     break;

    case 0x78:      BITbr(7, B);     break;
    case 0x79:      BITbr(7, C);     break;
    case 0x7A:      BITbr(7, D);     break;
    case 0x7B:      BITbr(7, E);     break;
    case 0x7C:      BITbr(7, H);     break;
    case 0x7D:      BITbr(7, L);     break;
    case 0x7E:      BITbHLInd(7);    break;
    case 0x7F:      BITbr(7, A);     break;

    case 0x80:      RESbr(0, B);     break;
    case 0x81:      RESbr(0, C);     break;
    case 0x82:      RESbr(0, D);     break;
    case 0x83:      RESbr(0, E);     break;
    case 0x84:      RESbr(0, H);     break;
    case 0x85:      RESbr(0, L);     break;
    case 0x86:      RESbHLInd(0);    break;
    case 0x87:      RESbr(0, A);     break;

    case 0x88:      RESbr(1, B);     break;
    case 0x89:      RESbr(1, C);     break;
    case 0x8A:      RESbr(1, D);     break;
    case 0x8B:      RESbr(1, E);     break;
    case 0x8C:      RESbr(1, H);     break;
    case 0x8D:      RESbr(1, L);     break;
    case 0x8E:      RESbHLInd(1);    break;
    case 0x8F:      RESbr(1, A);     break;

    case 0x90:      RESbr(2, B);     break;
    case 0x91:      RESbr(2, C);     break;
    case 0x92:      RESbr(2, D);     break;
    case 0x93:      RESbr(2, E);     break;
    case 0x94:      RESbr(2, H);     break;
    case 0x95:      RESbr(2, L);     break;
    case 0x96:      RESbHLInd(2);    break;
    case 0x97:      RESbr(2, A);     break;

    case 0x98:      RESbr(3, B);     break;
    case 0x99:      RESbr(3, C);     break;
    case 0x9A:      RESbr(3, D);     break;
    case 0x9B:      RESbr(3, E);     break;
    case 0x9C:      RESbr(3, H);     break;
    case 0x9D:      RESbr(3, L);     break;
    case 0x9E:      RESbHLInd(3);    break;
    case 0x9F:      RESbr(3, A);     break;

    case 0xA0:      RESbr(4, B);     break;
    case 0xA1:      RESbr(4, C);     break;
    case 0xA2:      RESbr(4, D);     break;
    case 0xA3:      RESbr(4, E);     break;
    case 0xA4:      RESbr(4, H);     break;
    case 0xA5:      RESbr(4, L);     break;
    case 0xA6:      RESbHLInd(4);    break;
    case 0xA7:      RESbr(4, A);     break;

    case 0xA8:      RESbr(5, B);     break;
    case 0xA9:      RESbr(5, C);     break;
    case 0xAA:      RESbr(5, D);     break;
    case 0xAB:      RESbr(5, E);     break;
    case 0xAC:      RESbr(5, H);     break;
    case 0xAD:      RESbr(5, L);     break;
    case 0xAE:      RESbHLInd(5);    break;
    case 0xAF:      RESbr(5, A);     break;

    case 0xB0:      RESbr(6, B);     break;
    case 0xB1:      RESbr(6, C);     break;
    case 0xB2:      RESbr(6, D);     break;
    case 0xB3:      RESbr(6, E);     break;
    case 0xB4:      RESbr(6, H);     break;
    case 0xB5:      RESbr(6, L);     break;
    case 0xB6:      RESbHLInd(6);    break;
    case 0xB7:      RESbr(6, A);     break;

    case 0xB8:      RESbr(7, B);     break;
    case 0xB9:      RESbr(7, C);     break;
    case 0xBA:      RESbr(7, D);     break;
    case 0xBB:      RESbr(7, E);     break;
    case 0xBC:      RESbr(7, H);     break;
    case 0xBD:      RESbr(7, L);     break;
    case 0xBE:      RESbHLInd(7);    break;
    case 0xBF:      RESbr(7, A);     break;

    case 0xC0:      SETbr(0, B);     break;
    case 0xC1:      SETbr(0, C);     break;
    case 0xC2:      SETbr(0, D);     break;
    case 0xC3:      SETbr(0, E);     break;
    case 0xC4:      SETbr(0, H);     break;
    case 0xC5:      SETbr(0, L);     break;
    case 0xC6:      SETbHLInd(0);    break;
    case 0xC7:      SETbr(0, A);     break;

    case 0xC8:      SETbr(1, B);     break;
    case 0xC9:      SETbr(1, C);     break;
    case 0xCA:      SETbr(1, D);     break;
    case 0xCB:      SETbr(1, E);     break;
    case 0xCC:      SETbr(1, H);     break;
    case 0xCD:      SETbr(1, L);     break;
    case 0xCE:      SETbHLInd(1);    break;
    case 0xCF:      SETbr(1, A);     break;

    case 0xD0:      SETbr(2, B);     break;
    case 0xD1:      SETbr(2, C);     break;
    case 0xD2:      SETbr(2, D);     break;
    case 0xD3:      SETbr(2, E);     break;
    case 0xD4:      SETbr(2, H);     break;
    case 0xD5:      SETbr(2, L);     break;
    case 0xD6:      SETbHLInd(2);    break;
    case 0xD7:      SETbr(2, A);     break;

    case 0xD8:      SETbr(3, B);     break;
    case 0xD9:      SETbr(3, C);     break;
    case 0xDA:      SETbr(3, D);     break;
    case 0xDB:      SETbr(3, E);     break;
    case 0xDC:      SETbr(3, H);     break;
    case 0xDD:      SETbr(3, L);     break;
    case 0xDE:      SETbHLInd(3);    break;
    case 0xDF:      SETbr(3, A);     break;

    case 0xE0:      SETbr(4, B);     break;
    case 0xE1:      SETbr(4, C);     break;
    case 0xE2:      SETbr(4, D);     break;
    case 0xE3:      SETbr(4, E);     break;
    case 0xE4:      SETbr(4, H);     break;
    case 0xE5:      SETbr(4, L);     break;
    case 0xE6:      SETbHLInd(4);    break;
    case 0xE7:      SETbr(4, A);     break;

    case 0xE8:      SETbr(5, B);     break;
    case 0xE9:      SETbr(5, C);     break;
    case 0xEA:      SETbr(5, D);     break;
    case 0xEB:      SETbr(5, E);     break;
    case 0xEC:      SETbr(5, H);     break;
    case 0xED:      SETbr(5, L);     break;
    case 0xEE:      SETbHLInd(5);    break;
    case 0xEF:      SETbr(5, A);     break;

    case 0xF0:      SETbr(6, B);     break;
    case 0xF1:      SETbr(6, C);     break;
    case 0xF2:      SETbr(6, D);     break;
    case 0xF3:      SETbr(6, E);     break;
    case 0xF4:      SETbr(6, H);     break;
    case 0xF5:      SETbr(6, L);     break;
    case 0xF6:      SETbHLInd(6);    break;
    case 0xF7:      SETbr(6, A);     break;

    case 0xF8:      SETbr(7, B);     break;
    case 0xF9:      SETbr(7, C);     break;
    case 0xFA:      SETbr(7, D);     break;
    case 0xFB:      SETbr(7, E);     break;
    case 0xFC:      SETbr(7, H);     break;
    case 0xFD:      SETbr(7, L);     break;
    case 0xFE:      SETbHLInd(7);    break;
    case 0xFF:      SETbr(7, A);     break;
    }
}

void Core::ExecuteED()
{
    // M2
    byte op = _memory.Read(PC++);
    IncrementR();
    Tick(4);

    byte junk;

    switch (op)
    {
    case 0x43:       LDnndd(BC);             break;
    case 0x53:       LDnndd(DE);             break;
    case 0x63:       LDnndd(HL);             break;
    case 0x73:       LDnndd(SP);             break;

    case 0x4B:       LDddnnInd(BC);          break;
    case 0x5B:       LDddnnInd(DE);          break;
    case 0x6B:       LDddnnInd(HL);          break;
    case 0x7B:       LDddnnInd(SP);          break;

    case 0x47:       LDIRA(I);               break;
    case 0x57:       LDAIR(I);               break;
    case 0x4F:       LDIRA(R);               break;
    case 0x5F:       LDAIR(R);               break;

    case 0x67:       RRD();                  break;
    case 0x6F:       RLD();                  break;

    case 0xA9:       CPBlock(false, false);  break;
    case 0xA1:       CPBlock(false, true);   break;
    case 0xB9:       CPBlock(true, false);   break;
    case 0xB1:       CPBlock(true, true);    break;

    case 0xA8:       LDBlock(false, false);  break;
    case 0xA0:       LDBlock(true, false);   break;
    case 0xB8:       LDBlock(false, true);   break;
    case 0xB0:       LDBlock(true, true);    break;

    case 0xAA:       INBlock(false, false);  break;
    case 0xBA:       INBlock(false, true);   break;
    case 0xA2:       INBlock(true, false);   break;
    case 0xB2:       INBlock(true, true);    break;

    case 0xAB:       OUTBlock(false, false); break;
    case 0xBB:       OUTBlock(false, true);  break;
    case 0xA3:       OUTBlock(true, false);  break;
    case 0xB3:       OUTBlock(true, true);   break;

    case 0x42:       SBCHLdd(BC);            break;
    case 0x52:       SBCHLdd(DE);            break;
    case 0x62:       SBCHLdd(HL);            break;
    case 0x72:       SBCHLdd(SP);            break;

    case 0x4A:       ADCHLdd(BC);            break;
    case 0x5A:       ADCHLdd(DE);            break;
    case 0x6A:       ADCHLdd(HL);            break;
    case 0x7A:       ADCHLdd(SP);            break;

    case 0x46:
    case 0x4E:       IM(0);                  break;
    case 0x56:       IM(1);                  break;
    case 0x5E:       IM(2);                  break;

    case 0x44:
    case 0x4C:
    case 0x54:
    case 0x5C:
    case 0x64:
    case 0x6C:
    case 0x74:
    case 0x7C:       NEG();                  break;

    case 0x45:
    case 0x55:
    case 0x5D:
    case 0x65:
    case 0x6D:
    case 0x75:
    case 0x7D:       RETN();                 break;

    case 0x4D:       RETI();                 break;

    case 0x40:       INrC(B);                break;
    case 0x48:       INrC(C);                break;
    case 0x50:       INrC(D);                break;
    case 0x58:       INrC(E);                break;
    case 0x60:       INrC(H);                break;
    case 0x68:       INrC(L);                break;
    case 0x70:       INrC(junk);             break;
    case 0x78:       INrC(A);                break;

    case 0x41:       OUTCr(B);               break;
    case 0x49:       OUTCr(C);               break;
    case 0x51:       OUTCr(D);               break;
    case 0x59:       OUTCr(E);               break;
    case 0x61:       OUTCr(H);               break;
    case 0x69:       OUTCr(L);               break;
    case 0x71:       OUTCr(0);               break;
    case 0x79:       OUTCr(A);               break;
    }
}

void Core::ExecuteDDFD(word& xy)
{
    byte op = MemReadRequest(PC++);
    IncrementR();
    Tick(4);

    switch (op)
    {
    case 0x21:       LDxynn(xy);                  break;
    case 0x23:       INCrr(xy);                   break;
    case 0x2B:       DECrr(xy);                   break;
    case 0x36:       LDxyn(xy);                   break;
    case 0xE3:       EXSPrr(xy);                  break;
    case 0xF9:       LDSPHL(xy);                  break;
    case 0xB6:       ORIndex(xy);                 break;
    case 0xA6:       ANDIndex(xy);                break;
    case 0xAE:       XORIndex(xy);                break;
    case 0xBE:       CPIndex(xy);                 break;
    case 0x86:       ADDIndex(xy, false);         break;
    case 0x8E:       ADDIndex(xy, true);          break;
    case 0x96:       SUBIndex(xy, false);         break;
    case 0x9E:       SUBIndex(xy, true);          break;

    case 0xE9:       JP(xy);                      break;

    case 0x22:       LDnnHL(xy);                  break;
    case 0x2A:       LDHLnnInd(xy);               break;

    case 0x34:       INCIndex(xy);                break;
    case 0x35:       DECIndex(xy);                break;

    case 0x46:       LDrxy(xy, B);                break;
    case 0x4E:       LDrxy(xy, C);                break;
    case 0x56:       LDrxy(xy, D);                break;
    case 0x5E:       LDrxy(xy, E);                break;
    case 0x66:       LDrxy(xy, H);                break;
    case 0x6E:       LDrxy(xy, L);                break;
    case 0x7E:       LDrxy(xy, A);                break;

    case 0x70:       LDxyr(xy, B);                break;
    case 0x71:       LDxyr(xy, C);                break;
    case 0x72:       LDxyr(xy, D);                break;
    case 0x73:       LDxyr(xy, E);                break;
    case 0x74:       LDxyr(xy, H);                break;
    case 0x75:       LDxyr(xy, L);                break;
    case 0x77:       LDxyr(xy, A);                break;

    case 0x09:       ADDxyrr(xy, BC);             break;
    case 0x19:       ADDxyrr(xy, DE);             break;
    case 0x29:       ADDxyrr(xy, xy);             break;
    case 0x39:       ADDxyrr(xy, SP);             break;

    case 0xE1:       POP(xy);                     break;
    case 0xE5:       PUSH(xy);                    break;

    case 0x84:       ADDr(High(xy), false);       break;
    case 0x85:       ADDr(Low(xy), false);        break;

    case 0x8C:       ADDr(High(xy), true);        break;
    case 0x8D:       ADDr(Low(xy), true);         break;

    case 0x94:       SUBr(High(xy), false);       break;
    case 0x95:       SUBr(Low(xy), false);        break;

    case 0x9C:       SUBr(High(xy), true);        break;
    case 0x9D:       SUBr(Low(xy), true);         break;

    case 0x26:       LDrn(High(xy));              break;
    case 0x2E:       LDrn(Low(xy));               break;

    case 0x44:       LDrr(B, High(xy));           break;
    case 0x45:       LDrr(B, Low(xy));            break;
    case 0x4C:       LDrr(C, High(xy));           break;
    case 0x4D:       LDrr(C, Low(xy));            break;
    case 0x54:       LDrr(D, High(xy));           break;
    case 0x55:       LDrr(D, Low(xy));            break;
    case 0x5C:       LDrr(E, High(xy));           break;
    case 0x5D:       LDrr(E, Low(xy));            break;
    case 0x60:       LDrr(High(xy), B);           break;
    case 0x61:       LDrr(High(xy), C);           break;
    case 0x62:       LDrr(High(xy), D);           break;
    case 0x63:       LDrr(High(xy), E);           break;
    case 0x64:       LDrr(High(xy), High(xy));    break;
    case 0x65:       LDrr(High(xy), Low(xy));     break;
    case 0x67:       LDrr(High(xy), A);           break;
    case 0x68:       LDrr(Low(xy), B);            break;
    case 0x69:       LDrr(Low(xy), C);            break;
    case 0x6A:       LDrr(Low(xy), D);            break;
    case 0x6B:       LDrr(Low(xy), E);            break;
    case 0x6C:       LDrr(Low(xy), High(xy));     break;
    case 0x6D:       LDrr(Low(xy), Low(xy));      break;
    case 0x6F:       LDrr(Low(xy), A);            break;

    case 0x7C:       LDrr(A, High(xy));           break;
    case 0x7D:       LDrr(A, Low(xy));            break;

    case 0x24:       INCr(High(xy));              break;
    case 0x2C:       INCr(Low(xy));               break;

    case 0x25:       DECr(High(xy));              break;
    case 0x2D:       DECr(Low(xy));               break;

    case 0xA4:       ANDr(High(xy));              break;
    case 0xA5:       ANDr(Low(xy));               break;

    case 0xAC:       XORr(High(xy));              break;
    case 0xAD:       XORr(Low(xy));               break;

    case 0xB4:       ORr(High(xy));               break;
    case 0xB5:       ORr(Low(xy));                break;

    case 0xBC:       CPr(High(xy));               break;
    case 0xBD:       CPr(Low(xy));                break;

    case 0xCB:       ExecuteDDFDCB(xy);           break;

    default:         Execute(op);                 break;
    }
}

void Core::ExecuteDDFDCB(word& xy)
{
    // M3
    offset o = (offset)MemReadRequest(PC++);
    Tick(3);

    // M4
    word addr = xy + o;
    Tick(2);

    byte op = MemReadRequest(PC++);
    Tick(3);

    byte junk;

    switch (op)
    {
    case 0x00:      RLCIndex(addr, B);         break;
    case 0x01:      RLCIndex(addr, C);         break;
    case 0x02:      RLCIndex(addr, D);         break;
    case 0x03:      RLCIndex(addr, E);         break;
    case 0x04:      RLCIndex(addr, H);         break;
    case 0x05:      RLCIndex(addr, L);         break;
    case 0x06:      RLCIndex(addr, junk);      break;
    case 0x07:      RLCIndex(addr, A);         break;

    case 0x08:      RRCIndex(addr, B);         break;
    case 0x09:      RRCIndex(addr, C);         break;
    case 0x0A:      RRCIndex(addr, D);         break;
    case 0x0B:      RRCIndex(addr, E);         break;
    case 0x0C:      RRCIndex(addr, H);         break;
    case 0x0D:      RRCIndex(addr, L);         break;
    case 0x0E:      RRCIndex(addr, junk);      break;
    case 0x0F:      RRCIndex(addr, A);         break;


    case 0x10:      RLIndex(addr, B);          break;
    case 0x11:      RLIndex(addr, C);          break;
    case 0x12:      RLIndex(addr, D);          break;
    case 0x13:      RLIndex(addr, E);          break;
    case 0x14:      RLIndex(addr, H);          break;
    case 0x15:      RLIndex(addr, L);          break;
    case 0x16:      RLIndex(addr, junk);       break;
    case 0x17:      RLIndex(addr, A);          break;

    case 0x18:      RRIndex(addr, B);          break;
    case 0x19:      RRIndex(addr, C);          break;
    case 0x1A:      RRIndex(addr, D);          break;
    case 0x1B:      RRIndex(addr, E);          break;
    case 0x1C:      RRIndex(addr, H);          break;
    case 0x1D:      RRIndex(addr, L);          break;
    case 0x1E:      RRIndex(addr, junk);       break;
    case 0x1F:      RRIndex(addr, A);          break;

    case 0x20:      SLAIndex(addr, B);         break;
    case 0x21:      SLAIndex(addr, C);         break;
    case 0x22:      SLAIndex(addr, D);         break;
    case 0x23:      SLAIndex(addr, E);         break;
    case 0x24:      SLAIndex(addr, H);         break;
    case 0x25:      SLAIndex(addr, L);         break;
    case 0x26:      SLAIndex(addr, junk);      break;
    case 0x27:      SLAIndex(addr, A);         break;

    case 0x28:      SRAIndex(addr, B);         break;
    case 0x29:      SRAIndex(addr, C);         break;
    case 0x2A:      SRAIndex(addr, D);         break;
    case 0x2B:      SRAIndex(addr, E);         break;
    case 0x2C:      SRAIndex(addr, H);         break;
    case 0x2D:      SRAIndex(addr, L);         break;
    case 0x2E:      SRAIndex(addr, junk);      break;
    case 0x2F:      SRAIndex(addr, A);         break;

    case 0x30:      SLLIndex(addr, B);         break;
    case 0x31:      SLLIndex(addr, C);         break;
    case 0x32:      SLLIndex(addr, D);         break;
    case 0x33:      SLLIndex(addr, E);         break;
    case 0x34:      SLLIndex(addr, H);         break;
    case 0x35:      SLLIndex(addr, L);         break;
    case 0x36:      SLLIndex(addr, junk);      break;
    case 0x37:      SLLIndex(addr, A);         break;

    case 0x38:      SRLIndex(addr, B);         break;
    case 0x39:      SRLIndex(addr, C);         break;
    case 0x3A:      SRLIndex(addr, D);         break;
    case 0x3B:      SRLIndex(addr, E);         break;
    case 0x3C:      SRLIndex(addr, H);         break;
    case 0x3D:      SRLIndex(addr, L);         break;
    case 0x3E:      SRLIndex(addr, junk);      break;
    case 0x3F:      SRLIndex(addr, A);         break;

    case 0x40:
    case 0x41:
    case 0x42:
    case 0x43:
    case 0x44:
    case 0x45:
    case 0x46:
    case 0x47:      BITbIndex(addr, 0);        break;

    case 0x48:
    case 0x49:
    case 0x4A:
    case 0x4B:
    case 0x4C:
    case 0x4D:
    case 0x4E:
    case 0x4F:      BITbIndex(addr, 1);        break;

    case 0x50:
    case 0x51:
    case 0x52:
    case 0x53:
    case 0x54:
    case 0x55:
    case 0x56:
    case 0x57:      BITbIndex(addr, 2);        break;

    case 0x58:
    case 0x59:
    case 0x5A:
    case 0x5B:
    case 0x5C:
    case 0x5D:
    case 0x5E:
    case 0x5F:      BITbIndex(addr, 3);        break;

    case 0x60:
    case 0x61:
    case 0x62:
    case 0x63:
    case 0x64:
    case 0x65:
    case 0x66:
    case 0x67:      BITbIndex(addr, 4);        break;

    case 0x68:
    case 0x69:
    case 0x6A:
    case 0x6B:
    case 0x6C:
    case 0x6D:
    case 0x6E:
    case 0x6F:      BITbIndex(addr, 5);        break;

    case 0x70:
    case 0x71:
    case 0x72:
    case 0x73:
    case 0x74:
    case 0x75:
    case 0x76:
    case 0x77:      BITbIndex(addr, 6);        break;

    case 0x78:
    case 0x79:
    case 0x7A:
    case 0x7B:
    case 0x7C:
    case 0x7D:
    case 0x7E:
    case 0x7F:      BITbIndex(addr, 7);        break;

    case 0x80:      RESbIndex(addr, 0, B);     break;
    case 0x81:      RESbIndex(addr, 0, C);     break;
    case 0x82:      RESbIndex(addr, 0, D);     break;
    case 0x83:      RESbIndex(addr, 0, E);     break;
    case 0x84:      RESbIndex(addr, 0, H);     break;
    case 0x85:      RESbIndex(addr, 0, L);     break;
    case 0x86:      RESbIndex(addr, 0, junk);  break;
    case 0x87:      RESbIndex(addr, 0, A);     break;

    case 0x88:      RESbIndex(addr, 1, B);     break;
    case 0x89:      RESbIndex(addr, 1, C);     break;
    case 0x8A:      RESbIndex(addr, 1, D);     break;
    case 0x8B:      RESbIndex(addr, 1, E);     break;
    case 0x8C:      RESbIndex(addr, 1, H);     break;
    case 0x8D:      RESbIndex(addr, 1, L);     break;
    case 0x8E:      RESbIndex(addr, 1, junk);  break;
    case 0x8F:      RESbIndex(addr, 1, A);     break;

    case 0x90:      RESbIndex(addr, 2, B);     break;
    case 0x91:      RESbIndex(addr, 2, C);     break;
    case 0x92:      RESbIndex(addr, 2, D);     break;
    case 0x93:      RESbIndex(addr, 2, E);     break;
    case 0x94:      RESbIndex(addr, 2, H);     break;
    case 0x95:      RESbIndex(addr, 2, L);     break;
    case 0x96:      RESbIndex(addr, 2, junk);  break;
    case 0x97:      RESbIndex(addr, 2, A);     break;

    case 0x98:      RESbIndex(addr, 3, B);     break;
    case 0x99:      RESbIndex(addr, 3, C);     break;
    case 0x9A:      RESbIndex(addr, 3, D);     break;
    case 0x9B:      RESbIndex(addr, 3, E);     break;
    case 0x9C:      RESbIndex(addr, 3, H);     break;
    case 0x9D:      RESbIndex(addr, 3, L);     break;
    case 0x9E:      RESbIndex(addr, 3, junk);  break;
    case 0x9F:      RESbIndex(addr, 3, A);     break;

    case 0xA0:      RESbIndex(addr, 4, B);     break;
    case 0xA1:      RESbIndex(addr, 4, C);     break;
    case 0xA2:      RESbIndex(addr, 4, D);     break;
    case 0xA3:      RESbIndex(addr, 4, E);     break;
    case 0xA4:      RESbIndex(addr, 4, H);     break;
    case 0xA5:      RESbIndex(addr, 4, L);     break;
    case 0xA6:      RESbIndex(addr, 4, junk);  break;
    case 0xA7:      RESbIndex(addr, 4, A);     break;

    case 0xA8:      RESbIndex(addr, 5, B);     break;
    case 0xA9:      RESbIndex(addr, 5, C);     break;
    case 0xAA:      RESbIndex(addr, 5, D);     break;
    case 0xAB:      RESbIndex(addr, 5, E);     break;
    case 0xAC:      RESbIndex(addr, 5, H);     break;
    case 0xAD:      RESbIndex(addr, 5, L);     break;
    case 0xAE:      RESbIndex(addr, 5, junk);  break;
    case 0xAF:      RESbIndex(addr, 5, A);     break;

    case 0xB0:      RESbIndex(addr, 6, B);     break;
    case 0xB1:      RESbIndex(addr, 6, C);     break;
    case 0xB2:      RESbIndex(addr, 6, D);     break;
    case 0xB3:      RESbIndex(addr, 6, E);     break;
    case 0xB4:      RESbIndex(addr, 6, H);     break;
    case 0xB5:      RESbIndex(addr, 6, L);     break;
    case 0xB6:      RESbIndex(addr, 6, junk);  break;
    case 0xB7:      RESbIndex(addr, 6, A);     break;

    case 0xB8:      RESbIndex(addr, 7, B);     break;
    case 0xB9:      RESbIndex(addr, 7, C);     break;
    case 0xBA:      RESbIndex(addr, 7, D);     break;
    case 0xBB:      RESbIndex(addr, 7, E);     break;
    case 0xBC:      RESbIndex(addr, 7, H);     break;
    case 0xBD:      RESbIndex(addr, 7, L);     break;
    case 0xBE:      RESbIndex(addr, 7, junk);  break;
    case 0xBF:      RESbIndex(addr, 7, A);     break;

    case 0xC0:      SETbIndex(addr, 0, B);     break;
    case 0xC1:      SETbIndex(addr, 0, C);     break;
    case 0xC2:      SETbIndex(addr, 0, D);     break;
    case 0xC3:      SETbIndex(addr, 0, E);     break;
    case 0xC4:      SETbIndex(addr, 0, H);     break;
    case 0xC5:      SETbIndex(addr, 0, L);     break;
    case 0xC6:      SETbIndex(addr, 0, junk);  break;
    case 0xC7:      SETbIndex(addr, 0, A);     break;

    case 0xC8:      SETbIndex(addr, 1, B);     break;
    case 0xC9:      SETbIndex(addr, 1, C);     break;
    case 0xCA:      SETbIndex(addr, 1, D);     break;
    case 0xCB:      SETbIndex(addr, 1, E);     break;
    case 0xCC:      SETbIndex(addr, 1, H);     break;
    case 0xCD:      SETbIndex(addr, 1, L);     break;
    case 0xCE:      SETbIndex(addr, 1, junk);  break;
    case 0xCF:      SETbIndex(addr, 1, A);     break;

    case 0xD0:      SETbIndex(addr, 2, B);     break;
    case 0xD1:      SETbIndex(addr, 2, C);     break;
    case 0xD2:      SETbIndex(addr, 2, D);     break;
    case 0xD3:      SETbIndex(addr, 2, E);     break;
    case 0xD4:      SETbIndex(addr, 2, H);     break;
    case 0xD5:      SETbIndex(addr, 2, L);     break;
    case 0xD6:      SETbIndex(addr, 2, junk);  break;
    case 0xD7:      SETbIndex(addr, 2, A);     break;

    case 0xD8:      SETbIndex(addr, 3, B);     break;
    case 0xD9:      SETbIndex(addr, 3, C);     break;
    case 0xDA:      SETbIndex(addr, 3, D);     break;
    case 0xDB:      SETbIndex(addr, 3, E);     break;
    case 0xDC:      SETbIndex(addr, 3, H);     break;
    case 0xDD:      SETbIndex(addr, 3, L);     break;
    case 0xDE:      SETbIndex(addr, 3, junk);  break;
    case 0xDF:      SETbIndex(addr, 3, A);     break;

    case 0xE0:      SETbIndex(addr, 4, B);     break;
    case 0xE1:      SETbIndex(addr, 4, C);     break;
    case 0xE2:      SETbIndex(addr, 4, D);     break;
    case 0xE3:      SETbIndex(addr, 4, E);     break;
    case 0xE4:      SETbIndex(addr, 4, H);     break;
    case 0xE5:      SETbIndex(addr, 4, L);     break;
    case 0xE6:      SETbIndex(addr, 4, junk);  break;
    case 0xE7:      SETbIndex(addr, 4, A);     break;

    case 0xE8:      SETbIndex(addr, 5, B);     break;
    case 0xE9:      SETbIndex(addr, 5, C);     break;
    case 0xEA:      SETbIndex(addr, 5, D);     break;
    case 0xEB:      SETbIndex(addr, 5, E);     break;
    case 0xEC:      SETbIndex(addr, 5, H);     break;
    case 0xED:      SETbIndex(addr, 5, L);     break;
    case 0xEE:      SETbIndex(addr, 5, junk);  break;
    case 0xEF:      SETbIndex(addr, 5, A);     break;

    case 0xF0:      SETbIndex(addr, 6, B);     break;
    case 0xF1:      SETbIndex(addr, 6, C);     break;
    case 0xF2:      SETbIndex(addr, 6, D);     break;
    case 0xF3:      SETbIndex(addr, 6, E);     break;
    case 0xF4:      SETbIndex(addr, 6, H);     break;
    case 0xF5:      SETbIndex(addr, 6, L);     break;
    case 0xF6:      SETbIndex(addr, 6, junk);  break;
    case 0xF7:      SETbIndex(addr, 6, A);     break;

    case 0xF8:      SETbIndex(addr, 7, B);     break;
    case 0xF9:      SETbIndex(addr, 7, C);     break;
    case 0xFA:      SETbIndex(addr, 7, D);     break;
    case 0xFB:      SETbIndex(addr, 7, E);     break;
    case 0xFC:      SETbIndex(addr, 7, H);     break;
    case 0xFD:      SETbIndex(addr, 7, L);     break;
    case 0xFE:      SETbIndex(addr, 7, junk);  break;
    case 0xFF:      SETbIndex(addr, 7, A);     break;
    }
}

bool Core::HandleInterrupt()
{
    if (_iff1 && _interruptRequested)
    {
        if (_halted)
        {
            PC++;
            _halted = false;
        }

        _interruptRequested = false;
        _iff1 = false;
        _iff2 = false;

        _crtc._scanLineCount &= 0xDF;

        IncrementR();

        switch (_interruptMode)
        {
        case 1:
        {
            Tick(7);

            SP--;
            MemWriteRequest(SP, High(PC));
            Tick(3);

            SP--;
            MemWriteRequest(SP, Low(PC));
            Tick(3);

            PC = 0x0038;
            _interruptRequested = false;

            return true;
        }
        break;
        }
    }

    return false;
}

StreamWriter& operator<<(StreamWriter& s, const Core& core)
{
    s << core._ticks;

    s << core.AF << core.BC << core.DE << core.HL;
    s << core.AF_ << core.BC_ << core.DE_ << core.HL_;

    s << core.IX << core.IY;

    s << core.PC << core.SP;

    s << core._iff1 << core._iff2 << core._interruptRequested << core._interruptMode << core._eiDelay << core._halted;

    s << core._memory;
    s << core._fdc;
    s << core._keyboard;
    s << core._crtc;
    s << core._psg;
    s << core._ppi;
    s << core._gateArray;

    s << core._tape;

    s << core._audioTickTotal;
    s << core._audioTicksToNextSample;
    s << core._audioSampleCount;
    s << core._frequency;

    return s;
}

StreamReader& operator>>(StreamReader& s, Core& core)
{
    s >> core._ticks;

    s >> core.AF >> core.BC >> core.DE >> core.HL;
    s >> core.AF_ >> core.BC_ >> core.DE_ >> core.HL_;

    s >> core.IX >> core.IY;

    s >> core.PC >> core.SP;

    s >> core._iff1 >> core._iff2 >> core._interruptRequested >> core._interruptMode >> core._eiDelay >> core._halted;

    s >> core._memory;
    s >> core._fdc;
    s >> core._keyboard;
    s >> core._crtc;
    s >> core._psg;
    s >> core._ppi;
    s >> core._gateArray;

    s >> core._tape;

    s >> core._audioTickTotal;
    s >> core._audioTicksToNextSample;
    s >> core._audioSampleCount;
    s >> core._frequency;

    return s;
}
