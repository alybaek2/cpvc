#pragma once

#include "common.h"

#include "Memory.h"
#include "PPI.h"
#include "PSG.h"
#include "Keyboard.h"
#include "Audio.h"
#include "GateArray.h"
#include "Tape.h"
#include "FDC.h"
#include "CRTC.h"
#include "Bus.h"
#include "IBus.h"

// Z80 flags
constexpr byte flagS = 0x80;     // Sign flag
constexpr byte flagZ = 0x40;     // Zero flag
constexpr byte flag5 = 0x20;     // Undocumented bit 5 flag
constexpr byte flagH = 0x10;     // Half-carry flag
constexpr byte flag3 = 0x08;     // Undocumented bit 3 flag
constexpr byte flagPV = 0x04;    // Parity/Overflow flag
constexpr byte flagN = 0x02;     // Add/Subtract flag
constexpr byte flagC = 0x01;     // Carry flag

// Bitmask for conditions to stop execution in RunUntil.
constexpr byte stopNone = 0x00;
constexpr byte stopAudioOverrun = 0x01;
constexpr byte stopVSync = 0x02;

// Class representing the CPC's hardware.
class Core
{
public:
    Core();
    Core(IBus* pBus);
    ~Core();

    void Init();
    void Reset();
    bool KeyPress(byte keycode, bool down);
    void LoadTape(const byte* pBuffer, int size);
    void LoadDisc(byte drive, const byte* pBuffer, int size);

    void SetScreen(byte* pBuffer, word pitch, word height, word width);

    int GetAudioBuffers(int numSamples, byte* (&pChannels)[3]);
    void SetFrequency(dword frequency);

    void EnableLowerROM(bool enabled);
    void SetLowerRom(Mem16k& lowerRom);
    void EnableUpperROM(bool enabled);
    void SetUpperRom(byte slot, Mem16k& rom);

    byte RunUntil(qword stopTicks, byte stopReason);

    byte ReadRAM(const word& addr);
    void WriteRAM(const word& addr, byte b);

    qword Ticks();

    // Z80 Registers.
    word AF; byte& A = High(AF); byte& F = Low(AF);
    word BC; byte& B = High(BC); byte& C = Low(BC);
    word DE; byte& D = High(DE); byte& E = Low(DE);
    word HL; byte& H = High(HL); byte& L = Low(HL);
    word IR; byte& I = High(IR); byte& R = Low(IR);

    word AF_;
    word BC_;
    word DE_;
    word HL_;

    word IX;
    word IY;

    word PC;
    word SP;

    // Interrupt-related member variables.
    bool _iff1;
    bool _iff2;
    bool _interruptRequested;
    byte _interruptMode;
    byte _eiDelay;
    bool _halted;

    friend StreamWriter& operator<<(StreamWriter& s, const Core& core);
    friend StreamReader& operator>>(StreamReader& s, Core& core);

private:
    // Hardware components.
    Memory _memory;
    IBus* _pBus;
    Bus _bus = Bus(_memory, _gateArray, _ppi, _crtc, _fdc);
    FDC _fdc;
    Keyboard _keyboard;
    CRTC _crtc = CRTC(_interruptRequested);
    PSG _psg = PSG(_keyboard);
    PPI _ppi = PPI(_psg, _keyboard, &_crtc._inVSync, &_tape._motor, &_tape._level);
    GateArray _gateArray = GateArray(_memory, _interruptRequested, _crtc._scanLineCount);
    Tape _tape;

    // The CPC's internal "clock"; each tick represents 0.25 microseconds.
    qword _ticks;

    byte BusRead(const word& addr);
    void BusWrite(const word& addr, byte b);

    // Runs the CPC until the next microsecond boundary. Used when accessing RAM or IO.
    void TickToNextMs();

    // Runs the CPC for the specified number of ticks.
    void Tick(byte ticks);

    // Runs all non-Z80 hardware for the specified number of ticks.
    void NonCPUTick(byte ticks);

    // Audio/Video rendering methods.
    void VideoRender();
    void AudioRender();

    // Audio members.
    dword _frequency = 48000;
    Audio _audio;
    dword _audioTickTotal;
    dword _audioTicksToNextSample;
    dword _audioSampleCount;

    byte MemReadRequest(word addr);
    void MemWriteRequest(word addr, byte b);
    byte BusReadRequest(word addr);
    void BusWriteRequest(word addr, byte b);

    // Z80 execution methods.
    void Step(byte stopReason);
    void Execute(byte op);
    void ExecuteCB();
    void ExecuteED();
    void ExecuteDDFD(word& xy);
    void ExecuteDDFDCB(word& xy);
    bool HandleInterrupt();

    // Screen buffer.
    byte* _pScreen;
    word _scrPitch;
    word _scrHeight;
    word _scrWidth;

#pragma region "Flag helpers"
    bool Sign() { return ((F &  flagS) != 0); }
    bool Zero() { return ((F &  flagZ) != 0); }
    bool HalfCarry() { return ((F &  flagH) != 0); }
    bool ParityOverflow() { return ((F & flagPV) != 0); }
    bool AddSubtract() { return ((F &  flagN) != 0); }
    bool Carry() { return ((F &  flagC) != 0); }

    byte Carry8(word w)
    {
        return ((w & 0x100) != 0) ? flagC : 0;
    }

    byte Carry16(dword d)
    {
        return ((d & 0x10000) != 0) ? flagC : 0;
    }

    byte Zero8(byte b)
    {
        return (b == 0) ? flagZ : 0;
    }

    byte Zero16(word w)
    {
        return (w == 0) ? flagZ : 0;
    }

    byte Sign8(byte b)
    {
        return Bit(b, 7) ? flagS : 0;
    }

    byte Sign16(word w)
    {
        return Sign8(High(w));
    }

