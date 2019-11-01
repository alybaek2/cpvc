#include "gtest/gtest.h"
#include "../cpvc-core/PPI.h"
#include "MockPSG.h"
#include "helpers.h"


TEST(PPITests, PortA) {
    for (word addr : Range<word>(0xF400, 0xF4FF))
    {
        for (byte writeControl : { 0x00, 0x10 }) 
        {
            for (byte readControl : { 0x00, 0x10 })
            {
                for (byte v : allBytes)
                {
                    bool vSync = false;
                    bool tapeMotor = false;
                    bool tapeLevel = false;
                    Keyboard keyboard;
                    MockPSG psg(keyboard);
                    PPI ppi(psg, keyboard, &vSync, &tapeMotor, &tapeLevel);
                    ppi._portA = ~v;
                    psg._readByte = ~(v + 1);

                    ppi._control = writeControl;
                    ppi.Write(addr, v);

                    ASSERT_EQ(ppi._portA, v);

                    if (writeControl == 0x00)
                    {
                        ASSERT_EQ(psg._writeCalled, true);
                        ASSERT_EQ(psg._writeByte, v);
                    }
                    else
                    {
                        ASSERT_EQ(psg._writeCalled, false);
                    }

                    ppi._control = readControl;
                    byte portA = ppi.Read(addr);

                    if (readControl == 0x00)
                    {
                        ASSERT_EQ(psg._readCalled, false);
                        ASSERT_EQ(portA, v);
                    }
                    else
                    {
                        ASSERT_EQ(psg._readCalled, true);
                        ASSERT_EQ(portA, psg._readByte);
                    }
                }
            }
        }
    }
}

TEST(PPITests, PortB) {
    for (word addr : Range<word>(0xF500, 0xF5FF))
    {
        for (byte writeControl : { 0x00, 0x02 })
        {
            for (byte readControl : { 0x00, 0x02 })
            {
                for (byte v : allBytes)
                {
                    bool vSync = false;
                    bool tapeMotor = false;
                    bool tapeLevel = false;
                    Keyboard keyboard;
                    MockPSG psg(keyboard);
                    PPI ppi(psg, keyboard, &vSync, &tapeMotor, &tapeLevel);

                    ppi.Reset();
                    tapeLevel = Bit(v, 7);
                    ppi._printerReady = Bit(v, 6);
                    ppi._exp = Bit(v, 5);
                    ppi._refreshRate = Bit(v, 4);
                    ppi._manufacturer = (v & 0x0E) >> 1;
                    vSync = Bit(v, 0);

                    ppi._portB = ~(v + 1);

                    ppi._control = writeControl;
                    ppi.Write(addr, ~v);

                    ppi._control = readControl;
                    byte portB = ppi.Read(addr);

                    if (readControl == 0x00)
                    {
                        ASSERT_EQ(portB, (byte)(~v));
                    }
                    else
                    {
                        ASSERT_EQ(portB, v);
                    }
                }
            }
        }
    }
}

TEST(PPITests, PortC) {
    for (word addr : Range<word>(0xF600, 0xF6FF))
    {
        for (byte writeControl : { 0x00, 0x01, 0x08, 0x09 })
        {
            for (byte readControl : { 0x00, 0x01, 0x08, 0x09 })
            {
                for (byte v : allBytes)
                {
                    bool vSync = false;
                    bool tapeMotor = false;
                    bool tapeLevel = false;
                    Keyboard keyboard;
                    MockPSG psg(keyboard);
                    PPI ppi(psg, keyboard, &vSync, &tapeMotor, &tapeLevel);

                    ppi.Reset();

                    ppi._portC = ~v;
                    psg._bdir = !Bit(v, 7);
                    psg._bc1 = !Bit(v, 6);
                    ppi._tapeWriteData = !Bit(v, 5);
                    ppi._tapeMotor = !Bit(v, 4);
                    keyboard.SelectLine((~v) & 0x0F);

                    ppi._control = writeControl;
                    ppi.Write(addr, v);

                    ppi._control = readControl;
                    byte portC = ppi.Read(addr);

                    ASSERT_EQ(portC & 0xF0, v & 0xF0);
                    if ((writeControl & 0x08) == 0x00)
                    {
                        ASSERT_EQ(Bit(v, 7), psg._bdir);
                        ASSERT_EQ(Bit(v, 6), psg._bc1);

                        ASSERT_EQ(Bit(v, 5), ppi._tapeWriteData);
                        ASSERT_EQ(Bit(v, 4), ppi._tapeMotor);
                    }
                    else
                    {
                        ASSERT_EQ(!Bit(v, 7), psg._bdir);
                        ASSERT_EQ(!Bit(v, 6), psg._bc1);

                        ASSERT_EQ(!Bit(v, 5), ppi._tapeWriteData);
                        ASSERT_EQ(!Bit(v, 4), ppi._tapeMotor);
                    }

                    ASSERT_EQ(portC & 0x0F, v & 0x0F);
                    if ((writeControl & 0x01) == 0x00)
                    {
                        ASSERT_EQ(v & 0x0F, keyboard.SelectedLine());
                    }
                    else
                    {
                        ASSERT_EQ((~v) & 0x0F, keyboard.SelectedLine());
                    }
                }
            }
        }
    }
}

TEST(PPITests, Control) {
    for (word addr : Range<word>(0xF700, 0xF7FF))
    {
        for (byte portA : { 0x00, 0xFF })
        {
            for (byte portC : { 0x00, 0xFF })
            {
                for (byte v : allBytes)
                {
                    bool vSync = false;
                    bool tapeMotor = false;
                    bool tapeLevel = false;
                    Keyboard keyboard;
                    MockPSG psg(keyboard);
                    PPI ppi(psg, keyboard, &vSync, &tapeMotor, &tapeLevel);

                    ppi.Reset();

                    ppi._portA = portA;
                    ppi._portC = portC;

                    psg._bdir = !Bit(portC, 7);
                    psg._bc1 = !Bit(portC, 6);
                    ppi._tapeWriteData = !Bit(portC, 5);
                    ppi._tapeMotor = !Bit(portC, 4);
                    keyboard.SelectLine((~v) & 0x0F);

                    ppi.Write(addr, v);

                    if ((v & 0x80) != 0x00)
                    {
                        ASSERT_EQ(ppi._control, v);
                        ASSERT_EQ(ppi._portA, 0x00);
                        ASSERT_EQ(ppi._portB, 0x00);
                        ASSERT_EQ(ppi._portC, 0x00);
                    }
                    else
                    {
                        byte bit = (v & 0x0E) >> 1;
                        byte expectedPortC = portC;
                        if (Bit(v, 0))
                        {
                            expectedPortC |= (0x01 << bit);
                        }
                        else
                        {
                            expectedPortC &= (~(0x01 << bit));
                        }

                        ASSERT_EQ(ppi._portC, expectedPortC);

                        ASSERT_EQ(Bit(expectedPortC, 7), psg._bdir);
                        ASSERT_EQ(Bit(expectedPortC, 6), psg._bc1);

                        ASSERT_EQ(psg._writeCalled, true);
                        ASSERT_EQ(psg._writeByte, portA);

                        ASSERT_EQ(Bit(expectedPortC, 5), ppi._tapeWriteData);
                        ASSERT_EQ(Bit(expectedPortC, 4), ppi._tapeMotor);
                    }
                }
            }
        }
    }
}
