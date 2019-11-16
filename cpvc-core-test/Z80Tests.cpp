#include "gtest/gtest.h"
#include "../cpvc-core/Core.h"
#include "MockDevice.h"

#include "helpers.h"

struct RegInfo
{
    RegInfo(byte code, byte* pReg, byte prefix = 0x00)
    {
        _code = code;
        _pReg = pReg;
        _prefix = prefix;
    }

    // Represents the 3-bit code typically used to represent a register in an opcode (e.g. 7 = A, 0 = B, 3 = E).
    byte _code;

    // Pointer to the 8-bit register.
    byte* _pReg;

    // Index instruction prefix (0xDD for IX instructions, 0xFD for IY, and 0x00 for neither).
    byte _prefix;
};

struct IndexRegInfo
{
    IndexRegInfo(byte prefix, word* pReg)
    {
        _prefix = prefix;
        _pReg = pReg;
    }

    // Index instruction prefix (0xDD for IX instructions, 0xFD for IY).
    byte _prefix;

    // Pointer to the index register.
    word* _pReg;
};

class Z80Tests : public ::testing::Test
{
public:
    Z80Tests() : _core(&_bus)
    {
    }

    ~Z80Tests() {}

    Core _core;
    MockDevice _bus;

    IndexRegInfo _idxRegs[2] = {
        IndexRegInfo(0xDD, &_core.IX),
        IndexRegInfo(0xFD, &_core.IY)
    };

    RegInfo _regB = RegInfo(0, &_core.B, 0x00);    RegInfo _regBIX = RegInfo(0, &_core.B, 0xDD);           RegInfo _regBIY = RegInfo(0, &_core.B, 0xFD);
    RegInfo _regC = RegInfo(1, &_core.C, 0x00);    RegInfo _regCIX = RegInfo(1, &_core.C, 0xDD);           RegInfo _regCIY = RegInfo(1, &_core.C, 0xFD);
    RegInfo _regD = RegInfo(2, &_core.D, 0x00);    RegInfo _regDIX = RegInfo(2, &_core.D, 0xDD);           RegInfo _regDIY = RegInfo(2, &_core.D, 0xFD);
    RegInfo _regE = RegInfo(3, &_core.E, 0x00);    RegInfo _regEIX = RegInfo(3, &_core.E, 0xDD);           RegInfo _regEIY = RegInfo(3, &_core.E, 0xFD);
    RegInfo _regH = RegInfo(4, &_core.H, 0x00);    RegInfo _regHIX = RegInfo(4, &High(_core.IX), 0xDD);    RegInfo _regHIY = RegInfo(4, &High(_core.IY), 0xFD);
    RegInfo _regL = RegInfo(5, &_core.L, 0x00);    RegInfo _regLIX = RegInfo(5, &Low(_core.IX), 0xDD);     RegInfo _regLIY = RegInfo(5, &Low(_core.IY), 0xFD);
    RegInfo _reg0 = RegInfo(6, nullptr,  0x00);    RegInfo _reg0IX = RegInfo(6, nullptr,  0xDD);           RegInfo _reg0IY = RegInfo(6, nullptr,  0xFD);
    RegInfo _regA = RegInfo(7, &_core.A, 0x00);    RegInfo _regAIX = RegInfo(7, &_core.A, 0xDD);           RegInfo _regAIY = RegInfo(7, &_core.A, 0xFD);

    RegInfo _regs[7] = { _regB, _regC, _regD, _regE, _regH, _regL, _regA };

    // Registers A, B, C, D, E, H, L, and the "void" register (code 6) used with undocumented IN, OUT, RES, and SET instructions.
    RegInfo _regsWith0[8] = { _regB, _regC, _regD, _regE, _regH, _regL, _reg0, _regA };

    RegInfo _regsWithPrefixes[21] = {
        _regB, _regBIX, _regBIY,
        _regC, _regCIX, _regCIY,
        _regD, _regDIX, _regDIY,
        _regE, _regEIX, _regEIY,
        _regH, _regHIX, _regHIY,
        _regL, _regLIX, _regLIY,
        _regA, _regAIX, _regAIY
    };

    byte HalfCarryParityInc(byte n, byte f)
    {
        return
            SZ35(n) |
            (((n & 0x0F) == 0x00) ? flagH : 0) |
            ((n == 0x80) ? flagPV : 0) |
            (f & flagC);
    }

    byte HalfCarryParityDec(byte n, byte f)
    {
        return
            SZ35(n) |
            (((n & 0x0F) == 0x0F) ? flagH : 0) |
            ((n == 0x7F) ? flagPV : 0) |
            flagN |
            (f & flagC);
    }

    byte BooleanOperation(byte operation, byte n)
    {
        byte expected = 0;
        switch (operation)
        {
        case 0: expected = _core.A & n; break;
        case 1: expected = _core.A ^ n; break;
        case 2: expected = _core.A | n; break;
        }

        return expected;
    }

    qword Run(int instructionCount)
    {
        _core.EnableLowerRom(false);
        _core.EnableUpperRom(false);

        qword ticksBefore = _core.Ticks();

        for (int i = 0; i < instructionCount; i++)
        {
            _core.RunUntil(_core.Ticks() + 1, 0);
        }

        qword ticksAfter = _core.Ticks();

        return ticksAfter - ticksBefore;
    }

    void CommonChecks(qword ticks, qword expectedTicks, word expectedPC, byte expectedR)
    {
        CommonChecksPrefix(0x00, ticks, expectedTicks, expectedPC, expectedR);
    }

    void CommonChecksPrefix(byte prefix, qword ticks, qword expectedTicks, word expectedPC, byte expectedR)
    {
        if (prefix == 0xDD || prefix == 0xED || prefix == 0xFD)
        {
            expectedTicks += 4;
            expectedPC += 1;
            expectedR += 1;
        }

        ASSERT_EQ(expectedTicks, ticks);
        ASSERT_EQ(expectedPC, _core.PC);
        ASSERT_EQ(expectedR, _core.R);
    }

    void SetInstruction(word addr, byte prefix, byte b0)
    {
        if (prefix != 0x00)
        {
            SetMemory(addr, prefix);
            addr++;
        }

        SetMemory(addr, b0);
    }

    void SetInstruction(word addr, byte prefix, byte b0, byte b1)
    {
        if (prefix != 0x00)
        {
            SetMemory(addr, prefix);
            addr++;
        }

        SetMemory(addr, b0, b1);
    }

    void SetMemory(word addr, byte b0)
    {
        _core.WriteRAM(addr, b0);
    }

    void SetMemory(word addr, byte b0, byte b1)
    {
        _core.WriteRAM(addr, b0);
        _core.WriteRAM(addr + 1, b1);
    }

    void SetMemory(word addr, byte b0, byte b1, byte b2)
    {
        _core.WriteRAM(addr, b0);
        _core.WriteRAM(addr + 1, b1);
        _core.WriteRAM(addr + 2, b2);
    }

    void SetMemory(word addr, byte b0, byte b1, byte b2, byte b3)
    {
        _core.WriteRAM(addr, b0);
        _core.WriteRAM(addr + 1, b1);
        _core.WriteRAM(addr + 2, b2);
        _core.WriteRAM(addr + 3, b3);
    }

    bool Parity(byte b)
    {
        byte oneBits = 0;
        for (int i = 0; i < 8; i++)
        {
            if (((b >> i) & 0x01) == 0x01)
            {
                oneBits++;
            }
        }

        return (oneBits % 2) == 0;
    }

    byte SZP35(byte b)
    {
        return SZ35(b) | (Parity(b) ? flagPV : 0);
    }

    byte SZ35(byte b)
    {
        return
            (b & (flag3 | flag5)) |
            ((b == 0) ? flagZ : 0) |
            (((b & 0x80) != 0) ? flagS : 0);
    }

    void CALLccnn(byte opcode, byte flag, bool positive)
    {
        for (byte f : flagBytes)
        {
            for (word sp : testAddresses)
            {
                word pc = sp + 0x0010;;
                word nn = 0x5678;

                _core.Init();
                _core.PC = pc;
                SetMemory(pc, opcode, Low(nn), High(nn));
                _core.SP = sp;
                _core.F = f;

                qword ticks = Run(1);

                bool called = positive ^ ((flag & f) == 0);

                ASSERT_EQ(f, _core.F);
                if (called)
                {
                    word pushedPC = (pc + 0x0003);

                    ASSERT_EQ(Low(pushedPC), _core.ReadRAM(_core.SP));
                    ASSERT_EQ(High(pushedPC), _core.ReadRAM(_core.SP + 1));
                    ASSERT_EQ(_core.SP, (word) (sp - 0x0002));
                    CommonChecks(ticks, 19, nn, 0x01);
                }
                else
                {
                    ASSERT_EQ(_core.SP, sp);
                    CommonChecks(ticks, 11, pc + 3, 0x01);
                }
            }
        }
    }

    void JPcc(byte opcode, byte flag, bool positive)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                _core.Init();
                _core.PC = addr + 0x0010;
                SetMemory(_core.PC, opcode, Low(addr), High(addr));
                _core.F = f;
                
                qword ticks = Run(1);