    byte Flags35(byte b)
    {
        return b & (flag3 | flag5);
    }

    byte Half8(byte b1, byte b2, word res)
    {
        // The Half-Carry flag indicates if there has been a carry from bit 3 to
        // bit 4 during an addition operation, or a borrow from bit 4 to bit 3
        // during a subtraction operation.
        //
        // A simple way to determine if there's been a carry for addition is to
        // add the low nibbles of each operand and see if the result has bit 4
        // set. Note that this is the same approach used with the Carry flag.
        //
        // If we don't want to be bothered to strip off the low nibble of each,
        // we can simply take the two operands (b1, b2) and add them to get the
        // result. However, note that we can't test for bit 4 yet since each of
        // the operands may have had bit 4 set!
        //
        // So we have the following scenarios:
        //
        // b1  ---0----                     b1  ---0----
        // b2  ---0----  No Half-Carry      b2  ---0----  Half-Carry
        // res ---0----                     res ---1----
        //
        // b1  ---0----                     b1  ---0----
        // b2  ---1----  Half-Carry         b2  ---1----  No Half-Carry
        // res ---0----                     res ---1----
        //
        // b1  ---1----                     b1  ---1----
        // b2  ---0----  Half-Carry         b2  ---0----  No Half-Carry
        // res ---0----                     res ---1----
        //
        // b1  ---1----                     b1  ---1----
        // b2  ---1----  No Half-Carry      b2  ---1----  Half-Carry
        // res ---0----                     res ---1----
        // 
        // The pattern here is that there is a Half-Carry if and only if
        // bit4(b1) XOR bit4(b2) XOR bit4(res) is 1. Otherwise it is 0. Note
        // that this is more conviniently expressed as bit4(b1 XOR b2 XOR res).
        //
        // Note that this approach applies for subtraction as well, since
        // b1 - b2 is the same as b1 + (~b2). So the caller should pass ~b2
        // instead of b2.
        //
        // Also note this method is called for ADC (Add with Carry) and SBC
        // (subtract with carry). The same method will work for these too.
        // The addition/subtraction of 1 (the carry flag) isn't enough to
        // change the results of the scenarios above.

        byte res1 = ((b1 ^ b2 ^ res) & 0x10) ? flagH : 0;

        return res1;
    }

    byte Half16(word b1, word b2, word res)
    {
        return Half8(High(b1), High(b2), High(res));
    }

    byte Overflow8Add(byte b1, byte b2, word res)
    {
        // The overflow flag is set when the result of an addition or
        // subtraction goes beyond the range [+127, -128].
        //
        // So we have the following scenarios for addition:
        //
        // b1  0-------                     b1  0-------
        // b2  0-------  No Overflow        b2  0-------  Overflow  
        // res 0-------                     res 1-------
        //
        // b1  0-------                     b1  0-------
        // b2  1-------  No Overflow        b2  1-------  No Overflow  
        // res 0-------                     res 1-------
        //                                        
        // b1  1-------                     b1  1-------
        // b2  0-------  No Overflow        b2  0-------  No Overflow  
        // res 0-------                     res 1-------
        //                                        
        // b1  1-------                     b1  1-------
        // b2  1-------  Overflow           b2  1-------  No Overflow  
        // res 0-------                     res 1-------
        //
        // Therefore, Overflow is set when bit 7 of both operands are the
        // same, and opposite to bit 7 of the result.

        return ((b1 ^ b2 ^ 0x80) & (b1 ^ res) & 0x80) ? flagPV : 0;
    }

    byte Overflow8Sub(byte b1, byte b2, word res)
    {
        // The overflow flag is set when the result of an addition or
        // subtraction goes beyond the range [+127, -128].
        //
        // So we have the following scenarios for subtraction:
        //
        // b1  0-------                     b1  0-------
        // b2  0-------  No Overflow        b2  0-------  No Overflow  
        // res 0-------                     res 1-------
        //
        // b1  0-------                     b1  0-------
        // b2  1-------  No Overflow        b2  1-------  Overflow  
        // res 0-------                     res 1-------
        //                                        
        // b1  1-------                     b1  1-------
        // b2  0-------  Overflow           b2  0-------  No Overflow  
        // res 0-------                     res 1-------
        //                                        
        // b1  1-------                     b1  1-------
        // b2  1-------  No Overflow        b2  1-------  No Overflow  
        // res 0-------                     res 1-------
        //
        // Therefore, Overflow is set when bit 7 of both operands are
        // different, and bit 7 of b2 equals bit 7 of the result.

        return ((b2 ^ res ^ 0x80) & (b2 ^ b1) & 0x80) ? flagPV : 0;
    }

    byte Overflow16Add(word w1, word w2, word res)
    {
        return Overflow8Add(High(w1), High(w2), High(res));
    }

    byte Overflow16Sub(word w1, word w2, word res)
    {
        return Overflow8Sub(High(w1), High(w2), High(res));
    }

    byte SZ(byte n)
    {
        return Zero8(n) | Sign8(n);
    }

    byte SZ35(byte n)
    {
        return Flags35(n) | SZ(n);
    }

    byte SZP35(byte n)
    {
        return SZ35(n) | Parity(n);
    }

    byte Parity(byte b)
    {
        // Parity flag is true if parity of argument is even, and is false otherwise
        byte t = b;
        t ^= (t >> 4);
        t ^= (t >> 2);
        t ^= (t >> 1);

        return ((t & 0x01) == 0) ? flagPV : 0;
    }
#pragma endregion

