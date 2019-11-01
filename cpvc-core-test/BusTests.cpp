#include "gtest/gtest.h"
#include "../cpvc-core/Bus.h"
#include "MockDevice.h"
#include "helpers.h"

TEST(BusTests, Write)
{
    Mem16k testRom;
    testRom.Fill(0x80);

    Mem16k originalRom;
    originalRom.Fill(0xCD);

    for (byte a : allBytes)
    {
        word addr = MakeWord(a, 0);

        for (byte b : testBytes)
        {
            Memory memory;
            memory.Reset();
            byte originalSelectedRom = ~b;
            memory.AddUpperRom(originalSelectedRom, originalRom);
            memory.SelectROM(originalSelectedRom);
            memory.AddUpperRom(b, testRom);
            memory.EnableUpperRom(true);
            
            FDC fdc;
            MockDevice ppi;
            MockDevice crtc;
            MockDeviceNoAddress gateArray;
            Bus bus(memory, gateArray, ppi, crtc, fdc);

            bus.Write(addr, b);

            if ((addr & 0x0800) == 0x0000)
            {
                ASSERT_EQ(true, ppi._writeCalled);
                ASSERT_EQ(addr, ppi._writeAddress);
                ASSERT_EQ(b, ppi._writeByte);
            }
            else
            {
                ASSERT_EQ(false, ppi._writeCalled);
            }
            
            if ((addr & 0x4000) == 0x0000)
            {
                ASSERT_EQ(true, crtc._writeCalled);
                ASSERT_EQ(addr, crtc._writeAddress);
                ASSERT_EQ(b, crtc._writeByte);
            }
            else
            {
                ASSERT_EQ(false, crtc._writeCalled);
            }
            
            if ((addr & 0xC000) == 0x4000)
            {
                ASSERT_EQ(true, gateArray._writeCalled);
                ASSERT_EQ(b, gateArray._writeByte);
            }
            else
            {
                ASSERT_EQ(false, gateArray._writeCalled);
            }

            if ((addr & 0x2000) == 0x0000)
            {
                ASSERT_EQ(testRom[0], memory.Read(0xC000));
            }
            else
            {
                ASSERT_EQ(originalRom[0], memory.Read(0xC000));
            }
        }
    }
}