                word expectedPC = (positive ^ ((flag & f) == 0)) ? addr : (addr + 0x0013);
                CommonChecks(ticks, 11, expectedPC, 0x01);
            }
        }
    }

    void JRcc(byte opcode, byte flag, bool positive)
    {
        for (byte f : flagBytes)
        {
            for (offset o : testOffsets)
            {
                word addr = 0x1234;
                _core.Init();
                _core.PC = addr;
                SetMemory(_core.PC, opcode, o);
                _core.F = f;

                qword ticks = Run(1);

                word expectedPC = addr + 2;
                byte expectedTicks = 7;
                if (positive ^ ((flag & f) == 0))
                {
                    expectedPC = (addr + 2 + o);
                    expectedTicks = 12;
                }

                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, expectedTicks, expectedPC, 0x01);
            }
        }
    }

    void BITbr()
    {
        for (byte f : flagBytes)
        {
            for (RegInfo reg : _regs)
            {
                for (byte n : testBytes)
                {
                    for (byte b : Range<byte>(0x00, 0x07))
                    {
                        _core.Init();
                        SetMemory(0x0000, 0xCB, 0x40 | (b << 3) | reg._code);
                        *reg._pReg = n;
                        _core.F = f;

                        qword ticks = Run(1);

                        byte expected = n & (1 << b);
                        byte expectedF =
                            (expected & (flag3 | flag5 | flagS)) |
                            flagH |
                            ((expected == 0) ? (flagZ | flagPV) : 0) |
                            (f & flagC);
                        ASSERT_EQ(n, *reg._pReg);
                        ASSERT_EQ(_core.F, expectedF);

                        CommonChecks(ticks, 8, 0x0002, 0x02);
                    }
                }
            }
        }
    }

    void BITbHLInd()
    {
        for (byte f : flagBytes)
        {
            for (word addr : Range<word>(0x3FFF, 0x4000))
            {
                for (byte n : testBytes)
                {
                    for (byte b : Range<byte>(0x00, 0x07))
                    {
                        _core.Init();
                        SetMemory(0x0000, 0xCB, 0x46 | (b << 3));
                        _core.HL = addr;
                        _core.F = f;
                        SetMemory(addr, n);

                        qword ticks = Run(1);

                        byte expected = n & (1 << b);
                        ASSERT_EQ(_core.F, (expected & (flag3 | flag5 | flagS)) | flagH | ((expected == 0) ? (flagZ | flagPV) : 0) | (f & flagC));
                        CommonChecks(ticks, 12, 0x0002, 0x02);
                    }
                }
            }
        }
    }

    void BITbIndex()
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (byte r : Range<byte>(0, 7))
                {
                    for (word addr : testAddresses)
                    {
                        for (offset o : testOffsets)
                        {
                            for (byte n : testBytes)
                            {
                                for (byte b : Range<byte>(0x00, 0x07))
                                {
                                    word pc = addr + 0x100;
                                    _core.Init();
                                    _core.PC = pc;
                                    SetMemory(pc, indexInfo._prefix, 0xCB, o, 0x40 | (b << 3) | r);
                                    SetMemory(addr + o, n);
                                    *indexInfo._pReg = addr;
                                    _core.F = f;

                                    qword ticks = Run(1);

                                    byte expected = n & (1 << b);
                                    ASSERT_EQ(_core.F, (expected & (flag3 | flag5 | flagS)) | flagH | ((expected == 0) ? (flagZ | flagPV) : 0) | (f & flagC));
                                    CommonChecks(ticks, 24, pc + 4, 0x02);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void POPrr(word& rr, byte opcode1, byte opcode2)
    {
        for (byte f : flagBytes)
        {
            for (word sp : testAddresses)
            {
                sp++;
                word pc = sp + 10;
                word nn = 0x1234;

                _core.Init();
                _core.PC = pc;
                rr = 0;
                SetMemory(_core.PC, opcode1, opcode2);
                SetMemory(sp, Low(nn), High(nn));
                _core.SP = sp;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(rr, nn);
                ASSERT_EQ(_core.SP, (word)(sp + 2));
                
                if (&rr != &_core.AF)
                {
                    ASSERT_EQ(f, _core.F);
                }

                CommonChecksPrefix(opcode1, ticks, 11, pc + 1, 0x01);
            }
        }
    }

    void RET()
    {
        for (byte f : flagBytes)
        {
            for (word sp : testAddresses)
            {
                sp++;
                word pc = sp + 10;
                word nn = 0x1234;

                _core.Init();
                _core.PC = pc;
                SetMemory(_core.PC, 0xC9);
                SetMemory(sp, Low(nn), High(nn));
                _core.SP = sp;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(f, _core.F);
                ASSERT_EQ(_core.SP, (word)(sp + 2));
                CommonChecks(ticks, 11, nn, 0x01);
            }
        }
    }

    void RETIN(byte opcode)
    {   
        for (byte f : flagBytes)
        {
            for (bool iff1 : { false, true })
            {
                for (bool iff2 : { false, true })
                {
                    for (word sp : testAddresses)
                    {
                        sp++;
                        word pc = sp + 10;
                        word nn = 0x1234;

                        _core.Init();
                        _core.PC = pc;
                        SetMemory(_core.PC, 0xED, opcode);
                        SetMemory(sp, Low(nn), High(nn));
                        _core.SP = sp;
                        _core.F = f;
                        _core._iff1 = iff1;
                        _core._iff2 = iff2;

                        qword ticks = Run(1);

                        ASSERT_EQ(f, _core.F);
                        ASSERT_EQ(_core.SP, (word)(sp + 2));
                        ASSERT_EQ(_core._iff1, iff2);
                        ASSERT_EQ(_core._iff2, iff2);
                        CommonChecks(ticks, 15, nn, 0x02);
                    }
                }
            }
        }
    }

    void RETcc(byte opcode, byte flag, bool positive)
    {
        for (byte f : flagBytes)
        {
            for (word sp : testAddresses)
            {
                sp++;
                word pc = sp + 10;
                word nn = 0x1234;

                _core.Init();
                _core.PC = pc;
                SetMemory(_core.PC, opcode);
                SetMemory(sp, Low(nn), High(nn));
                _core.SP = sp;
                _core.F = f;

                qword ticks = Run(1);

                bool returned = positive ^ ((flag & f) == 0);

                ASSERT_EQ(f, _core.F);

                if (returned)
                {
                    ASSERT_EQ(_core.SP, (word)(sp + 2));
                    CommonChecks(ticks, 15, nn, 0x01);
                }
                else
                {
                    ASSERT_EQ(_core.SP, sp);
                    CommonChecks(ticks, 5, pc + 1, 0x01);
                }
            }
        }
    }

    void PUSHrr(word& rr, byte opcode1, byte opcode2)
    {
        for (byte f : flagBytes)
        {
            for (word sp : testAddresses)
            {
                sp++;
                word pc = sp + 10;
                word nn = 0x1234;

                _core.Init();
                _core.PC = pc;
                rr = nn;
                SetMemory(_core.PC, opcode1, opcode2);
                _core.SP = sp;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(Low(rr), _core.ReadRAM(_core.SP));
                ASSERT_EQ(High(rr), _core.ReadRAM(_core.SP + 1));
                ASSERT_EQ(_core.SP, (word)(sp - 2));
                ASSERT_EQ(f, _core.F);
                CommonChecksPrefix(opcode1, ticks, 15, pc + 1, 0x01);
            }
        }
    }

    void ANDORXORr(byte o)
    {
        for (byte f : flagBytes)
        {
            for (byte a : testBytes)
            {
                for (RegInfo reg : _regs)
                {
                    for (byte n : testBytes)
                    {
                        _core.Init();
                        SetMemory(0x0000, 0xA0 | (o << 3) | reg._code);
                        _core.F = f;
                        *reg._pReg = n;
                        _core.A = a;

                        byte expected = BooleanOperation(o, *reg._pReg);

                        qword ticks = Run(1);

                        ASSERT_EQ(expected, _core.A);
                        ASSERT_EQ(_core.F, (expected & (flag3 | flag5)) | ((o == 0) ? flagH : 0) | (Parity(expected) ? flagPV : 0) | ((expected == 0) ? flagZ : 0) | ((expected & 0x80) ? flagS : 0));
                        CommonChecks(ticks, 4, 0x0001, 0x01);
                    }
                }
            }
        }
    }

    void ANDORXORn(byte o)
    {
        for (byte f : flagBytes)
        {
            for (byte a : testBytes)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    SetMemory(0x0000, 0xE6 | (o << 3), n);
                    _core.F = f;
                    _core.A = a;

                    byte expected = BooleanOperation(o, n);

                    qword ticks = Run(1);

                    byte expectedF = SZP35(expected) | ((o == 0) ? flagH : 0);

                    ASSERT_EQ(_core.A, expected);
                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecks(ticks, 7, 0x0002, 0x01);
                }
            }
        }
    }

    void ANDORXORHLInd(byte o)
    {
        for (byte f : flagBytes)
        {
            for (byte a : testBytes)
            {
                for (word hl : testAddresses)
                {
                    for (byte n : testBytes)
                    {
                        _core.Init();
                        _core.PC = hl + 0x10;
                        SetMemory(_core.PC, 0xA6 | (o << 3));
                        SetMemory(hl, n);
                        _core.HL = hl;
                        _core.F = f;
                        _core.A = a;

                        byte expected = BooleanOperation(o, n);

                        qword ticks = Run(1);

                        byte expectedF = SZP35(expected) | ((o == 0) ? flagH : 0);

                        ASSERT_EQ(_core.A, expected);
                        ASSERT_EQ(_core.F, expectedF);
                        CommonChecks(ticks, 7, hl + 0x11, 0x01);
                    }
                }
            }
        }
    }

    void ANDORXORIndex(byte o)
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (word addr : Range<word>(0x3FFF, 0x4000))
                {
                    for (offset offset : testOffsets)
                    {
                        for (byte a : testBytes)
                        {
                            for (byte n : testBytes)
                            {
                                _core.Init();
                                SetMemory(0x0000, indexInfo._prefix, 0xA6 | (o << 3), offset);
                                SetMemory(addr + offset, n);
                                *indexInfo._pReg = addr;
                                _core.F = f;
                                _core.A = a;

                                byte expected = BooleanOperation(o, n);

                                qword ticks = Run(1);

                                byte expectedF = SZP35(expected) | ((o == 0) ? flagH : 0);

                                ASSERT_EQ(_core.A, expected);
                                ASSERT_EQ(_core.F, expectedF);
                                CommonChecks(ticks, 19, 0x0003, 0x02);
                            }
                        }
                    }
                }
            }
        }
    }

    void DECr()
    {
        for (RegInfo reg : _regsWithPrefixes)
        {
            for (byte f : flagBytes)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    byte opcode = 0x05 | (reg._code << 3);
                    SetInstruction(0x0000, reg._prefix, opcode);

                    _core.F = f;
                    *reg._pReg = n;

                    qword ticks = Run(1);

                    byte expected = n - 1;
                    byte expectedF = HalfCarryParityDec(expected, f);

                    CommonChecksPrefix(reg._prefix, ticks, 4, 0x0001, 0x01);

                    ASSERT_EQ(expected, *reg._pReg);
                    ASSERT_EQ(_core.F, expectedF);
                }
            }
        }
    }

    void INCr()
    {
        for (RegInfo reg : _regsWithPrefixes)
        {
            for (byte f : flagBytes)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    byte opcode = 0x04 | (reg._code << 3);
                    SetInstruction(0x0000, reg._prefix, opcode);

                    _core.F = f;
                    *reg._pReg = n;

                    qword ticks = Run(1);

                    byte expected = n + 1;
                    byte expectedF = HalfCarryParityInc(expected, f);

                    CommonChecksPrefix(reg._prefix, ticks, 4, 0x0001, 0x01);
                    ASSERT_EQ(expected, *reg._pReg);
                    ASSERT_EQ(_core.F, expectedF);
                }
            }
        }
    }

    void LDIRA(byte opcode, byte& r)
    {
        for (byte f : flagBytes)
        {
            for (byte ir : testBytes)
            {
                _core.Init();
                _core.A = ir;
                SetMemory(0x0000, 0xED, opcode);
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(r, ir);
                ASSERT_EQ(_core.F, f);
                ASSERT_EQ(9, ticks);
                ASSERT_EQ(0x0002, _core.PC);
                if (&r != &_core.R)
                {
                    ASSERT_EQ(_core.R, 0x02);
                }
            }
        }
    }

    void LDAIR(byte opcode, byte& r)
    {
        for (byte f : flagBytes)
        {
            for (byte ir : allBytes)
            {
                for (bool iff2 : { false, true })
                {
                    _core.Init();
                    r = ir;
                    _core._iff2 = iff2;
                    SetMemory(0x0000, 0xED, opcode);
                    _core.F = f;

                    qword ticks = Run(1);

                    byte expectedIR = (&r == &_core.R) ? ((ir & 0x80) | ((ir + 2) & 0x7F)) : ir;
                    byte expectedF = (Bit(expectedIR, 7) ? flagS : 0) |
                                     ((expectedIR == 0) ? flagZ : 0) |
                                     (iff2 ? flagPV : 0) |
                                     (f & flagC) |
                                     (expectedIR & (flag3 | flag5));

                    if (&r == &_core.R)
                    {
                        ASSERT_EQ(_core.R, expectedIR);
                    }
                    else
                    {
                        ASSERT_EQ(_core.R, 0x02);
                    }

                    ASSERT_EQ(expectedIR, _core.A);
                    ASSERT_EQ(_core.F, expectedF);
                    ASSERT_EQ(9, ticks);
                    ASSERT_EQ(0x0002, _core.PC);
                }
            }
        }
    }

    void LDHLr()
    {
        for (byte f : flagBytes)
        {
            for (RegInfo reg : _regs)
            {
                for (word addr : testAddresses)
                {
                    byte b = 0xFE;
                    _core.Init();
                    _core.PC = addr + 0x0010;
                    byte opcode = 0x70 | reg._code;
                    SetMemory(_core.PC, opcode);
                    *reg._pReg = b;
                    _core.HL = addr;
                    byte expected = *reg._pReg;
                    _core.F = f;

                    qword ticks = Run(1);

                    ASSERT_EQ(expected, _core.ReadRAM(addr));
                    ASSERT_EQ(f, _core.F);
                    CommonChecksPrefix(0x00, ticks, 7, addr + 0x0011, 0x01);
                }
            }
        }
    }

    void LDrHL()
    {
        for (byte f : flagBytes)
        {
            for (RegInfo reg : _regs)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    byte opcode = 0x46 | (reg._code << 3);
                    SetMemory(0x0000, opcode);
                    _core.HL = 0x1234;
                    SetMemory(_core.HL, n);
                    _core.F = f;

                    qword ticks = Run(1);

                    ASSERT_EQ(n, *reg._pReg);
                    ASSERT_EQ(f, _core.F);
                    CommonChecks(ticks, 7, 0x0001, 0x01);
                }
            }
        }
    }

    void LDArr(byte opcode, word& rr)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                byte b = 0xFE;
                _core.Init();
                _core.PC = addr + 0x0010;
                SetMemory(_core.PC, opcode);
                rr = addr;
                SetMemory(addr, b);
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(b, _core.A);
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 7, addr + 0x11, 0x01);
            }
        }
    }

    void LDrrA(byte opcode, word& rr)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                byte b = 0xFE;
                _core.Init();
                _core.PC = addr + 0x0010;
                SetMemory(_core.PC, opcode);
                rr = addr;
                _core.A = b;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(b, _core.ReadRAM(addr));
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 7, addr + 0x11, 0x01);
            }
        }
    }

    void LDIdxr()
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (RegInfo reg : _regs)
                {
                    for (word addr : Range<word>(0x3FFF, 0x4000))
                    {
                        for (offset o : testOffsets)
                        {
                            byte b = 0xFE;
                            _core.Init();
                            _core.PC = addr + 0x0100;
                            *indexInfo._pReg = addr;
                            *reg._pReg = b;
                            byte opcode = 0x70 | reg._code;
                            SetMemory(_core.PC, indexInfo._prefix, opcode, o);
                            _core.F = f;

                            qword ticks = Run(1);

                            ASSERT_EQ(b, _core.ReadRAM(addr + o));
                            ASSERT_EQ(f, _core.F);
                            CommonChecks(ticks, 19, addr + 0x0103, 0x02);
                        }
                    }
                }
            }
        }
    }

    void LDrIdx()
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (RegInfo reg : _regs)
                {
                    for (word addr : testAddresses)
                    {
                        for (offset o : testOffsets)
                        {
                            byte b = 0xFE;
                            _core.Init();
                            _core.PC = addr + 0x0100;
                            *indexInfo._pReg = addr;
                            byte opcode = 0x46 | (reg._code << 3);
                            SetMemory(_core.PC, indexInfo._prefix, opcode, o);
                            SetMemory((addr + o), b);
                            _core.F = f;

                            qword ticks = Run(1);

                            ASSERT_EQ(b, *reg._pReg);
                            ASSERT_EQ(f, _core.F);
                            CommonChecks(ticks, 19, addr + 0x0103, 0x02);
                        }
                    }
                }
            }
        }
    }

    void LDnndd(byte opcode, word& rr)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                word w = 0xFEFF;
                _core.Init();
                _core.PC = addr + 0x0010;
                SetMemory(_core.PC, 0xED, opcode, Low(addr), High(addr));
                rr = w;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(Low(w), _core.ReadRAM(addr));
                ASSERT_EQ(High(w), _core.ReadRAM((word) (addr + 1)));
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 23, addr + 0x14, 0x02);
            }
        }
    }

    void LDrrnn(byte opcode, word& rr)
    {
        for (byte f : flagBytes)
        {
            word nn = 0x1234;
            _core.Init();
            SetMemory(0x0000, opcode, Low(nn), High(nn));
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(nn, rr);
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 11, 0x0003, 0x01);
        }
    }

    void LDrrnnInd(byte opcode, word& rr)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                word w = 0xFEFF;
                _core.Init();
                _core.PC = addr + 0x0010;
                SetMemory(_core.PC, 0xED, opcode, Low(addr), High(addr));
                SetMemory(addr, Low(w), High(w));
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(w, rr);
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 23, addr + 0x14, 0x02);
            }
        }
    }

    void INCDECrr(word& rr, word incAmount, byte opcode1, byte opcode2 = 0)
    {
        for (byte f : flagBytes)
        {
            for (word nn : {0x0000, 0x0001, 0xFFFE, 0xFFFF})
            {
                _core.Init();
                SetMemory(_core.PC, opcode1, opcode2);
                rr = nn;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(rr, (word) (nn + incAmount));
                ASSERT_EQ(f, _core.F);
                CommonChecksPrefix(opcode1, ticks, 6, 0x0001, 0x01);
            }
        }
    }

    void EXSPrr(word& rr, byte opcode1)
    {
        for (byte f : flagBytes)
        {
            for (byte n : allBytes)
            {
                for (word sp : { 0x3FFF, 0x4000 })
                {
                    _core.Init();
                    _core.PC = sp + 0x10;
                    SetInstruction(_core.PC, opcode1, 0xE3);
                    _core.SP = sp;
                    SetMemory(sp, n, n + 1);
                    Low(rr) = n + 2;
                    High(rr) = n + 3;
                    _core.F = f;

                    qword ticks = Run(1);

                    ASSERT_EQ(_core.F, f);
                    ASSERT_EQ(Low(rr), n);
                    ASSERT_EQ(High(rr), (byte) (n + 1));
                    ASSERT_EQ(_core.ReadRAM(sp), (byte) (n + 2));
                    ASSERT_EQ(_core.ReadRAM(sp + 1), (byte) (n + 3));
                    CommonChecksPrefix(opcode1, ticks, 21, sp + 0x11, 0x01);
                }
            }
        }
    }

    byte ADDExpectedFlags(byte operand1, byte operand2, bool carry)
    {
        signed short res = 0;
        word ures = 0;
        bool halfCarry = false;
        res = ((char)operand1) + ((char)operand2) + (carry ? 1 : 0);
        ures = operand1 + operand2 + (carry ? 1 : 0);
        halfCarry = (((operand1 & 0x0F) + (operand2 & 0x0F) + (carry ? 1 : 0)) >= 0x10);

        byte expectedA = (byte)res;

        bool expectedVFlag = (res > 127) | (res < -128);
        byte expectedF = (Bit(expectedA, 7) ? flagS : 0) |
            ((expectedA == 0x00) ? flagZ : 0) |
            (expectedA & (flag3 | flag5)) |
            (halfCarry ? flagH : 0) |
            (expectedVFlag ? flagPV : 0) |
            (Bit(ures, 8) ? flagC : 0);

        return expectedF;
    }

    byte SUBExpectedFlags(byte operand1, byte operand2, bool carry)
    {
        signed short res = 0;
        word ures = 0;
        bool halfCarry = false;
        res = ((char)operand1) - ((char)operand2) - (carry ? 1 : 0);
        ures = (byte)operand1 - (byte)operand2 - (carry ? 1 : 0);
        halfCarry = ((operand1 & 0x0F) < ((operand2 & 0x0F) + (carry ? 1 : 0)));

        byte expectedA = (byte)res;

        bool expectedVFlag = (res > 127) | (res < -128);
        byte expectedF = (Bit(expectedA, 7) ? flagS : 0) |
            ((expectedA == 0x00) ? flagZ : 0) |
            (expectedA & (flag3 | flag5)) |
            (halfCarry ? flagH : 0) |
            (expectedVFlag ? flagPV : 0) |
            flagN |
            (Bit(ures, 8) ? flagC : 0);

        return expectedF;
    }

    void CPr()
    {
        for (byte f : flagBytes)
        {
            for (RegInfo reg : _regs)
            {
                for (byte n : testBytes)
                {
                    for (byte a : testBytes)
                    {
                        _core.Init();
                        byte opcode = 0xB8 | reg._code;
                        SetMemory(0x0000, opcode);
                        *reg._pReg = n;
                        _core.A = a;
                        _core.F = f;

                        byte expectedF = SUBExpectedFlags(_core.A, *reg._pReg, false);
                        expectedF &= (~(flag3 | flag5));
                        expectedF |= (*reg._pReg & (flag3 | flag5));

                        qword ticks = Run(1);

                        ASSERT_EQ(_core.F, expectedF);
                        CommonChecks(ticks, 4, 0x0001, 0x01);
                    }
                }
            }
        }
    }

    void CPn()
    {
        for (byte f : flagBytes)
        {
            for (byte n : testBytes)
            {
                for (byte a : testBytes)
                {
                    _core.Init();
                    SetMemory(0x0000, 0xFE, n);
                    _core.A = a;
                    _core.F = f;

                    byte expectedF = SUBExpectedFlags(_core.A, n, false);
                    expectedF &= (~(flag3 | flag5));
                    expectedF |= (n & (flag3 | flag5));

                    qword ticks = Run(1);

                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecks(ticks, 7, 0x0002, 0x01);
                }
            }
        }
    }

    void CPHLInd()
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                for (byte n : testBytes)
                {
                    for (byte a : testBytes)
                    {
                        _core.Init();
                        _core.PC = addr + 0x0010;
                        SetMemory(_core.PC, 0xBE);
                        SetMemory(addr, n);
                        _core.A = a;
                        _core.HL = addr;
                        _core.F = f;

                        byte expectedF = SUBExpectedFlags(_core.A, n, false);
                        expectedF &= (~(flag3 | flag5));
                        expectedF |= (n & (flag3 | flag5));

                        qword ticks = Run(1);

                        ASSERT_EQ(_core.F, expectedF);
                        CommonChecks(ticks, 7, addr + 0x11, 0x01);
                    }
                }
            }
        }
    }

    void CPBlock(bool repeat, bool decrement)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                for (word bc : Range<word>(0x0000, 0x0001))
                {
                    for (byte n : testBytes)
                    {
                        for (byte a : testBytes)
                        {
                            _core.Init();
                            _core.PC = addr + 0x100;
                            byte opcode = 0xa1 | (repeat ? 0x10 : 0x00) | (decrement ? 0x08 : 0x00);
                            SetMemory(_core.PC, 0xED, opcode);
                            SetMemory(addr, n);
                            _core.A = a;
                            _core.HL = addr;
                            _core.BC = bc;
                            _core.F = f;

                            byte expectedF = SUBExpectedFlags(_core.A, n, false);
                            expectedF = (expectedF & (~(flagPV))) | ((bc != 1) ? flagPV : 0);
                            byte res = a - n - (((expectedF & flagH) != 0) ? 1 : 0);
                            expectedF &= ~(flag3 | flag5);
                            expectedF |= (res & (flag3 | flag5));
                            expectedF &= ~flagC;
                            expectedF |= (f & flagC);


                            qword ticks = Run(1);

                            ASSERT_EQ((repeat && (bc != 0x0001) && (a != n)) ? 21 : 16, ticks);
                            ASSERT_EQ((repeat && (bc != 0x0001) && (a != n)) ? ((word) (addr + 0x0100)) : ((word) (addr + 0x0102)), _core.PC);
                            ASSERT_EQ(_core.F, expectedF);
                            ASSERT_EQ(_core.BC, (word) (bc - 1));
                            ASSERT_EQ(_core.HL, (word) (decrement ? (addr - 1) : (addr + 1)));
                            ASSERT_EQ(_core.R, 0x02);
                        }
                    }
                }
            }
        }
    }

    void CPIndex()
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (word addr : testAddresses)
                {
                    for (offset o : testOffsets)
                    {
                        for (byte n : testBytes)
                        {
                            for (byte a : testBytes)
                            {
                                _core.Init();
                                _core.PC = addr + 0x0100;
                                SetMemory(_core.PC, indexInfo._prefix, 0xBE, o);
                                SetMemory(addr + o, n);
                                _core.A = a;
                                *indexInfo._pReg = addr;
                                _core.F = f;

                                byte expectedF = SUBExpectedFlags(_core.A, n, false);
                                expectedF &= (~(flag3 | flag5));
                                expectedF |= (n & (flag3 | flag5));

                                qword ticks = Run(1);

                                ASSERT_EQ(_core.F, expectedF);
                                CommonChecks(ticks, 19, addr + 0x0103, 0x02);
                            }
                        }
                    }
                }
            }
        }
    }

    void LDBlock(bool repeat, bool decrement)
    {
        for (byte f : flagBytes)
        {
            for (byte a : allBytes)
            {
                for (word bc : Range<word>(0x0000, 0x0001))
                {
                    byte b = 0xFE;
                    word de = 0x1234;
                    word hl = 0xFFFF;

                    word pc = 0x0000;

                    byte opcode = 0xA0 | (decrement ? 0x08 : 0x00) | (repeat ? 0x10 : 0x00);
                    _core.Init();
                    SetMemory(pc, 0xED, opcode);
                    SetMemory(de, 0x00);
                    SetMemory(hl, b);
                    _core.A = a;
                    _core.F = f;
                    _core.BC = bc;
                    _core.DE = de;
                    _core.HL = hl;
                    _core.PC = pc;

                    qword ticks = Run(1);

                    if (repeat && bc != 0x0001)
                    {
                        ASSERT_EQ(22, ticks);
                        ASSERT_EQ(0x0000, _core.PC);
                    }
                    else
                    {
                        ASSERT_EQ(17, ticks);
                        ASSERT_EQ(0x0002, _core.PC);
                    }

                    ASSERT_EQ((word)(hl + (decrement ? -1 : 1)), _core.HL);
                    ASSERT_EQ((word)(de + (decrement ? -1 : 1)), _core.DE);
                    ASSERT_EQ((word)(bc - 1), _core.BC);
                    ASSERT_EQ(b, _core.ReadRAM(de));

                    byte i = _core.A + b;
                    ASSERT_EQ(_core.F, (f & (flagS | flagZ | flagC)) | (i & flag3) | (((i & 0x02) != 0) ? flag5: 0) | ((bc != 0x0001) ? flagPV : 0));
                    ASSERT_EQ(_core.R, 0x02);
                }
            }
        }
    }

    void ADDADCSUBSBCn(bool subtraction, bool carry)
    {
        for (byte f : flagBytes)
        {
            for (byte n : testBytes)
            {
                for (byte a : testBytes)
                {
                    byte opcode = 0xC6 | (carry ? 0x08 : 0x00) | (subtraction ? 0x10 : 0x00);

                    _core.Init();
                    SetMemory(0x0000, opcode, n);
                    _core.F = f;
                    _core.A = a;
                    bool applyCarry = (carry && ((f & flagC) != 0));
                    word subtrahend = n + (applyCarry ? 1 : 0);
                    byte expectedA = a + (subtraction ? -subtrahend : subtrahend);
                    byte expectedF = subtraction ? SUBExpectedFlags(a, n, applyCarry) : ADDExpectedFlags(a, n, applyCarry);

                    qword ticks = Run(1);

                    ASSERT_EQ(_core.F, expectedF);
                    ASSERT_EQ(_core.A, expectedA);
                    CommonChecks(ticks, 7, 0x0002, 0x01);
                }
            }
        }
    }

    void ADDADCSUBSBCr(bool subtraction, bool carry)
    {
        for (byte f : flagBytes)
        {
            for (byte n : testBytes)
            {
                for (byte a : testBytes)
                {
                    for (RegInfo reg : _regs)
                    {
                        byte opcode = 0x80 | reg._code | (carry ? 0x08 : 0x00) | (subtraction  ? 0x10 : 0x00);

                        _core.Init();
                        SetMemory(0x0000, opcode);
                        _core.F = f;
                        *reg._pReg = n;
                        _core.A = a;
                        byte s = *reg._pReg;
                        bool applyCarry = (carry && ((f & flagC) != 0));
                        word subtrahend = (short) (char) s + (applyCarry ? 1 : 0);
                        byte expectedA = _core.A + (subtraction ? -subtrahend : subtrahend);
                        byte originalA = _core.A;
                        byte expectedF = subtraction ? SUBExpectedFlags(_core.A, s, applyCarry) : ADDExpectedFlags(_core.A, s, applyCarry);

                        qword ticks = Run(1);

                        ASSERT_EQ(_core.F, expectedF);
                        ASSERT_EQ(_core.A, expectedA);
                        CommonChecks(ticks, 4, 0x0001, 0x01);
                    }
                }
            }
        }
    }

    void ADDADCSUBSBCHLInd(bool subtraction, bool carry)
    {
        for (byte f : flagBytes)
        {
            for (word hl : testAddresses)
            {
                for (byte n : testBytes)
                {
                    for (byte a : testBytes)
                    {
                        byte opcode = 0x86 | (carry ? 0x08 : 0x00) | (subtraction ? 0x10 : 0x00);

                        _core.Init();
                        _core.PC = hl + 0x0010;
                        SetMemory(_core.PC, opcode);
                        _core.HL = hl;
                        SetMemory(hl, n);
                        _core.F = f;
                        _core.A = a;
                        bool applyCarry = (carry && ((f & flagC) != 0));
                        word subtrahend = n + (applyCarry ? 1 : 0);
                        byte expectedA = a + (subtraction ? -subtrahend : subtrahend);
                        byte expectedF = subtraction ? SUBExpectedFlags(a, n, applyCarry) : ADDExpectedFlags(a, n, applyCarry);

                        qword ticks = Run(1);

                        ASSERT_EQ(_core.F, expectedF);
                        ASSERT_EQ(_core.A, expectedA);
                        CommonChecks(ticks, 7, hl + 0x11, 0x01);
                    }
                }
            }
        }
    }

    void ADDADCSUBSBCIndex(bool subtraction, bool carry)
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (word addr : Range<word>(0x3FFF, 0x4000))
                {
                    for (offset o : testOffsets)
                    {
                        for (byte n : testBytes)
                        {
                            for (byte a : testBytes)
                            {
                                byte opcode = 0x86 | (carry ? 0x08 : 0x00) | (subtraction ? 0x10 : 0x00);

                                _core.Init();
                                SetMemory(0x0000, indexInfo._prefix, opcode, o);
                                *indexInfo._pReg = addr;
                                SetMemory(addr + o, n);
                                _core.F = f;
                                _core.A = a;
                                bool applyCarry = (carry && ((f & flagC) != 0));
                                word subtrahend = n + (applyCarry ? 1 : 0);
                                byte expectedA = a + (subtraction ? -subtrahend : subtrahend);

                                qword ticks = Run(1);

                                byte expectedF = subtraction ? SUBExpectedFlags(a, n, applyCarry) : ADDExpectedFlags(a, n, applyCarry);

                                ASSERT_EQ(_core.F, expectedF);
                                ASSERT_EQ(_core.A, expectedA);
                                CommonChecks(ticks, 19, 0x0003, 0x02);
                            }
                        }
                    }
                }
            }
        }
    }

    void ADCADDSBCSUBHLrr(byte opcode1, byte opcode2, word& rr, word& hl, bool carry, bool subtraction, byte expectedTicks, word expectedPCOffset)
    {
        for (byte f : flagBytes)
        {
            for (word mm : testAddresses)
            {
                for (word nn : Range<word>(0x7FFF, 0x8000))
                {
                    if ((&rr == &hl) && (mm != nn))
                    {
                        continue;
                    }

                    // Setup
                    _core.Init();
                    rr = nn;
                    hl = mm;
                    _core.F = f;
                    SetMemory(0x0000, opcode1, opcode2);

                    // Act
                    qword ticks = Run(1);


                    // Verify
                    dword addend = nn + ((carry & ((f & flagC) != 0)) ? 1 : 0);

                    bool halfCarry = false;
                    if (subtraction)
                    {
                        halfCarry = (((dword)(mm & 0xFFF)) < (addend & 0xFFF));
                    }
                    else
                    {
                        halfCarry = (((mm & 0xFFF) + (addend & 0xFFF)) >= 0x1000);
                    }

                    signed int sRes = ((signed short)mm);
                    if (subtraction)
                    {
                        sRes = sRes - ((signed short)nn) - ((carry && ((f & flagC) != 0)) ? 1 : 0);
                    }
                    else
                    {
                        sRes = sRes + ((signed short)nn) + ((carry && ((f & flagC) != 0)) ? 1 : 0);
                    }

                    dword expectedHL = subtraction ? mm - addend : mm + addend;
                    byte expectedSZPV = carry ?
                        (((expectedHL & 0x8000) != 0) ? flagS : 0) |
                        ((((word)expectedHL) == 0) ? flagZ : 0) |
                        (((sRes > 32767) | (sRes < -32768)) ? flagPV : 0)
                        : (f & (flagS | flagZ | flagPV));

                    byte expectedF = ((expectedHL >= 0x10000) ? flagC : 0) |
                        (halfCarry ? flagH : 0) |
                        ((expectedHL >> 8) & (flag3 | flag5)) |
                        expectedSZPV |
                        (subtraction ? flagN : 0);

                    ASSERT_EQ(hl, (word) expectedHL);
                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecksPrefix(opcode1, ticks, 11, 0x0001, 0x01);
                }
            }
        }
    }

    void Shift(bool left, bool logical)
    {
        for (byte f : flagBytes)
        {
            for (RegInfo reg : _regs)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    byte opcode = 0x20 | (logical ? 0x10 : 0x00) | reg._code | (left ? 0x00 : 0x08);
                    SetMemory(0x0000, 0xCB, opcode);
                    *reg._pReg = n;
                    _core.F = f;

                    qword ticks = Run(1);

                    byte expected = 0;
                    byte expectedF = 0;
                    if (left)
                    {
                        expected = (n << 1) | (logical ? 0x01 : 0x00);
                        expectedF = SZP35(expected) | (Bit(n, 7) ? flagC : 0);
                    }
                    else
                    {
                        expected = (n >> 1) | (logical ? 0x00 : (n & 0x80));
                        expectedF = SZP35(expected) | (Bit(n, 0) ? flagC : 0);
                    }

                    ASSERT_EQ(*reg._pReg, expected);
                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecks(ticks, 8, 0x0002, 0x02);
                }
            }
        }
    }

    void ShiftHLInd(bool left, bool logical)
    {
        for (byte f : flagBytes)
        {
            for (word hl : testAddresses)
            {
                for (byte n : testBytes)
                {
                    word pc = hl + 0x100;
                    _core.Init();
                    byte opcode = 0x26 | (logical ? 0x10 : 0x00) | (left ? 0x00 : 0x08);
                    SetMemory(pc, 0xCB, opcode);
                    SetMemory(hl, n);
                    _core.PC = pc;
                    _core.HL = hl;
                    _core.F = f;

                    qword ticks = Run(1);

                    byte expected = 0;
                    byte expectedF = 0;
                    if (left)
                    {
                        expected = (n << 1) | (logical ? 0x01 : 0x00);
                        expectedF = SZP35(expected) | (Bit(n, 7) ? flagC : 0);
                    }
                    else
                    {
                        expected = (n >> 1) | (logical ? 0x00 : (n & 0x80));
                        expectedF = SZP35(expected) | (Bit(n, 0) ? flagC : 0);
                    }

                    ASSERT_EQ(_core.ReadRAM(hl), expected);
                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecks(ticks, 15, pc + 2, 0x02);
                }
            }
        }
    }

    void ShiftIndex(bool left, bool logical)
    {
        for (byte f : flagBytes)
        {
            for (word hl : testAddresses)
            {
                for (IndexRegInfo indexInfo : _idxRegs)
                {
                    for (offset o : testOffsets)
                    {
                        for (byte n : testBytes)
                        {
                            word pc = hl + 0x100;
                            _core.Init();
                            byte opcode = 0x26 | (logical ? 0x10 : 0x00) | (left ? 0x00 : 0x08);
                            SetMemory(pc, indexInfo._prefix, 0xCB, o, opcode);
                            SetMemory(hl + o, n);
                            _core.PC = pc;
                            *indexInfo._pReg = hl;
                            _core.F = f;

                            qword ticks = Run(1);

                            byte expected = 0;
                            byte expectedF = 0;
                            if (left)
                            {
                                expected = (n << 1) | (logical ? 0x01 : 0x00);
                                expectedF = SZP35(expected) | (Bit(n, 7) ? flagC : 0);
                            }
                            else
                            {
                                expected = (n >> 1) | (logical ? 0x00 : (n & 0x80));
                                expectedF = SZP35(expected) | (Bit(n, 0) ? flagC : 0);
                            }

                            ASSERT_EQ(_core.ReadRAM(hl + o), expected);
                            ASSERT_EQ(_core.F, expectedF);
                            CommonChecks(ticks, 27, pc + 4, 0x02);
                        }
                    }
                }
            }
        }
    }

    void Rotate(bool left, bool rotateWithCarry)
    {
        for (byte f : flagBytes)
        {
            for (RegInfo reg : _regs)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    byte opcode = (rotateWithCarry?0x10:0x00) | reg._code | (left?0x00:0x08);
                    SetMemory(0x0000, 0xCB, opcode);
                    *reg._pReg = n;
                    _core.F = f;

                    qword ticks = Run(1);

                    byte expected = 0;
                    byte expectedF = 0;
                    bool previousCarryFlag = ((f & flagC) != 0);
                    bool newCarryFlag = false;
                    if (left)
                    {
                        newCarryFlag = Bit(n, 7);
                        bool lowbit = rotateWithCarry ? previousCarryFlag : newCarryFlag;
                        expected = (n << 1) | (lowbit ? 0x01 : 0);
                    }
                    else
                    {
                        newCarryFlag = Bit(n, 0);
                        bool highbit = rotateWithCarry ? previousCarryFlag : newCarryFlag;
                        expected = (n >> 1) | (highbit ? 0x80 : 0);
                    }
                     
                    expectedF = SZP35(expected) | (newCarryFlag ? flagC : 0);

                    ASSERT_EQ(*reg._pReg, expected);
                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecks(ticks, 8, 0x0002, 0x02);
                }
            }
        }
    }

    void RotateA(bool left, bool rotateWithCarry)
    {
        for (byte f : flagBytes)
        {
            for (byte n : testBytes)
            {
                _core.Init();
                byte opcode = 0x07 | (rotateWithCarry ? 0x10 : 0x00) | (left ? 0x00 : 0x08);
                SetMemory(0x0000, opcode);
                _core.A = n;
                _core.F = f;

                qword ticks = Run(1);

                byte expected = 0;
                byte expectedF = 0;
                bool previousCarryFlag = ((f & flagC) != 0);
                bool newCarryFlag = false;
                if (left)
                {
                    newCarryFlag = Bit(n, 7);
                    bool newLowBit = rotateWithCarry ? previousCarryFlag : newCarryFlag;
                    expected = (n << 1) | (newLowBit ? 0x01 : 0x00);
                }
                else
                {
                    newCarryFlag = Bit(n, 0);
                    bool newHighBit = rotateWithCarry ? previousCarryFlag : newCarryFlag;
                    expected = (n >> 1) | (newHighBit ? 0x80 : 0x00);
                }

                expectedF = (expected & (flag3 | flag5)) | (newCarryFlag ? flagC : 0) | (f & (flagS | flagZ | flagPV));

                ASSERT_EQ(_core.A, expected);
                ASSERT_EQ(_core.F, expectedF);
                CommonChecks(ticks, 4, 0x0001, 0x01);
            }
        }
    }

    void RotateHLInd(bool left)
    {
        for (byte f : flagBytes)
        {
            for (word hl : testAddresses)
            {
                for (byte n : testBytes)
                {
                    _core.Init();
                    byte opcode = 0x16 | (left ? 0x00 : 0x08);
                    word pc = hl + 0x100;
                    SetMemory(pc, 0xCB, opcode);
                    SetMemory(hl, n);
                    _core.HL = hl;
                    _core.F = f;
                    _core.PC = pc;

                    qword ticks = Run(1);

                    byte expected = 0;
                    byte expectedF = 0;
                    if (left)
                    {
                        expected = (n << 1) | (((f & flagC) != 0) ? 0x01 : 0);
                        expectedF = SZP35(expected) | (Bit(n, 7) ? flagC : 0);
                    }
                    else
                    {
                        expected = (n >> 1) | (((f & flagC) != 0) ? 0x80 : 0);
                        expectedF = SZP35(expected) | (Bit(n, 0) ? flagC : 0);
                    }

                    ASSERT_EQ(_core.ReadRAM(hl), expected);
                    ASSERT_EQ(_core.F, expectedF);
                    CommonChecks(ticks, 15, pc + 2, 0x02);
                }
            }
        }
    }

    void RotateIndex(bool left, bool rotateWithCarry)
    {
        for (byte f : flagBytes)
        {
            for (IndexRegInfo indexInfo : _idxRegs)
            {
                for (word hl : testAddresses)
                {
                    for (offset o : testOffsets)
                    {
                        for (byte n : testBytes)
                        {
                            _core.Init();
                            byte opcode = 0x06 | (left ? 0x00 : 0x08) | (rotateWithCarry ? 0x00 : 0x10);
                            word pc = hl + 0x100;
                            SetMemory(pc, indexInfo._prefix, 0xCB, o, opcode);
                            SetMemory(hl + o, n);
                            *indexInfo._pReg = hl;
                            _core.F = f;
                            _core.PC = pc;

                            qword ticks = Run(1);

                            byte expected = 0;
                            byte expectedF = 0;
                            bool previousCarryFlag = ((f & flagC) != 0);
                            bool newCarryFlag = ((n & (left ? 0x80 : 0x01)) != 0);
                            bool wrappedBit = rotateWithCarry ? newCarryFlag : previousCarryFlag;
                            if (left)
                            {
                                expected = (n << 1) | (wrappedBit ? 0x01 : 0);
                            }
                            else
                            {
                                expected = (n >> 1) | (wrappedBit ? 0x80 : 0);
                            }
                            expectedF = SZP35(expected) | (newCarryFlag ? flagC : 0);

                            ASSERT_EQ(_core.ReadRAM(hl + o), expected);
                            ASSERT_EQ(_core.F, expectedF);
                            CommonChecks(ticks, 27, pc + 4, 0x02);
                        }
                    }
                }
            }
        }
    }

    void RotateBCD(bool left)
    {
        for (byte f : flagBytes)
        {
            for (word hl : testAddresses)
            {
                for (byte n : testBytes)
                {
                    for (byte a : testBytes)
                    {
                        _core.Init();
                        byte opcode = left ? 0x6F : 0x67;
                        word pc = hl + 0x100;
                        _core.PC = pc;
                        _core.A = a;
                        _core.F = f;
                        _core.HL = hl;
                        SetMemory(pc, 0xED, opcode);
                        SetMemory(hl, n);

                        qword ticks = Run(1);

                        byte expectedA = 0;
                        byte expectedHL = 0;
                        if (left)
                        {
                            expectedA = HighNibble(a) | (HighNibble(n) >> 4);
                            expectedHL = (LowNibble(n) << 4) | LowNibble(a);
                        }
                        else
                        {
                            expectedA = HighNibble(a) | LowNibble(n);
                            expectedHL = (LowNibble(a) << 4) | (HighNibble(n) >> 4);
                        }

                        byte expectedF = SZP35(expectedA) | (f & flagC);

                        ASSERT_EQ(_core.A, expectedA);
                        ASSERT_EQ(_core.F, expectedF);
                        ASSERT_EQ(expectedHL, _core.ReadRAM(hl));
                        CommonChecks(ticks, 19, pc + 2, 0x02);
                    }
                }
            }
        }
    }

    void JPrr(word& rr, byte opcode1)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                word pc = addr + 0x100;

                _core.Init();
                _core.PC = pc;
                SetInstruction(_core.PC, opcode1, 0xE9);
                _core.F = f;
                rr = addr;

                qword ticks = Run(1);

                ASSERT_EQ(addr, _core.PC);
                ASSERT_EQ(f, _core.F);
                ASSERT_EQ((opcode1 == 0) ? 4 : 8, ticks);
                ASSERT_EQ(_core.R, (opcode1 == 0x00)?0x01:0x02);
            }
        }
    }

    void LDrn()
    {
        for (RegInfo reg : _regsWithPrefixes)
        {
            for (byte f : flagBytes)
            {
                byte op = 0x06 | (reg._code << 3);
                for (byte n : testBytes)
                {
                    _core.Init();
                    SetInstruction(0x0000, reg._prefix, op, n);
                    _core.F = f;
                
                    qword ticks = Run(1);
                
                    ASSERT_EQ(n, *reg._pReg);
                    ASSERT_EQ(f, _core.F);
                    CommonChecksPrefix(reg._prefix, ticks, 7, 0x0002, 0x01);
                }
            }
        }
    }

    void LDrr()
    {
        for (byte f : flagBytes)
        {
            for (RegInfo srcReg : _regsWithPrefixes)
            {
                for (RegInfo destReg : _regsWithPrefixes)
                {
                    // Test is only valid for registers using the same prefix.
                    if (srcReg._prefix != destReg._prefix)
                    {
                        continue;
                    }

                    for (byte n : testBytes)
                    {
                        _core.Init();
                        *srcReg._pReg = n;
                        byte op = 0x40 | (destReg._code << 3) | srcReg._code;
                        SetInstruction(0x0000, srcReg._prefix, op);

                        _core.F = f;

                        qword ticks = Run(1);

                        ASSERT_EQ(n, *destReg._pReg);
                        ASSERT_EQ(f, _core.F);

                        CommonChecksPrefix(srcReg._prefix, ticks, 4, 0x0001, 0x01);
                    }
                }
            }
        }
    }
};