    // Z80 implementation methods. For optimal performance, these methods
    // are defined inline.
    //
    // r: 8-bit register (A, F, B, C, D, E, H, L, IXh, IXl, IYh, IYl)
    // n: 8-bit value
    // nn: 16-bit value
    // xy: 16-bit indexing register (IX, IY)
    // o: 8-bit signed integer
    // dd: 16-bit register (BC, DE, HL, SP)
    // cc: Conditional flag (NZ, Z, NC, C, PO, PE, P, M)
    // p: RST addresses (0x00, 0x08, 0x10, 0x18, 0x20, 0x28, 0x30, 0x38)

    void IncrementR()
    {
        R = (R & 0x80) | ((R + 1) & 0x7F);
    }

#pragma region "8-bit Load Group"
    // LD r,r
    void LDrr(byte& dst, byte& src)
    {
        // M1 (continued)
        dst = src;
    }

    // LD r,n
    void LDrn(byte& r)
    {
        // M2
        r = MemReadRequest(PC++);
        Tick(3);
    }

    // LD r,(HL)
    void LDrHL(byte& r)
    {
        // M2
        r = MemReadRequest(HL);
        Tick(3);
    }

    // LD r,(xy + o)
    void LDrxy(const word& xy, byte& r)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        r = MemReadRequest(addr);
        Tick(3);
    }

    // LD (HL),r
    void LDHLr(byte r)
    {
        // M2
        MemWriteRequest(HL, r);
        Tick(3);
    }

    // LD (xy + o),r
    void LDxyr(const word& xy, byte r)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        MemWriteRequest(addr, r);
        Tick(3);
    }

    // LD (HL),n
    void LDHLn()
    {
        // M2
        byte n = MemReadRequest(PC++);
        Tick(3);

        // M3
        MemWriteRequest(HL, n);
        Tick(3);
    }

    // LD (xy + o),n
    void LDxyn(const word& xy)
    {
        // M3
        offset o = (offset) MemReadRequest(PC++);
        Tick(3);

        // M4
        byte n = MemReadRequest(PC++);
        Tick(3);

        // M5
        word addr = xy + o;
        Tick(5);

        // M6
        MemWriteRequest(addr, n);
        Tick(3);
    }

    // LD A,(BC)
    // LD A,(DE)
    void LDArr(const word& addr)
    {
        // M2
        A = MemReadRequest(addr);
        Tick(3);
    }

    // LD A,(nn)
    void LDAnn()
    {
        word addr = 0;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        // M4
        A = MemReadRequest(addr);
        Tick(3);
    }

    // LD (BC),A
    // LD (DE),A
    void LDrrA(const word& addr, byte& r)
    {
        // M2
        MemWriteRequest(addr, r);
        Tick(3);
    }

    // LD (nn),A
    void LDnnA()
    {
        word addr = 0;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        // M4
        MemWriteRequest(addr, A);
        Tick(3);
    }

    // LD A,I
    // LD A,R
    void LDAIR(byte b)
    {
        // M2 (continued)
        F = Sign8(b) |
            Zero8(b) |
             Flags35(b) |
             (_iff2 ? flagPV : 0) |
             (F & flagC);
        A = b;
        Tick(1);
    }

    // LD I,A
    // LD R,A
    void LDIRA(byte& b)
    {
        // M2 (continued)
        b = A;
        Tick(1);
    }
#pragma endregion

#pragma region "16-bit Load Group"
    // LD dd,nn
    void LDddnn(word& rr)
    {
        // M2
        Low(rr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(rr) = MemReadRequest(PC++);
        Tick(3);
    }

    // LD IX,nn
    // LD IY,nn
    void LDxynn(word& xy)
    {
        // M3
        Low(xy) = MemReadRequest(PC++);
        Tick(3);

        // M4
        High(xy) = MemReadRequest(PC++);
        Tick(3);
    }

    // LD HL,(nn)
    // LD IX,(nn)
    // LD IY,(nn)
    // Note for IX and IY, M2-M5 are actually M3-M6
    void LDHLnnInd(word& rr)
    {
        word addr = 0;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        // M4
        Low(rr) = MemReadRequest(addr);
        Tick(3);

        // M5
        High(rr) = MemReadRequest(addr + 1);
        Tick(3);
    }

    // LD dd,(nn)
    void LDddnnInd(word& dd)
    {
        word addr = 0;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        // M4
        Low(dd) = MemReadRequest(addr);
        Tick(3);

        // M5
        High(dd) = MemReadRequest(addr + 1);
        Tick(3);
    }

    // LD (nn),HL
    // LD (nn),IX
    // LD (nn),IY
    // Note for IX and IY, M2-M5 are actually M3-M6
    void LDnnHL(const word& rr)
    {
        word addr = 0;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        // M4
        MemWriteRequest(addr, Low(rr));
        Tick(3);

        // M5
        MemWriteRequest(addr + 1, High(rr));
        Tick(3);
    }

    // LD (nn),dd
    void LDnndd(word& dd)
    {
        word addr = 0;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        // M4
        MemWriteRequest(addr, Low(dd));
        Tick(3);

        // M5
        MemWriteRequest(addr + 1, High(dd));
        Tick(3);
    }

    // LD SP,HL
    // LD SP,IX
    // LD SP,IY
    // Note for IX and IY, M2 is actually M3
    void LDSPHL(const word& rr)
    {
        // M2
        SP = rr;
        Tick(2);
    }

    // PUSH qq
    // PUSH IX
    // PUSH IY
    // Note for IX and IY, M1-M3 is actually M2-M4
    void PUSH(const word& qq)
    {
        // M1 (continued)
        Tick(1);

        // M2
        MemWriteRequest(--SP, High(qq));
        Tick(3);

        // M3
        MemWriteRequest(--SP, Low(qq));
        Tick(3);
    }

    // POP qq
    // POP IX
    // POP IY
    // Note for IX and IY, M2-M3 is actually M3-M4
    void POP(word& qq)
    {
        // M2
        Low(qq) = MemReadRequest(SP++);
        Tick(3);

        // M3
        High(qq) = MemReadRequest(SP++);
        Tick(3);
    }
#pragma endregion

#pragma region "Exchange, Block Transfer, Search Group"
    // EX DE,HL
    // EX AF,AF'
    void EX(word& x, word& y)
    {
        // M1 (continued)
        word t = y;
        y = x;
        x = t;
    }

    // EXX
    void EXX()
    {
        EX(BC, BC_);
        EX(DE, DE_);
        EX(HL, HL_);
    }

    // EX (SP),HL
    // EX (SP),IX
    // EX (SP),IY
    // Note for IX and IY, M2-M5 are actually M3-M6
    void EXSPrr(word& rr)
    {
        word temp = 0;

        // M2
        Low(temp) = MemReadRequest(SP++);
        Tick(3);

        // M3
        High(temp) = MemReadRequest(SP);
        Tick(4);

        // M4
        MemWriteRequest(SP--, High(rr));
        Tick(3);

        // M5
        MemWriteRequest(SP, Low(rr));
        rr = temp;
        Tick(5);
    }

    // LDI
    // LDIR
    // LDD
    // LDDR
    void LDBlock(bool inc, bool repeat)
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(3);

        // M4
        MemWriteRequest(DE, n);
        n += A;
        BC--;
        DE += (inc?1:-1);
        HL += (inc?1:-1);
        F = (F & (flagC | flagS | flagZ)) |
            ((BC != 0) ? flagPV : 0) |
            (n & flag3) |
            (((n & 0x02) != 0) ? flag5 : 0);
        Tick(5);

        if (repeat && BC != 0)
        {
            // M5
            PC -= 2;
            Tick(5);
        }
    }

    // CPI
    // CPIR
    // CPD
    // CPDR
    void CPBlock(bool repeat, bool inc)
    {
        // M3
        byte n = MemReadRequest(HL);
        word result = (word) A - (word) n;
        Tick(3);

        // M4
        HL += inc ? 1 : -1;
        BC--;

        byte halfCarry = Half8(A, n, result);
        byte h = (byte) result;
        if (halfCarry != 0)
        {
            h--;
        }

        F =
            (F & flagC) |
            SZ((byte) result) |
            flagN |
            ((BC != 0) ? flagPV : 0) |
            Flags35(h) |
            halfCarry;
            
        Tick(5);

        if (repeat && BC != 0 && !Zero())
        {
            // M5
            PC -= 2;
            Tick(5);
        }
    }
