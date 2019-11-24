#include "gtest/gtest.h"
#include "../cpvc-core/GateArray.h"
#include "helpers.h"

TEST(GateArrayTests, SelectModeAndLowerAndUpperRoms)
{
    byte ramByte1 = 0x00;
    byte ramByte2 = 0x08;

    for (byte p : Range<byte>(0x00, 0x3F))
    {
        bool interruptRequested;
        byte scanLineCount;

        Mem16k lowerRom;
        lowerRom.Fill(0x80);

        Memory memory;
        GateArray* pGateArray = new GateArray(memory, interruptRequested, scanLineCount);
        memory.Reset();
        memory.Write(0x0000, ramByte1);
        memory.Write(0xC000, ramByte2);
        Mem16k upperRom;
        upperRom.Fill(0xFF);
        memory.SetUpperROM(0x00, upperRom);
        memory.SelectROM(0x00);
        memory.SetLowerROM(lowerRom);

        pGateArray->Write(0x80 | p);

        bool expectedLowerRomEnabled = ((p & 0x04) == 0);
        bool expectedUpperRomEnabled = ((p & 0x08) == 0);

        ASSERT_EQ(p & 0x03, pGateArray->_mode);
        ASSERT_EQ(memory.Read(0x0000), (expectedLowerRomEnabled ? lowerRom[0] : ramByte1));
        ASSERT_EQ(memory.Read(0xC000), (expectedUpperRomEnabled ? upperRom[0] : ramByte2));

        delete pGateArray;
    }
}

TEST(GateArrayTests, SelectPenAndColour)
{
    bool interruptRequested;
    byte scanLineCount;
    Memory memory;
    GateArray* pGateArray = new GateArray(memory, interruptRequested, scanLineCount);
    memory.Reset();

    for (byte p : Range<byte>(0x00, 0x3F))
    {
        pGateArray->Write(0x00 | p);

        ASSERT_EQ(p & 0x1F, pGateArray->_selectedPen);

        for (byte c : Range<byte>(0x00, 0x3F))
        {
            pGateArray->Write(0x40 | c);

            ASSERT_EQ(c & 0x1F, Bit(p, 4) ? pGateArray->_border : pGateArray->_pen[p & 0x1F]);
        }
    }

    delete pGateArray;
}

TEST(GateArrayTests, Serialize)
{
    // Setup
    bool interruptRequested;
    byte scanLineCount;
    Memory memory;
    GateArray* pGateArray = new GateArray(memory, interruptRequested, scanLineCount);

    pGateArray->_selectedPen = 13;
    pGateArray->_mode = 3;
    pGateArray->_border = 4;

    for (byte i = 0; i < 16; i++)
    {
        pGateArray->_pen[i] = i;
    }

    pGateArray->RenderPens();

    // Act
    StreamWriter writer;
    writer << (*pGateArray);

    StreamReader reader(writer);
    GateArray* pGateArray2 = new GateArray(memory, interruptRequested, scanLineCount);
    reader >> (*pGateArray2);

    // Verify
    ASSERT_EQ(pGateArray->_selectedPen, pGateArray2->_selectedPen);
    ASSERT_EQ(pGateArray->_mode, pGateArray2->_mode);
    ASSERT_EQ(pGateArray->_border, pGateArray2->_border);
    ArraysEqual(pGateArray->_pen, pGateArray2->_pen);
    ArraysEqual(pGateArray->_renderedPenBytes, pGateArray2->_renderedPenBytes);

    delete pGateArray;
    delete pGateArray2;
}