TEST_F(Z80Tests, LDrrnn)
{
    LDrrnn(0x01, _core.BC);
    LDrrnn(0x11, _core.DE);
    LDrrnn(0x21, _core.HL);
    LDrrnn(0x31, _core.SP);
}

TEST_F(Z80Tests, LDxynn)
{
    for (IndexRegInfo indexInfo : _idxRegs)
    {
        for (byte f : flagBytes)
        {
            for (word nn : testAddresses)
            {
                _core.Init();
                SetMemory(0x0000, indexInfo._prefix, 0x21, Low(nn), High(nn));
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(nn, *indexInfo._pReg);
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 15, 0x0004, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, LDrrnnInd)
{
    LDrrnnInd(0x4B, _core.BC);
    LDrrnnInd(0x5B, _core.DE);
    LDrrnnInd(0x6B, _core.HL);
    LDrrnnInd(0x7B, _core.SP);
}

TEST_F(Z80Tests, LDAnn)
{
    for (byte f : flagBytes)
    {
        for (word addr : testAddresses)
        {
            byte b = 0xFE;
            _core.Init();
            _core.PC = addr + 0x0010;
            SetMemory(_core.PC, 0x3A, Low(addr), High(addr));
            SetMemory(addr, b);
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(b, _core.A);
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 15, addr + 0x13, 0x01);
        }
    }
}

TEST_F(Z80Tests, LDnnA)
{
    for (byte f : flagBytes)
    {
        for (word addr : testAddresses)
        {
            byte b = 0xFE;
            _core.Init();
            _core.PC = addr + 0x0010;
            SetMemory(_core.PC, 0x32, Low(addr), High(addr));
            _core.A = b;
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(b, _core.ReadRAM(addr));
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 15, addr + 0x13, 0x01);
        }
    }
}

TEST_F(Z80Tests, LDnndd)
{
    LDnndd(0x43, _core.BC);
    LDnndd(0x53, _core.DE);
    LDnndd(0x63, _core.HL);
    LDnndd(0x73, _core.SP);
}

TEST_F(Z80Tests, LDnnHL)
{
    for (byte f : flagBytes)
    {
        for (word addr : testAddresses)
        {
            word w = 0xFEFF;
            _core.Init();
            _core.PC = addr + 0x0010;
            SetMemory(_core.PC, 0x22, Low(addr), High(addr));
            _core.HL = w;
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(Low(w), _core.ReadRAM(addr));
            ASSERT_EQ(High(w), _core.ReadRAM(addr + 1));
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 19, addr + 0x13, 0x01);
        }
    }
}