#pragma endregion

#pragma region "8-Bit Arithmetic and Logical Group"
    // Helper for addition/subtraction
    void ADD(const byte b, bool carry)
    {
        byte previousA = A;
        signed short carryAddend = ((carry & ((F & flagC) != 0)) ? 1 : 0);
        signed short addend = b + carryAddend;
        signed short result = A + addend;
        A = (byte)result;
        F =
            SZ35(A) |
            (((LowNibble(previousA) + LowNibble(b) + LowNibble((byte) carryAddend)) >= 0x10) ? flagH : 0x00) |
            (Bit(result, 8) ? flagC : 0x00) |
            Overflow8Add(previousA, b, result);
    }

    void SUB(const byte b, bool carry)
    {
        SUB(A, b, carry);
    }

    void SUB(byte& a, const byte b, bool carry)
    {
        byte previousA = A;
        word subtrahend = b + ((carry & ((F & flagC) != 0)) ? 1 : 0);
        word result = A - subtrahend;
        a = (byte)result;
        F =
            SZ35(a) |
            Half8(previousA, b, result) |
            Carry8(result) |
            flagN |
            Overflow8Sub(previousA, b, result);
    }

    // ADD A,r
    // ADC A,r
    void ADDr(byte b, bool carry)
    {
        // M1 (continued)
        ADD(b, carry);
    }

    // ADD A,n
    // ADC A,n
    void ADDn(bool carry)
    {
        // M2
        byte n = MemReadRequest(PC++);
        ADD(n, carry);
        Tick(3);
    }

    // ADD A,(HL)
    // ADC A,(HL)
    void ADDHLInd(bool carry)
    {
        // M2
        byte n = MemReadRequest(HL);
        ADD(n, carry);
        Tick(3);
    }

    // ADD A,(xy + o)
    // ADC A,(xy + o)
    void ADDIndex(word& xy, bool carry)
    {
        // M2
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M3
        word addr = xy + o;
        Tick(5);

        // M4
        byte n = MemReadRequest(addr);
        ADD(n, carry);
        Tick(3);
    }

    // SUB A,r
    // SBC A,r
    void SUBr(byte b, bool carry)
    {
        // M1 (continued)
        SUB(b, carry);
    }

    // SUB A,n
    // SBC A,n
    void SUBn(bool carry)
    {
        // M2
        byte n = MemReadRequest(PC++);
        SUB(n, carry);
        Tick(3);
    }

    // SUB A,(HL)
    // SBC A,(HL)
    void SUBHLInd(bool carry)
    {
        // M2
        byte n = MemReadRequest(HL);
        SUB(n, carry);
        Tick(3);
    }

    // SUB A,(xy + o)
    // SBC A,(xy + o)
    void SUBIndex(word& xy, bool carry)
    {
        // M2
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M3
        word addr = xy + o;
        Tick(5);

        // M4
        byte n = MemReadRequest(addr);
        SUB(n, carry);
        Tick(3);
    }

    // AND A,r
    void ANDr(byte b)
    {
        // M1 (continued)
        A = A & b;
        F = SZP35(A) | flagH;
    }

    // AND A,n
    void ANDn()
    {
        // M2
        byte n = MemReadRequest(PC++);
        ANDr(n);
        Tick(3);
    }

    // AND A,(HL)
    void ANDHLInd()
    {
        // M2
        byte n = MemReadRequest(HL);
        ANDr(n);
        Tick(3);
    }

    // AND A,(xy + o)
    void ANDIndex(word& xy)
    {
        // M3
        offset o = (offset) MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        byte n = MemReadRequest(addr);
        ANDr(n);
        Tick(3);
    }

    // OR A,r
    void ORr(byte b)
    {
        // M1 (continued)
        A = A | b;
        F = SZP35(A);
    }

    // OR A,n
    void ORn()
    {
        // M2
        byte n = MemReadRequest(PC++);
        ORr(n);
        Tick(3);
    }

    // OR A,(HL)
    void ORHLInd()
    {
        // M2
        byte n = MemReadRequest(HL);
        ORr(n);
        Tick(3);
    }

    // OR A,(xy + o)
    void ORIndex(word& xy)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        byte n = MemReadRequest(addr);
        ORr(n);
        Tick(3);
    }

    // XOR A,r
    void XORr(byte b)
    {
        // M1 (continued)
        A = A ^ b;
        F = SZP35(A);
    }

    // XOR A,n
    void XORn()
    {
        // M2
        byte n = MemReadRequest(PC++);
        XORr(n);
        Tick(3);
    }

    // XOR A,(HL)
    void XORHLInd()
    {
        // M2
        byte n = MemReadRequest(HL);
        XORr(n);
        Tick(3);
    }

    // XOR A,(xy + o)
    void XORIndex(word& xy)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        byte n = MemReadRequest(addr);
        XORr(n);
        Tick(3);
    }

    // CP A,r
    void CPr(byte b)
    {
        // M1 (continued)
        word result = (word)A - (word)b;
        F =
            Carry8(result) |
            SZ((byte) result) |
            flagN |
            Overflow8Sub(A, b, result) |
            Flags35(b) |
            Half8(A, b, result);
    }

    // CP A,n
    void CPn()
    {
        // M2
        byte n = MemReadRequest(PC++);
        CPr(n);
        Tick(3);
    }

    // CP A,(HL)
    void CPHLInd()
    {
        // M2
        byte n = MemReadRequest(HL);
        CPr(n);
        Tick(3);
    }

    // CP A,(xy + o)
    void CPIndex(word& xy)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        byte n = MemReadRequest(addr);
        CPr(n);
        Tick(3);
    }

    // INC r
    void INCr(byte& r)
    {
        // M1 (continued)
        r++;
        F = Sign8(r) |
            ((r == 0x80) ? flagPV : 0) |
            (F & flagC) |
            Zero8(r) |
            ((LowNibble(r) == 0x00) ? flagH : 0) |
            Flags35(r);
    }

    // INC (HL)
    void INCHLInd()
    {
        // M2
        byte n = MemReadRequest(HL);
        INCr(n);
        Tick(3);

        // M3
        MemWriteRequest(HL, n);
        Tick(3);
    }

    // INC (xy + o)
    void INCIndex(const word& xy)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        byte n = MemReadRequest(addr);
        INCr(n);
        Tick(3);

        // M6
        MemWriteRequest(addr, n);
        Tick(3);
    }

    // DEC r
    void DECr(byte& r)
    {
        // M1 (continued)
        r--;
        F = Sign8(r) |
            ((r == 0x7F) ? flagPV : 0) |
            (F & flagC) |
            Zero8(r) |
            ((LowNibble(r) == 0x0F) ? flagH : 0) |
            Flags35(r) |
            flagN;
    }

    // DEC (HL)
    void DECHLInd()
    {
        // M2
        byte n = MemReadRequest(HL);
        DECr(n);
        Tick(3);

        // M3
        MemWriteRequest(HL, n);
        Tick(3);
    }

    // DEC (xy + o)
    void DECIndex(const word& xy)
    {
        // M3
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        // M4
        word addr = xy + o;
        Tick(5);

        // M5
        byte n = MemReadRequest(addr);
        DECr(n);
        Tick(3);

        // M6
        MemWriteRequest(addr, n);
        Tick(3);
    }
#pragma endregion

#pragma region "General-Purpose Arithmetic and CPU Control Group"
    // DAA
    void DAA()
    {
        // M1 (continued
        bool cflag = false;
        byte corr = 0;
        byte ABefore = A;
        if ((A > 0x99) || Carry())
        {
            corr |= 0x60;
            cflag = true;
        }

        if ((LowNibble(A) > 0x09) || HalfCarry())
        {
            corr |= 0x06;
        }

        if (AddSubtract())
        {
            A -= corr;
        }
        else
        {
            A += corr;
        }

        F = (F & ~flagC) | (cflag?flagC:0);
        F = (F & ~flagH) | ((A ^ ABefore) & flagH);
        F = (F & ~(flagS | flagZ | flagPV | flag3 | flag5)) | SZP35(A);
    }

    // CPL
    void CPL()
    {
        // M1 (continued)
        A = 0xFF ^ A;
        F = (F & (flagS | flagZ | flagPV | flagC)) |
            Flags35(A) |
            flagH |
            flagN;
    }

    // NEG
    void NEG()
    {
        byte s = A;
        A = 0;
        SUB(A, s, false);

        F = (F & (~(flagPV | flagC)));
        F |= (s == 0x80) ? flagPV : 0;
        F |= (s != 0x00) ? flagC : 0;
    }

    // CCF
    void CCF()
    {
        // M1 (continued)
        bool carry = Carry();
        F &= (flagS | flagZ | flagPV);
        F |= Flags35(A);
        F |= (carry ? flagH : flagC);
    }

    // SCF
    void SCF()
    {
        // M1 (continued)
        F &= (flagS | flagZ | flagPV);
        F |= flagC;
        F |= Flags35(A);
    }

    // HALT
    void HALT()
    {
        // M1 (continued)
        PC--;
        _halted = true;
    }

    // DI
    void DI()
    {
        // M1 (continued)
        _iff1 = false;
        _iff2 = false;
    }

    // EI
    void EI()
    {
        // M1 (continued)
        _eiDelay = 2;
    }

    // IM 0
    // IM 1
    // IM 2
    void IM(byte mode)
    {
        // M1 (continued)
        _interruptMode = mode;
    }