TEST_F(Z80Tests, LDnnIdx)
{
    for (IndexRegInfo indexInfo : _idxRegs)
    {
        for (byte f : flagBytes)
        {
            for (word addr : testAddresses)
            {
                word w = 0xFEFF;
                _core.Init();
                _core.PC = addr + 0x0010;
                SetMemory(_core.PC, indexInfo._prefix, 0x22, Low(addr), High(addr));
                *indexInfo._pReg = w;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(Low(w), _core.ReadRAM(addr));
                ASSERT_EQ(High(w), _core.ReadRAM(addr + 1));
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 23, addr + 0x14, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, LDrrA)
{
    LDrrA(0x02, _core.BC);
    LDrrA(0x12, _core.DE);
}

TEST_F(Z80Tests, LDArr)
{
    LDArr(0x0A, _core.BC);
    LDArr(0x1A, _core.DE);
}

TEST_F(Z80Tests, LDHLr)
{
    LDHLr();
}

TEST_F(Z80Tests, LDHLn)
{
    for (byte f : flagBytes)
    {
        for (word addr : testAddresses)
        {
            byte b = 0xFE;
            _core.Init();
            _core.PC = addr + 0x0010;
            SetMemory(_core.PC, 0x36, b);
            _core.HL = addr;
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(b, _core.ReadRAM(addr));
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 11, addr + 0x12, 0x01);
        }
    }
}

TEST_F(Z80Tests, LDrHL)
{
    LDrHL();
}

TEST_F(Z80Tests, LDrIdx)
{
    LDrIdx();
}

TEST_F(Z80Tests, LDIdxr)
{
    LDIdxr();
}

TEST_F(Z80Tests, LDIdxn)
{
    for (byte f : flagBytes)
    {
        for (IndexRegInfo indexInfo : _idxRegs)
        {
            for (word addr : testAddresses)
            {
                for (offset o : testOffsets)
                {
                    byte b = 0xFE;
                    _core.Init();
                    _core.PC = addr + 0x0100;
                    *indexInfo._pReg = addr;
                    SetMemory(_core.PC, indexInfo._prefix, 0x36, o, b);
                    _core.F = f;

                    qword ticks = Run(1);

                    ASSERT_EQ(b, _core.ReadRAM(addr + o));
                    ASSERT_EQ(f, _core.F);
                    CommonChecks(ticks, 23, addr + 0x0104, 0x02);
                }
            }
        }
    }
}

TEST_F(Z80Tests, LDrn)
{
    LDrn();
}

TEST_F(Z80Tests, LDrr)
{
    LDrr();
}

TEST_F(Z80Tests, LDAIR)
{
    LDAIR(0x57, _core.I);
    LDAIR(0x5F, _core.R);
}

TEST_F(Z80Tests, LDIRA)
{
    LDIRA(0x47, _core.I);
    LDIRA(0x4F, _core.R);
}

TEST_F(Z80Tests, LDHLnnInd)
{
    for (byte f : flagBytes)
    {
        for (word nn : testAddresses)
        {
            word w = 0xFEFF;
            _core.Init();
            _core.PC = nn + 0x0010;
            SetMemory(_core.PC, 0x2A, Low(nn), High(nn));
            SetMemory(nn, Low(w), High(w));
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(w, _core.HL);
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 19, nn + 0x13, 0x01);
        }
    }
}

TEST_F(Z80Tests, LDxynnInd)
{
    for (IndexRegInfo indexInfo : _idxRegs)
    {
        for (byte f : flagBytes)
        {
            for (word nn : testAddresses)
            {
                word w = 0xFEFF;
                _core.Init();
                _core.PC = nn + 0x0010;
                SetMemory(_core.PC, indexInfo._prefix, 0x2A, Low(nn), High(nn));
                SetMemory(nn, Low(w), High(w));
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(w, *indexInfo._pReg);
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 23, nn + 0x14, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, LDSPHL)
{
    for (byte f : flagBytes)
    {
        for (word nn : testAddresses)
        {
            _core.Init();
            _core.PC = 0x0000;
            SetMemory(_core.PC, 0xF9);
            _core.HL = nn;
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(nn, _core.SP);
            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 6, 0x0001, 0x01);
        }
    }
}

TEST_F(Z80Tests, LDSPxy)
{
    for (IndexRegInfo indexInfo : _idxRegs)
    {
        for (byte f : flagBytes)
        {
            for (word nn : testAddresses)
            {
                _core.Init();
                _core.PC = 0x0000;
                SetMemory(_core.PC, indexInfo._prefix, 0xF9);
                *indexInfo._pReg = nn;
                _core.F = f;

                qword ticks = Run(1);

                ASSERT_EQ(nn, _core.SP);
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 10, 0x0002, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, POPrr)
{
    POPrr(_core.BC, 0xC1, 0x00);
    POPrr(_core.DE, 0xD1, 0x00);
    POPrr(_core.HL, 0xE1, 0x00);
    POPrr(_core.AF, 0xF1, 0x00);
    POPrr(_core.IX, 0xDD, 0xE1);
    POPrr(_core.IY, 0xFD, 0xE1);
}

TEST_F(Z80Tests, RET)
{
    RET();
}

TEST_F(Z80Tests, RETI)
{
    RETIN(0x4D);
}

TEST_F(Z80Tests, RETN)
{
    RETIN(0x45);
    RETIN(0x55);
    RETIN(0x5D);
    RETIN(0x65);
    RETIN(0x6D);
    RETIN(0x75);
    RETIN(0x7D);
}

TEST_F(Z80Tests, RETcc)
{
    RETcc(0xC0, flagZ, false);
    RETcc(0xC8, flagZ, true);
    RETcc(0xD0, flagC, false);
    RETcc(0xD8, flagC, true);
    RETcc(0xE0, flagPV, false);
    RETcc(0xE8, flagPV, true);
    RETcc(0xF0, flagS, false);
    RETcc(0xF8, flagS, true);
}

TEST_F(Z80Tests, PUSHrr)
{
    PUSHrr(_core.BC, 0xC5, 0x00);
    PUSHrr(_core.DE, 0xD5, 0x00);
    PUSHrr(_core.HL, 0xE5, 0x00);
    PUSHrr(_core.AF, 0xF5, 0x00);
    PUSHrr(_core.IX, 0xDD, 0xE5);
    PUSHrr(_core.IY, 0xFD, 0xE5);
}

TEST_F(Z80Tests, CALLnn)
{
    for (byte f : flagBytes)
    {
        for (word nn : testAddresses)
        {
            word pc = 0x1234;

            _core.Init();
            _core.PC = pc;
            SetMemory(_core.PC, 0xCD, Low(nn), High(nn));
            _core.SP = _core.PC - 0x0010;
            _core.F = f;

            qword ticks = Run(1);
            pc += 3;

            ASSERT_EQ(Low(pc), _core.ReadRAM(_core.SP));
            ASSERT_EQ(High(pc), _core.ReadRAM(_core.SP + 1));
            ASSERT_EQ(_core.PC, nn);
            ASSERT_EQ(f, _core.F);
            ASSERT_EQ(19, ticks);
            ASSERT_EQ(_core.R, 0x01);
            CommonChecks(ticks, 19, nn, 0x01);
        }
    }
}

TEST_F(Z80Tests, CALLccnn)
{
    CALLccnn(0xC4, flagZ, false);
    CALLccnn(0xCC, flagZ, true);
    CALLccnn(0xD4, flagC, false);
    CALLccnn(0xDC, flagC, true);
    CALLccnn(0xE4, flagPV, false);
    CALLccnn(0xEC, flagPV, true);
    CALLccnn(0xF4, flagS, false);
    CALLccnn(0xFC, flagS, true);
}

TEST_F(Z80Tests, OTIDR)
{
    for (byte f : flagBytes)
    {
        for (bool inFlag : { false, true })
        {
            for (bool repeatFlag : { false, true })
            {
                for (bool incFlag : { false, true })
                {
                    byte opcode = 0xA2 | (incFlag ? 0x00 : 0x08) | (repeatFlag ? 0x10 : 0x00) | (inFlag?0x00:0x01);

                    for (word hl : testAddresses)
                    {
                        for (byte n : testBytes)
                        {
                            // Setup
                            byte port = 0xf4;
                            byte b = port + (inFlag ? 0 : 1);
                            word pc = hl + 0x100;

                            _bus.Reset();
                            _core.Init();
                            _core.PC = pc;
                            _core.F = f;
                            SetMemory(pc, 0xED, opcode);
                            SetMemory(hl, inFlag ? ~n : n);
                            _core.HL = hl;
                            _core.B = b;
                            _core.C = n;
                            _bus._readByte = inFlag ? n : ~n;

                            // Act
                            qword ticks = Run(1);

                            // Verify
                            byte expectedB = b - 1;
                            word expectedPC = pc + 0x0002;
                            if (repeatFlag && b != 0)
                            {
                                expectedPC = pc;
                            }

                            byte l = Low(hl) + (incFlag ? 1 : -1);
                            if (inFlag)
                            {
                                l = n + (incFlag ? 1 : -1);
                            }

                            signed short k = n + l;
                            byte p = (k & 0x07) ^ expectedB;
                            byte expectedF = SZ35(expectedB) |
                                (Bit(n, 7) ? flagN : 0) |
                                ((k > 0xff) ? (flagH | flagC) : 0) |
                                (Parity(p) ? flagPV : 0);

                            ASSERT_EQ(ticks, (repeatFlag ? 25 : 20) - (inFlag ? 1 : 0));
                            ASSERT_EQ(_core.F, expectedF);
                            ASSERT_EQ(_core.B, expectedB);
                            ASSERT_EQ(_core.HL, (word)(hl + (incFlag ? 1 : -1)));

                            if (inFlag)
                            {
                                ASSERT_EQ(_bus._writeCalled, false);
                                ASSERT_EQ(_bus._readCalled, true);
                                ASSERT_EQ(_bus._readAddress, MakeWord(0xf4, n));
                                ASSERT_EQ(n, _core.ReadRAM(hl));
                            }
                            else
                            {
                                ASSERT_EQ(_bus._readCalled, false);
                                ASSERT_EQ(_bus._writeCalled, true);
                                ASSERT_EQ(_bus._writeAddress, MakeWord(0xf4, n));
                                ASSERT_EQ(_bus._writeByte, n);
                            }

                            ASSERT_EQ(_core.PC, expectedPC);
                        }
                    }
                }
            }
        }
    }
}

TEST_F(Z80Tests, INAn)
{
    for (byte f : flagBytes)
    {
        for (byte n : allBytes)
        {
            byte a = 0xf4;
            _core.Init();
            _bus.Reset();
            _core.F = f;
            SetMemory(_core.PC, 0xDB, n);
            _core.A = a;
            _bus._readByte = n;

            qword ticks = Run(1);

            ASSERT_EQ(_bus._writeCalled, false);
            ASSERT_EQ(_bus._readCalled, true);
            ASSERT_EQ(_bus._readAddress, MakeWord(a, n));
            ASSERT_EQ(_bus._readByte, n);
            ASSERT_EQ(_core.F, f);
            CommonChecks(ticks, 12, 0x0002, 0x01);
        }
    }
}

TEST_F(Z80Tests, OUTnA)
{
    for (byte f : flagBytes)
    {
        for (byte n : allBytes)
        {
            byte a = 0xf4;
            _core.Init();
            _core.F = f;
            SetMemory(_core.PC, 0xD3, n);
            _core.A = a;
            _bus._writeByte = ~a;

            qword ticks = Run(1);

            ASSERT_EQ(_bus._readCalled, false);
            ASSERT_EQ(_bus._writeCalled, true);
            ASSERT_EQ(_bus._writeAddress, MakeWord(a, n));
            ASSERT_EQ(_bus._writeByte, a);
            ASSERT_EQ(_core.F, f);
            CommonChecks(ticks, 12, 0x0002, 0x01);
        }
    }
}

TEST_F(Z80Tests, INrC)
{
    for (byte f : flagBytes)
    {
        for (RegInfo reg : _regsWith0)
        {
            for (byte n : allBytes)
            {
                byte port = 0xf4;
                if (reg._pReg == nullptr && n != 0)
                {
                    continue;
                }

                if (reg._pReg != nullptr)
                {
                    *reg._pReg = 0;
                }

                _core.Init();
                _core.F = f;
                byte opcode = 0x40 | (reg._code << 3);
                SetMemory(_core.PC, 0xED, opcode);
                _core.B = port;
                _core.C = 0x00;
                _bus._readByte = n;

                qword ticks = Run(1);

                byte expectedF = SZP35(n) | (f & flagC);

                if (reg._pReg != nullptr)
                {
                    ASSERT_EQ(*reg._pReg, n);
                }

                ASSERT_EQ(_bus._writeCalled, false);
                ASSERT_EQ(_bus._readCalled, true);
                ASSERT_EQ(_bus._readAddress, MakeWord(port, 0x00));
                ASSERT_EQ(_core.F, expectedF);
                CommonChecks(ticks, 16, 0x0002, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, OUTCr)
{
    for (byte f : flagBytes)
    {
        for (RegInfo reg : _regsWith0)
        {
            for (byte n : allBytes)
            {
                byte port = 0xf4;
                if (reg._pReg == &_core.B && n != port)
                {
                    continue;
                }

                if (reg._pReg == nullptr && n != 0)
                {
                    continue;
                }

                _core.Init();
                _core.F = f;
                byte opcode = 0x41 | (reg._code << 3);
                SetMemory(_core.PC, 0xED, opcode);
                _core.B = port;
                _core.C = 0x00;
                _bus._writeByte = ~n;

                if (reg._pReg != nullptr)
                {
                    *reg._pReg = n;
                }

                qword ticks = Run(1);

                ASSERT_EQ(_bus._readCalled, false);
                ASSERT_EQ(_bus._writeCalled, true);
                ASSERT_EQ(_bus._writeAddress, MakeWord(port, _core.C));
                ASSERT_EQ(_bus._writeByte, n);
                ASSERT_EQ(_core.F, f);
                CommonChecks(ticks, 16, 0x0002, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, RSTp)
{
    for (byte f : flagBytes)
    {
        for (byte r : Range<byte>(0x00, 0x07))
        {
            for (word pc : testAddresses)
            {
                _core.Init();
                _core.PC = pc;
                byte opcode = 0xC7 | (r << 3);
                SetMemory(_core.PC, opcode);
                _core.SP = _core.PC - 0x0010;
                _core.F = f;

                qword ticks = Run(1);
                word expectedPC = r * 8;
                pc++;

                ASSERT_EQ(Low(pc), _core.ReadRAM(_core.SP));
                ASSERT_EQ(High(pc), _core.ReadRAM(_core.SP + 1));
                ASSERT_EQ(f, _core.F);
                CommonChecks(ticks, 15, expectedPC, 0x01);
            }
        }
    }
}

TEST_F(Z80Tests, CCF)
{
    for (byte f : testBytes)
    {
        for (byte a : testBytes)
        {
            _core.Init();
            SetMemory(_core.PC, 0x3F);
            _core.F = f;
            _core.A = a;

            qword ticks = Run(1);

            byte expectedF =
                (f & (flagS | flagZ | flagPV)) |
                (a & (flag3 | flag5)) |
                (((f & flagC) == 0) ? flagC : flagH);
            ASSERT_EQ(expectedF, _core.F);
            CommonChecks(ticks, 4, 0x0001, 0x01);
        }
    }
}

TEST_F(Z80Tests, SCF)
{
    for (byte f : allBytes)
    {
        for (byte a : testBytes)
        {
            _core.Init();
            SetMemory(_core.PC, 0x37);
            _core.F = f;
            _core.A = a;

            qword ticks = Run(1);

            byte expectedF = flagC;
            expectedF |= a & (flag3 | flag5);
            expectedF |= f & (flagS | flagZ | flagPV);
            ASSERT_EQ(expectedF, _core.F);
            CommonChecks(ticks, 4, 0x0001, 0x01);
        }
    }
}

TEST_F(Z80Tests, DECrr)
{
    INCDECrr(_core.BC, -1, 0x0B);
    INCDECrr(_core.DE, -1, 0x1B);
    INCDECrr(_core.HL, -1, 0x2B);
    INCDECrr(_core.SP, -1, 0x3B);
    INCDECrr(_core.IX, -1, 0xDD, 0x2B);
    INCDECrr(_core.IY, -1, 0xFD, 0x2B);
}

TEST_F(Z80Tests, INCrr)
{
    INCDECrr(_core.BC, 1, 0x03);
    INCDECrr(_core.DE, 1, 0x13);
    INCDECrr(_core.HL, 1, 0x23);
    INCDECrr(_core.SP, 1, 0x33);
    INCDECrr(_core.IX, 1, 0xDD, 0x23);
    INCDECrr(_core.IY, 1, 0xFD, 0x23);
}

TEST_F(Z80Tests, INCHLInd)
{
    for (byte f : flagBytes)
    {
        for (word hl : testAddresses)
        {
            for (byte n : testBytes)
            {
                word pc = hl + 0x100;
                _core.Init();
                _core.PC = pc;
                _core.HL = hl;
                _core.F = f;
                SetMemory(pc, 0x34);
                SetMemory(hl, n);

                qword ticks = Run(1);

                byte expected = n + 1;
                byte expectedF = HalfCarryParityInc(expected, f);

                ASSERT_EQ(expected, _core.ReadRAM(hl));
                ASSERT_EQ(_core.F, expectedF);
                CommonChecks(ticks, 11, pc + 1, 0x01);
            }
        }
    }
}

TEST_F(Z80Tests, DECHLInd)
{
    for (byte f : flagBytes)
    {
        for (word hl : testAddresses)
        {
            for (byte n : testBytes)
            {
                word pc = hl + 0x100;
                _core.Init();
                _core.PC = pc;
                _core.HL = hl;
                _core.F = f;
                SetMemory(pc, 0x35);
                SetMemory(hl, n);

                qword ticks = Run(1);

                byte expected = n - 1;
                byte expectedF = HalfCarryParityDec(expected, f);

                ASSERT_EQ(expected, _core.ReadRAM(hl));
                ASSERT_EQ(_core.F, expectedF);
                CommonChecks(ticks, 11, pc + 1, 0x01);
            }
        }
    }
}

TEST_F(Z80Tests, DECIndex)
{
    for (byte f : flagBytes)
    {
        for (IndexRegInfo indexInfo : _idxRegs)
        {
            for (word hl : testAddresses)
            {
                for (offset o : testOffsets)
                {
                    for (byte n : testBytes)
                    {
                        word pc = hl + 0x100;
                        _core.Init();
                        SetMemory(pc, indexInfo._prefix, 0x35, o);
                        _core.PC = pc;
                        *indexInfo._pReg = hl;
                        SetMemory(hl + o, n);
                        _core.F = f;

                        qword ticks = Run(1);

                        byte expected = n - 1;
                        byte expectedF = HalfCarryParityDec(expected, f);

                        ASSERT_EQ(expected, _core.ReadRAM(hl + o));
                        ASSERT_EQ(_core.F, expectedF);
                        CommonChecks(ticks, 23, pc + 3, 0x02);
                    }
                }
            }
        }
    }
}

TEST_F(Z80Tests, INCIndex)
{
    for (byte f : flagBytes)
    {
        for (IndexRegInfo indexInfo : _idxRegs)
        {
            for (word hl : testAddresses)
            {
                for (offset o : testOffsets)
                {
                    for (byte n : testBytes)
                    {
                        word pc = hl + 0x100;
                        _core.Init();
                        SetMemory(pc, indexInfo._prefix, 0x34, o);
                        _core.PC = pc;
                        *indexInfo._pReg = hl;
                        SetMemory(hl + o, n);
                        _core.F = f;

                        qword ticks = Run(1);

                        byte expected = n + 1;
                        byte expectedF = HalfCarryParityInc(expected, f);

                        ASSERT_EQ(expected, _core.ReadRAM(hl + o));
                        ASSERT_EQ(_core.F, expectedF);
                        CommonChecks(ticks, 23, pc + 3, 0x02);
                    }
                }
            }
        }
    }
}

TEST_F(Z80Tests, DI)
{
    _core.Init();
    _core._iff1 = true;
    _core._interruptRequested = true;
    _core._interruptMode = 1;

    // Ensure interrupts actually work first
    qword ticks = Run(1);

    CommonChecks(ticks, 15, 0x0038, 0x01);

    _core.Init();
    _core._iff1 = true;
    SetMemory(0x0000, 0xF3);

    ticks = Run(1);

    CommonChecks(ticks, 4, 0x0001, 0x01);

    _core._interruptRequested = true;

    ticks = Run(1);

    CommonChecks(ticks, 4, 0x0002, 0x02);
    ASSERT_EQ(_core._interruptRequested, true);
}

TEST_F(Z80Tests, EI)
{
    _core.Init();
    _core._iff1 = false;
    _core._interruptRequested = true;
    _core._interruptMode = 1;

    // Ensure interrupts are disabled first...
    qword ticks = Run(1);

    CommonChecks(ticks, 4, 0x0001, 0x01);

    _core.Init();
    _core._iff1 = false;
    _core._interruptMode = 1;
    SetMemory(0x0000, 0xFB);

    ticks = Run(1);

    CommonChecks(ticks, 4, 0x0001, 0x01);

    _core._interruptRequested = true;

    ticks = Run(1);

    CommonChecks(ticks, 4, 0x0002, 0x02);
    ASSERT_EQ(_core._interruptRequested, true);

    ticks = Run(1);

    CommonChecks(ticks, 15, 0x0038, 0x03);
    ASSERT_EQ(_core._interruptRequested, false);
}

TEST_F(Z80Tests, IM0)
{
    for (byte opcode : { 0x46, 0x4E })
    {
        _core.Init();
        _core._interruptMode = 3;

        SetMemory(0x0000, 0xED, opcode);

        qword ticks = Run(1);

        ASSERT_EQ(_core._interruptMode, 0);
        CommonChecks(ticks, 8, 0x0002, 0x02);
    }
}

TEST_F(Z80Tests, IM1)
{
    _core.Init();
    _core._interruptMode = 3;

    SetMemory(0x0000, 0xED, 0x56);

    qword ticks = Run(1);

    ASSERT_EQ(_core._interruptMode, 1);
    CommonChecks(ticks, 8, 0x0002, 0x02);
}

TEST_F(Z80Tests, IM2)
{
    _core.Init();
    _core._interruptMode = 3;

    SetMemory(0x0000, 0xED, 0x5E);

    qword ticks = Run(1);

    ASSERT_EQ(_core._interruptMode, 2);
    CommonChecks(ticks, 8, 0x0002, 0x02);
}

TEST_F(Z80Tests, JR)
{
    for (byte f : flagBytes)
    {
        for (offset o : testOffsets)
        {
            word pc = 0x1234;

            _core.Init();
            _core.PC = pc;
            SetMemory(_core.PC, 0x18, o);
            _core.F = f;

            qword ticks = Run(1);

            word expectedPC = (pc + 2 + o);

            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 12, expectedPC, 0x01);
        }
    }
}

TEST_F(Z80Tests, JP)
{
    for (byte f : flagBytes)
    {
        for (word addr : testAddresses)
        {
            word pc = 0x1234;

            _core.Init();
            _core.PC = pc;
            SetMemory(_core.PC, 0xC3, Low(addr), High(addr));
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(f, _core.F);
            CommonChecks(ticks, 11, addr, 0x01);
        }
    }
}

TEST_F(Z80Tests, JPHL)
{
    JPrr(_core.HL, 0x00);
    JPrr(_core.IX, 0xDD);
    JPrr(_core.IY, 0xFD);
}

TEST_F(Z80Tests, JPcc)
{
    JPcc(0xC2, flagZ, false);
    JPcc(0xCA, flagZ, true);
    JPcc(0xD2, flagC, false);
    JPcc(0xDA, flagC, true);
    JPcc(0xE2, flagPV, false);
    JPcc(0xEA, flagPV, true);
    JPcc(0xF2, flagS, false);
    JPcc(0xFA, flagS, true);
}

TEST_F(Z80Tests, JRcc)
{
    JRcc(0x20, flagZ, false);
    JRcc(0x28, flagZ, true);
    JRcc(0x30, flagC, false);
    JRcc(0x38, flagC, true);
}

TEST_F(Z80Tests, LDI)
{
    LDBlock(false, false);
}

TEST_F(Z80Tests, LDIR)
{
    LDBlock(true, false);
}

TEST_F(Z80Tests, LDD)
{
    LDBlock(false, true);
}

TEST_F(Z80Tests, LDDR)
{
    LDBlock(true, true);
}

TEST_F(Z80Tests, NOP)
{
    for (byte f : flagBytes)
    {
        _core.Init();
        SetMemory(0x0000, 0x00);
        _core.F = f;

        qword ticks = Run(1);

        ASSERT_EQ(f, _core.F);
        CommonChecks(ticks, 4, 0x0001, 0x01);
    }
}

TEST_F(Z80Tests, DECr)
{
    DECr();
}

TEST_F(Z80Tests, INCr)
{
    INCr();
}

TEST_F(Z80Tests, ANDr)
{
    ANDORXORr(0);
}

TEST_F(Z80Tests, XORr)
{
    ANDORXORr(1);
}

TEST_F(Z80Tests, ORr)
{
    ANDORXORr(2);
}

TEST_F(Z80Tests, ANDn)
{
    ANDORXORn(0);
}

TEST_F(Z80Tests, XORn)
{
    ANDORXORn(1);
}

TEST_F(Z80Tests, ORn)
{
    ANDORXORn(2);
}

TEST_F(Z80Tests, ANDHLInd)
{
    ANDORXORHLInd(0);
}

TEST_F(Z80Tests, XORHLInd)
{
    ANDORXORHLInd(1);
}

TEST_F(Z80Tests, ORHLInd)
{
    ANDORXORHLInd(2);
}

TEST_F(Z80Tests, ANDIndex)
{
    ANDORXORIndex(0);
}

TEST_F(Z80Tests, XORIndex)
{
    ANDORXORIndex(1);
}

TEST_F(Z80Tests, ORIndex)
{
    ANDORXORIndex(2);
}

TEST_F(Z80Tests, BITbr)
{
    BITbr();
}

TEST_F(Z80Tests, BITbHLInd)
{
    BITbHLInd();
}

TEST_F(Z80Tests, BITbIndex)
{
    BITbIndex();
}

TEST_F(Z80Tests, EXX)
{
    for (byte n : allBytes)
    {
        word _af = MakeWord(n, n + 1);
        word _bc = MakeWord(n + 2, n + 3);
        word _de = MakeWord(n + 4, n + 5);
        word _hl = MakeWord(n + 6, n + 7);

        word __af = MakeWord(n + 8, n + 9);
        word __bc = MakeWord(n + 10, n + 11);
        word __de = MakeWord(n + 12, n + 13);
        word __hl = MakeWord(n + 14, n + 15);

        _core.Init();
        SetMemory(0x0000, 0xD9);
        _core.AF = _af;
        _core.BC = _bc;
        _core.DE = _de;
        _core.HL = _hl;
        _core.AF_ = __af;
        _core.BC_ = __bc;
        _core.DE_ = __de;
        _core.HL_ = __hl;

        qword ticks = Run(1);

        ASSERT_EQ(__af, _core.AF_);
        ASSERT_EQ(__bc, _core.BC);
        ASSERT_EQ(__de, _core.DE);
        ASSERT_EQ(__hl, _core.HL);
        ASSERT_EQ(_af, _core.AF);
        ASSERT_EQ(_bc, _core.BC_);
        ASSERT_EQ(_de, _core.DE_);
        ASSERT_EQ(_hl, _core.HL_);
        CommonChecks(ticks, 4, 0x0001, 0x01);
    }
}

TEST_F(Z80Tests, EXAFAF)
{
    for (byte n : allBytes)
    {
        word _af = MakeWord(n, n + 1);
        word __af = MakeWord(n + 2, n + 3);

        _core.Init();
        SetMemory(0x0000, 0x08);
        _core.AF = _af;
        _core.AF_ = __af;

        qword ticks = Run(1);

        ASSERT_EQ(__af, _core.AF);
        ASSERT_EQ(_af, _core.AF_);
        CommonChecks(ticks, 4, 0x0001, 0x01);
    }
}

TEST_F(Z80Tests, EXDEHL)
{
    for (byte n : allBytes)
    {
        word _de = MakeWord(n, n + 1);
        word _hl = MakeWord(n + 2, n + 3);

        _core.Init();
        SetMemory(0x0000, 0xEB);
        _core.DE = _de;
        _core.HL = _hl;

        qword ticks = Run(1);

        ASSERT_EQ(_de, _core.HL);
        ASSERT_EQ(_hl, _core.DE);
        CommonChecks(ticks, 4, 0x0001, 0x01);
    }
}

TEST_F(Z80Tests, EXSPrr)
{
    EXSPrr(_core.HL, 0x00);
    EXSPrr(_core.IX, 0xDD);
    EXSPrr(_core.IY, 0xFD);
}

TEST_F(Z80Tests, DJNZ)
{
    for (byte b : allBytes)
    {
        for (offset o : testOffsets)
        {
            _core.Init();
            SetMemory(0x0000, 0x10, o);
            _core.B = b;

            qword ticks = Run(1);

            ASSERT_EQ(ticks, (b == 1) ? 11 : 16);
            ASSERT_EQ((byte) (b - 1), _core.B);
            ASSERT_EQ(_core.PC, (word) ((b == 1) ? 0x0002 : 0x0002 + o));
            ASSERT_EQ(_core.R, 0x01);
        }
    }
}

TEST_F(Z80Tests, CPL)
{
    for (byte f : flagBytes)
    {
        for (byte a : allBytes)
        {
            _core.Init();
            SetMemory(0x0000, 0x2F);
            _core.A = a;
            _core.F = f;

            qword ticks = Run(1);

            ASSERT_EQ(_core.A, a ^ 0xFF);
            ASSERT_EQ(_core.F, flagH | flagN | (f & (flagS | flagZ | flagPV | flagC)) | ((a ^ 0xFF) & (flag3 | flag5)));
            CommonChecks(ticks, 4, 0x0001, 0x01);
        }
    }
}

TEST_F(Z80Tests, RESbr)
{
    for (byte f : flagBytes)
    {
        for (RegInfo reg : _regs)
        {
            for (byte b : Range<byte>(0, 7))
            {
                byte n = 0xFF;
                byte opcode = 0x80 | reg._code | (b << 3);

                _core.Init();
                SetMemory(0x0000, 0xCB, opcode);
                _core.F = f;
                *reg._pReg = n;

                qword ticks = Run(1);

                ASSERT_EQ(f, _core.F);
                ASSERT_EQ(*reg._pReg, n & (~(1 << b)));
                CommonChecks(ticks, 8, 0x0002, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, RESbHLInd)
{
    for (byte f : flagBytes)
    {
        for (word hl : testAddresses)
        {
            for (byte b : Range<byte>(0, 7))
            {
                byte n = 0xFF;
                byte opcode = 0x86 | (b << 3);

                word pc = hl + 0x100;
                _core.Init();
                SetMemory(pc, 0xCB, opcode);
                _core.PC = pc;
                _core.F = f;
                _core.HL = hl;
                SetMemory(hl, n);

                qword ticks = Run(1);

                ASSERT_EQ(f, _core.F);
                ASSERT_EQ(_core.ReadRAM(hl), n & (~(1 << b)));
                CommonChecks(ticks, 15, pc + 2, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, RESbIndex)
{
    for (byte f : flagBytes)
    {
        for (IndexRegInfo indexInfo : _idxRegs)
        {
            for (RegInfo reg : _regsWith0)
            {
                for (word hl : testAddresses)
                {
                    for (offset o : testOffsets)
                    {
                        for (byte b : Range<byte>(0, 7))
                        {
                            byte n = 0xFF;
                            byte opcode = 0x80 | (b << 3) | reg._code;

                            word pc = hl + 0x100;
                            _core.Init();
                            SetMemory(pc, indexInfo._prefix, 0xCB, o, opcode);
                            _core.PC = pc;
                            _core.F = f;
                            *indexInfo._pReg = hl;
                            SetMemory(hl + o, n);
                            if (reg._pReg != nullptr)
                            {
                                *reg._pReg = 0;
                            }

                            qword ticks = Run(1);

                            byte expected = n & (~(1 << b));
                            ASSERT_EQ(f, _core.F);
                            ASSERT_EQ(_core.ReadRAM(hl + o), expected);
                            CommonChecks(ticks, 27, pc + 4, 0x02);

                            if (reg._pReg != nullptr)
                            {
                                ASSERT_EQ(*reg._pReg, expected);
                            }
                        }
                    }
                }
            }
        }
    }
}

TEST_F(Z80Tests, SETbr)
{
    for (byte f : flagBytes)
    {
        for (RegInfo reg : _regs)
        {
            for (byte b : Range<byte>(0, 7))
            {
                byte n = 0;
                byte opcode = 0xC0 | reg._code | (b << 3);

                _core.Init();
                SetMemory(0x0000, 0xCB, opcode);
                _core.F = f;
                *reg._pReg = n;

                qword ticks = Run(1);

                ASSERT_EQ(f, _core.F);
                ASSERT_EQ(*reg._pReg, n | (1 << b));

                CommonChecks(ticks, 8, 0x0002, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, SETbHLInd)
{
    for (byte f : flagBytes)
    {
        for (word hl : testAddresses)
        {
            for (byte b : Range<byte>(0, 7))
            {
                byte n = 0;
                byte opcode = 0xC6 | (b << 3);

                word pc = hl + 0x100;
                _core.Init();
                SetMemory(pc, 0xCB, opcode);
                _core.PC = pc;
                _core.F = f;
                _core.HL = hl;
                SetMemory(hl, n);

                qword ticks = Run(1);

                ASSERT_EQ(f, _core.F);
                ASSERT_EQ(_core.ReadRAM(hl), n | (1 << b));

                CommonChecks(ticks, 15, pc + 0x0002, 0x02);
            }
        }
    }
}

TEST_F(Z80Tests, SETbIndex)
{
    for (byte f : flagBytes)
    {
        for (IndexRegInfo indexInfo : _idxRegs)
        {
            for (word hl : testAddresses)
            {
                for (offset o : testOffsets)
                {
                    for (RegInfo reg : _regsWith0)
                    {
                        for (byte b : Range<byte>(0, 7))
                        {
                            byte n = 0;
                            byte opcode = 0xC0 | (b << 3) | reg._code;

                            word pc = hl + 0x100;
                            _core.Init();
                            SetMemory(pc, indexInfo._prefix, 0xCB, o, opcode);
                            _core.PC = pc;
                            _core.F = f;
                            *indexInfo._pReg = hl;
                            SetMemory(hl + o, n);
                            if (reg._pReg != nullptr)
                            {
                                reg._pReg = 0;
                            }

                            qword ticks = Run(1);

                            byte expected = n | (1 << b);
                            ASSERT_EQ(f, _core.F);
                            ASSERT_EQ(_core.ReadRAM(hl + o), expected);
                            if (reg._pReg != nullptr)
                            {
                                ASSERT_EQ(*reg._pReg, expected);
                            }

                            CommonChecks(ticks, 27, pc + 0x0004, 0x02);
                        }
                    }
                }
            }
        }
    }
}

TEST_F(Z80Tests, ADD)
{
    ADDADCSUBSBCr(false, false);
    ADDADCSUBSBCn(false, false);
    ADDADCSUBSBCHLInd(false, false);
    ADDADCSUBSBCIndex(false, false);
}

TEST_F(Z80Tests, ADC)
{
    ADDADCSUBSBCr(false, true);
    ADDADCSUBSBCn(false, true);
    ADDADCSUBSBCHLInd(false, true);
    ADDADCSUBSBCIndex(false, true);
}

TEST_F(Z80Tests, SUB)
{
    ADDADCSUBSBCr(true, false);
    ADDADCSUBSBCn(true, false);
    ADDADCSUBSBCHLInd(true, false);
    ADDADCSUBSBCIndex(true, false);
}

TEST_F(Z80Tests, SBC)
{
    ADDADCSUBSBCr(true, true);
    ADDADCSUBSBCn(true, true);
    ADDADCSUBSBCHLInd(true, true);
    ADDADCSUBSBCIndex(true, true);
}

TEST_F(Z80Tests, NEG)
{
    for (byte f : flagBytes)
    {
        for (byte a : allBytes)
        {
            _core.Init();
            _core.PC = 0x0000;
            SetMemory(_core.PC, 0xED, 0x44);
            _core.A = a;
            _core.F = f;

            qword ticks = Run(1);

            signed short res = 0 - ((signed short)a);
            bool halfCarry = (0 < (a & 0x0F));

            byte expectedA = 0 - a;
            byte expectedF = (Bit(expectedA, 7) ? flagS : 0) |
                ((expectedA == 0) ? flagZ : 0) |
                ((expectedA == 0x80) ? flagPV : 0) |
                flagN |
                ((expectedA != 0x00) ? flagC : 0) |
                (halfCarry ? flagH : 0) | 
                (expectedA & (flag3 | flag5));
            ASSERT_EQ(_core.F, expectedF);
            ASSERT_EQ(_core.A, expectedA);

            CommonChecks(ticks, 8, 0x0002, 2);
        }
    }
}

TEST_F(Z80Tests, CPr)
{
    CPr();
}

TEST_F(Z80Tests, CPn)
{
    CPn();
}

TEST_F(Z80Tests, CPHLInd)
{
    CPHLInd();
}

TEST_F(Z80Tests, CPIndex)
{
    CPIndex();
}

TEST_F(Z80Tests, CPD)
{
    CPBlock(false, true);
}

TEST_F(Z80Tests, CPDR)
{
    CPBlock(true, true);
}

TEST_F(Z80Tests, CPI)
{
    CPBlock(false, false);
}

TEST_F(Z80Tests, CPIR)
{
    CPBlock(true, false);
}

TEST_F(Z80Tests, ADDHLrr)
{
    ADCADDSBCSUBHLrr(0x09, 0x00, _core.BC, _core.HL, false, false, 11, 1);
    ADCADDSBCSUBHLrr(0x19, 0x00, _core.DE, _core.HL, false, false, 11, 1);
    ADCADDSBCSUBHLrr(0x29, 0x00, _core.HL, _core.HL, false, false, 11, 1);
    ADCADDSBCSUBHLrr(0x39, 0x00, _core.SP, _core.HL, false, false, 11, 1);
}

TEST_F(Z80Tests, ADDxyrr)
{
    ADCADDSBCSUBHLrr(0xDD, 0x09, _core.BC, _core.IX, false, false, 15, 2);
    ADCADDSBCSUBHLrr(0xDD, 0x19, _core.DE, _core.IX, false, false, 15, 2);
    ADCADDSBCSUBHLrr(0xDD, 0x29, _core.IX, _core.IX, false, false, 15, 2);
    ADCADDSBCSUBHLrr(0xDD, 0x39, _core.SP, _core.IX, false, false, 15, 2);

    ADCADDSBCSUBHLrr(0xFD, 0x09, _core.BC, _core.IY, false, false, 15, 2);
    ADCADDSBCSUBHLrr(0xFD, 0x19, _core.DE, _core.IY, false, false, 15, 2);
    ADCADDSBCSUBHLrr(0xFD, 0x29, _core.IY, _core.IY, false, false, 15, 2);
    ADCADDSBCSUBHLrr(0xFD, 0x39, _core.SP, _core.IY, false, false, 15, 2);
}

TEST_F(Z80Tests, ADCHLrr)
{
    ADCADDSBCSUBHLrr(0xED, 0x4A, _core.BC, _core.HL, true, false, 15, 2);
    ADCADDSBCSUBHLrr(0xED, 0x5A, _core.DE, _core.HL, true, false, 15, 2);
    ADCADDSBCSUBHLrr(0xED, 0x6A, _core.HL, _core.HL, true, false, 15, 2);
    ADCADDSBCSUBHLrr(0xED, 0x7A, _core.SP, _core.HL, true, false, 15, 2);
}

TEST_F(Z80Tests, SBCHLrr)
{
    ADCADDSBCSUBHLrr(0xED, 0x42, _core.BC, _core.HL, true, true, 15, 2);
    ADCADDSBCSUBHLrr(0xED, 0x52, _core.DE, _core.HL, true, true, 15, 2);
    ADCADDSBCSUBHLrr(0xED, 0x62, _core.HL, _core.HL, true, true, 15, 2);
    ADCADDSBCSUBHLrr(0xED, 0x72, _core.SP, _core.HL, true, true, 15, 2);
}

TEST_F(Z80Tests, SLLr)
{
    Shift(true, true);
}

TEST_F(Z80Tests, SLLHLInd)
{
    ShiftHLInd(true, true);
}

TEST_F(Z80Tests, SLLIndex)
{
    ShiftIndex(true, true);
}

TEST_F(Z80Tests, SRLr)
{
    Shift(false, true);
}

TEST_F(Z80Tests, SRLHLInd)
{
    ShiftHLInd(false, true);
}

TEST_F(Z80Tests, SRLIndex)
{
    ShiftIndex(false, true);
}

TEST_F(Z80Tests, SLAr)
{
    Shift(true, false);
}

TEST_F(Z80Tests, SLAHLInd)
{
    ShiftHLInd(true, false);
}

TEST_F(Z80Tests, SLAIndex)
{
    ShiftIndex(true, false);
}

TEST_F(Z80Tests, SRAr)
{
    Shift(false, false);
}

TEST_F(Z80Tests, SRAHLInd)
{
    ShiftHLInd(false, false);
}

TEST_F(Z80Tests, SRAIndex)
{
    ShiftIndex(false, false);
}

TEST_F(Z80Tests, RLr)
{
    Rotate(true, true);
}

TEST_F(Z80Tests, RRr)
{
    Rotate(false, true);
}

TEST_F(Z80Tests, RLCr)
{
    Rotate(true, false);
}

TEST_F(Z80Tests, RLCHLInd)
{
    RotateHLInd(true);
}

TEST_F(Z80Tests, RLCIndex)
{
    RotateIndex(true, true);
}

TEST_F(Z80Tests, RRCr)
{
    Rotate(false, false);
}

TEST_F(Z80Tests, RRCIndex)
{
    RotateIndex(false, true);
}

TEST_F(Z80Tests, RLA)
{
    RotateA(true, true);
}

TEST_F(Z80Tests, RRA)
{
    RotateA(false, true);
}

TEST_F(Z80Tests, RLCA)
{
    RotateA(true, false);
}

TEST_F(Z80Tests, RRCA)
{
    RotateA(false, false);
}

TEST_F(Z80Tests, RLHLInd)
{
    RotateHLInd(true);
}

TEST_F(Z80Tests, RRHLInd)
{
    RotateHLInd(false);
}

TEST_F(Z80Tests, RLIndex)
{
    RotateIndex(true, false);
}

TEST_F(Z80Tests, RRIndex)
{
    RotateIndex(false, false);
}

TEST_F(Z80Tests, DAA)
{
    for (byte f : testBytes)
    {
        for (byte a : testBytes)
        {
            _core.Init();
            SetMemory(0x0000, 0x27);
            _core.A = a;
            _core.F = f;

            qword ticks = Run(1);

            byte expectedA = 0;
            bool carryFlag = ((f & flagC) > 0);
            bool halfFlag = ((f & flagH) > 0);
            bool subtraction = ((f & flagN) > 0);
            bool carryFlagAfter = false;
            byte lowNibble = LowNibble(a);

            byte correction = 0x00;
            if (halfFlag || (lowNibble >= 0x0A))
            {
                correction |= 0x06;
            }

            if (carryFlag || (a >= 0x9A))
            {
                correction |= 0x60;
                carryFlagAfter = true;
            }

            if (subtraction)
            {
                expectedA = a - correction;
            }
            else
            {
                expectedA = a + correction;
            }

            byte expectedF = SZP35(expectedA);
            expectedF |= (carryFlagAfter ? flagC : 0);
            expectedF |= (f & flagN);

            if (subtraction)
            {
                expectedF |= ((((a & 0x0F) < (correction & 0x0F))) ? flagH : 0);
            }
            else
            {
                expectedF |= ((((a & 0x0F) + (correction & 0x0F)) > 0x0F) ? flagH : 0);
            }

            ASSERT_EQ(_core.A, expectedA);
            ASSERT_EQ(_core.F, expectedF);

            CommonChecks(ticks, 4, 0x0001, 0x01);
        }
    }
}

TEST_F(Z80Tests, RLD)
{
    RotateBCD(true);
}

TEST_F(Z80Tests, RRD)
{
    RotateBCD(false);
}