#pragma endregion

#pragma region "16-Bit Arithmetic Group"
    // ADD HL,dd
    void ADDHLdd(const word& dd)
    {
        // M2
        dword res = HL + dd;
        word halfRes = (HL & 0x0FFF) + (dd & 0x0FFF);

        // The official documentation for the Z80 and the "Undocumented Z80" don't seem to agree
        // on the affects of this operation on the S, Z, and P/V flags...
        F = (F & (flagS | flagZ | flagPV)) |
            Carry16(res) |
            Half16(HL, dd, res) |
            Flags35(High((word) res));
        HL = res;
        Tick(4);

        // M3 (not sure what parts are done in M2 vs M3. Not that it really matters...)
        Tick(3);
    }

    // ADC HL,dd
    void ADCHLdd(const word& dd)
    {
        // M3
        dword add = dd + (Carry() ? 1 : 0);
        dword res = HL + add;
        F = Carry16(res) |
            Half16(HL, add, res) |
            Flags35(High((word)res)) |
            Zero16(res) |
            Overflow16Add(HL, dd, res) |
            Sign16(res);
        HL = res;

        Tick(4);

        // M3 (not sure what parts are done in M2 vs M3. Not that it really matters...)
        Tick(3);
    }

    void SBCHLdd(const word& dd)
    {
        // M3
        dword sub = dd + (Carry()?1:0);
        dword res = HL - sub;
        F = Carry16(res) |
            Half16(HL, sub, res) |
            Flags35(High((word)res)) |
            Zero16(res) |
            Overflow16Sub(HL, dd, res) |
            flagN |
            Sign16(res);
        HL = res;
        Tick(4);

        // M3 (not sure what parts are done in M2 vs M3. Not that it really matters...)
        Tick(3);
    }

    // ADD xy,BC
    // ADD xy,DE
    // ADD xy,xy
    // ADD xy,SP
    void ADDxyrr(word& xy, const word& rr)
    {
        // M3
        dword res = xy + rr;
        word halfRes = (xy & 0x0FFF) + (rr & 0x0FFF);
        F = (F & (flagS | flagZ | flagPV)) |
            Carry16(res) |
            //Zero16(res) |
            //Overflow16Add(xy, rr, res) |
            //Sign16(res) |
            Half16(xy, rr, res) |
            Flags35(High((word)res));
        xy = res;
        Tick(4);

        // M4 (not sure what parts are done in M2 vs M3. Not that it really matters...)
        Tick(3);
    }

    // INC dd
    // INC IX
    // INC IY
    // Note for IX and IY, M1 is actually M2
    void INCrr(word& rr)
    {
        // M1 (continued)
        rr++;
        Tick(2);
    }

    // DEC dd
    // DEC IX
    // DEC IY
    // Note for IX and IY, M1 is actually M2
    void DECrr(word& rr)
    {
        // M1 (continued)
        rr--;
        Tick(2);
    }
#pragma endregion

#pragma region "Rotate and Shift Group Group"
    // RLCA
    void RLCA()
    {
        // M1 (continued)
        byte result = A << 1;
        result |= (Bit(A, 7) ? 0x01 : 0x00);

        F = Flags35(result) | (Bit(A, 7) ? flagC : 0) | (F & (flagS | flagZ | flagPV));
        A = result;
    }

    // RLA
    void RLA()
    {
        // M1 (continued)
        byte result = A << 1;
        result |= (Carry() ? 0x01 : 0x00);

        F = Flags35(result) | (Bit(A, 7) ? flagC : 0) | (F & (flagS | flagZ | flagPV));
        A = result;
    }

    // RRCA
    void RRCA()
    {
        // M1 (continued)
        byte result = A >> 1;
        result |= (Bit(A, 0) ? 0x80 : 0x00);

        F = Flags35(result) | (Bit(A, 0) ? flagC : 0) | (F & (flagS | flagZ | flagPV));
        A = result;
    }

    // RRA
    void RRA()
    {
        // M1 (continued)
        byte result = A >> 1;
        result |= (Carry() ? 0x80 : 0x00);

        F = Flags35(result) | (Bit(A, 0) ? flagC : 0) | (F & (flagS | flagZ | flagPV));
        A = result;
    }

    // RLC r
    void RLCr(byte& r)
    {
        // M2 (continued)
        byte result = r << 1;
        result |= (Bit(r, 7) ? 0x01 : 0x00);

        F = SZP35(result) | (Bit(r, 7) ? flagC : 0);
        r = result;
    }

    // RLC (HL)
    void RLCHLInd(word addr)
    {
        // M3
        byte n = MemReadRequest(addr);

        byte result = n << 1;
        result |= (Bit(n, 7) ? 0x01 : 0x00);
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        Tick(4);

        // M4
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RLC (xy + o)
    void RLCIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);

        byte result = n << 1;
        result |= (Bit(n, 7) ? 0x01 : 0x00);
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        n = result;
        Tick(4);

        // M6
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RL r
    void RLr(byte& r)
    {
        // M2 (continued)
        byte result = r << 1;
        result |= (Carry() ? 0x01 : 0x00);

        F = SZP35(result) | (Bit(r, 7) ? flagC : 0);
        r = result;
    }

    // RL (HL)
    void RLHLInd(word addr)
    {
        // M3
        byte n = MemReadRequest(addr);

        byte result = n << 1;
        result |= (Carry() ? 0x01 : 0x00);
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        Tick(4);

        // M4
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RL (xy + o)
    void RLIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);

        byte result = n << 1;
        result |= (Carry() ? 0x01 : 0x00);
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        n = result;
        Tick(4);

        // M6
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RRC r
    void RRCr(byte& r)
    {
        // M2 (continued)
        byte result = r >> 1;
        result |= (Bit(r, 0) ? 0x80 : 0x00);

        F = SZP35(result) | (Bit(r, 0) ? flagC : 0);
        r = result;
    }

    // RRC (HL)
    void RRCHLInd(word addr)
    {
        // M3
        byte n = MemReadRequest(addr);

        byte result = n >> 1;
        result |= (Bit(n, 0) ? 0x80 : 0x00);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        Tick(4);

        // M4
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RRC (xy + o)
    void RRCIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);

        byte result = n >> 1;
        result |= (Bit(n, 0) ? 0x80 : 0x00);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        n = result;
        Tick(4);

        // M6
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RR r
    void RRr(byte& r)
    {
        // M2 (continued)
        byte result = r >> 1;
        result |= (Carry() ? 0x80 : 0x00);

        F = SZP35(result) | (Bit(r, 0) ? flagC : 0);
        r = result;
    }

    // RR (HL)
    void RRHLInd(word addr)
    {
        // M3
        byte n = MemReadRequest(addr);

        byte result = n >> 1;
        result |= (Carry() ? 0x80 : 0x00);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        Tick(4);

        // M4
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RR (xy + o)
    void RRIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);

        byte result = n >> 1;
        result |= (Carry() ? 0x80 : 0x00);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        n = result;
        Tick(4);

        // M6
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // SLA r
    void SLAr(byte& r)
    {
        // M2 (continued)
        byte result = (r << 1);

        F = SZP35(result) | (Bit(r, 7) ? flagC : 0);
        r = result;
    }

    // SLA (HL)
    void SLAHLInd()
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(4);

        // M4
        byte result = (n << 1);
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        MemWriteRequest(HL, result);
        Tick(3);
    }

    // SLA (xy + o)
    void SLAIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);
        Tick(4);

        // M6
        byte result = (n << 1);
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        n = result;
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // SLL r
    void SLLr(byte& r)
    {
        // M2 (continued)
        byte result = (r << 1) | 0x01;

        F = SZP35(result) | (Bit(r, 7) ? flagC : 0);
        r = result;
    }

    // SLL (HL)
    void SLLHLInd()
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(4);

        // M4
        byte result = (n << 1) | 0x01;
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        MemWriteRequest(HL, result);
        Tick(3);
    }

    // SLL (xy + o)
    void SLLIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);
        Tick(4);

        // M6
        byte result = (n << 1) | 0x01;
        F = SZP35(result) | (Bit(n, 7) ? flagC : 0);
        n = result;
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // SRA r
    void SRAr(byte& r)
    {
        // M2 (continued)
        byte result = (r >> 1) | (r & 0x80);

        F = SZP35(result) | (Bit(r, 0) ? flagC : 0);
        r = result;
    }

    // SRA (HL)
    void SRAHLInd()
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(4);

        // M4
        byte result = (n >> 1) | (n & 0x80);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        MemWriteRequest(HL, result);
        Tick(3);
    }

    // SRA (xy + o)
    void SRAIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);
        Tick(4);

        // M6
        byte result = (n >> 1) | (n & 0x80);

        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        n = result;
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // SRL r
    void SRLr(byte& r)
    {
        // M2 (continued)
        byte result = (r >> 1);

        F = SZP35(result) | (Bit(r, 0) ? flagC : 0);
        r = result;
    }

    // SRL (HL)
    void SRLHLInd()
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(4);

        // M4
        byte result = (n >> 1);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        MemWriteRequest(HL, result);
        Tick(3);
    }

    // SRL (xy + o)
    void SRLIndex(word addr, byte& n)
    {
        // M5
        n = MemReadRequest(addr);
        Tick(4);

        // M6
        byte result = (n >> 1);
        F = SZP35(result) | (Bit(n, 0) ? flagC : 0);
        n = result;
        MemWriteRequest(addr, result);
        Tick(3);
    }

    // RLD
    void RLD()
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(3);

        // M4
        byte newA = HighNibble(A) | (HighNibble(n) >> 4);
        byte newHL = (LowNibble(n) << 4) | (LowNibble(A));
        A = newA;
        F = SZP35(A) | (F & flagC);
        Tick(4);

        // M5
        MemWriteRequest(HL, newHL);
        Tick(3);
    }

    // RRD
    void RRD()
    {
        // M3
        byte n = MemReadRequest(HL);
        Tick(3);

        // M4
        byte newA = HighNibble(A) | LowNibble(n);
        byte newHL = (LowNibble(A) << 4) | (HighNibble(n) >> 4);
        A = newA;
        F = SZP35(A) | (F & flagC);
        Tick(4);

        // M5
        MemWriteRequest(HL, newHL);
        Tick(3);
    }
#pragma endregion

#pragma region "Bit Set, Reset, and Test Group"
    // BIT b,r
    void BITbr(byte b, byte r)
    {
        byte mask = 1 << b;

        byte n = r & mask;

        F = ((flagS | flag3 | flag5) & n) |
            ((n == 0)?(flagZ | flagPV):0) |
            flagH |
            (F & flagC);
    }

    // BIT b,(HL)
    void BITbHLInd(byte b)
    {
        // M3
        byte n = MemReadRequest(HL);
        BITbr(b, n);
        Tick(4);
    }

    // BIT b,(xy + o)
    void BITbIndex(const word& addr, byte b)
    {
        // M5
        byte n = MemReadRequest(addr);
        BITbr(b, n);
        Tick(4);
    }

    // SET b,r
    void SETbr(byte b, byte& r)
    {
        r |= (1 << b);
    }

    // SET b,(HL)
    void SETbHLInd(byte b)
    {
        // M3
        byte n = MemReadRequest(HL);
        SETbr(b, n);
        Tick(4);

        // M4
        MemWriteRequest(HL, n);
        Tick(3);
    }

    // SET b,(xy + o)
    void SETbIndex(word addr, byte b, byte& n)
    {
        // M5
        n = MemReadRequest(addr);
        SETbr(b, n);
        Tick(4);

        // M6
        MemWriteRequest(addr, n);
        Tick(3);
    }

    // RES b,r
    void RESbr(byte b, byte& r)
    {
        r &= (~(1 << b));
    }

    // RES b,(HL)
    void RESbHLInd(byte b)
    {
        // M3
        byte n = MemReadRequest(HL);
        RESbr(b, n);
        Tick(4);

        // M4
        MemWriteRequest(HL, n);
        Tick(3);
    }

    // RES b,(xy + o)
    void RESbIndex(word addr, byte b, byte& n)
    {
        // M5
        n = MemReadRequest(addr);
        RESbr(b, n);
        Tick(4);

        // M6
        MemWriteRequest(addr, n);
        Tick(3);
    }
#pragma endregion

#pragma region "Jump Group"
    // JP nn
    // JP cc,nn
    void JP(bool condition)
    {
        word addr;

        // M2
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        if (condition)
        {
            PC = addr;
        }
    }

    // JR o
    // JR cc,o
    void JR(bool condition)
    {
        // M2
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        if (condition)
        {
            // M3
            PC += o;

            Tick(5);
        }
    }

    // JP HL
    // JP xy
    void JP(word addr)
    {
        PC = addr;
    }

    // DJNZ
    void DJNZ()
    {
        // M1 (continued)
        Tick(1);

        // M2
        offset o = (offset)MemReadRequest(PC++);
        Tick(3);

        B--;
        if (B != 0)
        {
            PC += o;
            Tick(5);
        }
    }
#pragma endregion

#pragma region "Call and Return Group"
    // CALL nn
    // CALL cc,nn
    void CALL(bool condition)
    {
        // M2
        word addr = 0;
        Low(addr) = MemReadRequest(PC++);
        Tick(3);

        // M3
        High(addr) = MemReadRequest(PC++);
        Tick(3);

        if (condition)
        {
            Tick(1);

            // M4
            MemWriteRequest(--SP, High(PC));
            Tick(3);

            // M5
            MemWriteRequest(--SP, Low(PC));
            Tick(3);

            PC = addr;
        }
    }

    // RET
    void RET()
    {
        POP(PC);
    }

    // RET 
    void RETcc(bool condition)
    {
        // M1 (continued)
        Tick(1);

        if (condition)
        {
            // M2
            Low(PC) = MemReadRequest(SP++);
            Tick(3);

            // M3
            High(PC) = MemReadRequest(SP++);
            Tick(3);
        }
    }

    // RETI
    void RETI()
    {
        POP(PC);
        _iff1 = _iff2;
    }

    // RETN
    void RETN()
    {
        POP(PC);
        _iff1 = _iff2;
    }

    // RST p
    void RST(const word& addr)
    {
        PUSH(PC);

        PC = addr;
    }
#pragma endregion

#pragma region "Input and Output Group"
    // IN A,(n)
    void INAn()
    {
        // M2
        byte n = MemReadRequest(PC++);
        Tick(3);

        // M3
        A = BusReadRequest(MakeWord(A, n));
        Tick(4);
    }

    // IN (C),r
    void INrC(byte& r)
    {
        // M1 (continued)
        // One of those instructions with odd timing...
        Tick(4);

        // M2
        r = BusReadRequest(BC);
        F = SZP35(r) | (F & flagC);

        Tick(4);
    }

    // INI
    // INIR
    // IND
    // INDR
    void INBlock(bool inc, bool repeat)
    {
        // M1 (continued)
        Tick(1);

        // M2
        byte n = BusReadRequest(BC);
        Tick(4);

        // M3
        MemWriteRequest(HL, n);
        HL += (inc ? 1 : -1);
        B--;
        Tick(3);

        byte c = C + (inc ? 1 : -1);
        word k = n + c;
        byte p = (k & 0x07) ^ B;

        F = SZ35(B);
        F |= (Bit(n, 7) ? flagN : 0);
        F |= ((k > 0xff) ? (flagH | flagC) : 0);
        F |= Parity(p);

        if (repeat && B != 0)
        {
            PC -= 2;
            Tick(5);
        }
    }

    // OUT (n),A
    void OUTnA()
    {
        // M2
        byte n = MemReadRequest(PC++);
        Tick(3);

        // M3
        BusWriteRequest(MakeWord(A, n), A);
        Tick(4);
    }

    // OUT (C),r
    void OUTCr(const byte r)
    {
        // M1 (continued)
        // One of those instructions with odd timing...
        Tick(4);

        // M2
        BusWriteRequest(BC, r);
        Tick(4);
    }

    // OUTI
    // OTIR
    // OUTD
    // OTDR
    void OUTBlock(bool inc, bool repeat)
    {
        // M1 (continued)
        Tick(1);

        // M2
        byte n = MemReadRequest(HL);
        HL += (inc ? 1 : -1);
        Tick(3);

        // M3
        B--;
        BusWriteRequest(BC, n);
        Tick(4);

        word k = n + L;
        byte p = (k & 0x07) ^ B;

        F = SZ35(B);
        F |= (Bit(n, 7) ? flagN : 0);
        F |= ((k > 0xff) ? (flagH | flagC) : 0);
        F |= Parity(p);

        if (repeat && B != 0)
        {
            PC -= 2;
            Tick(5);
        }
    }
#pragma endregion
};
