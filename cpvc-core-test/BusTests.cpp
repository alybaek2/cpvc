#include "gtest/gtest.h"
#include "../cpvc-core/Bus.h"
#include "MockDevice.h"
#include "helpers.h"

std::vector<word> GetTestAddresses()
{
    // Attempt every combination of 0, 1, and 2 devices.
    std::vector<word> addrs = { 0xffff };
    for (byte bit0 : Range<byte>(0, 15))
    {
        for (byte bit1 : Range<byte>(bit0, 15))
        {
            word addr = 0x0000;
            addr |= (1 << bit0);
            addr |= (1 << bit1);
            addr = ~addr;

            addrs.push_back(addr);
        }
    }

    return addrs;
}

std::vector<word> testBusAddresses = GetTestAddresses();

TEST(BusTests, Read)
{
    byte fdcReadByte = 0x01;
    byte ppiReadByte = 0x02;

    for (word addr : testBusAddresses)
    {
        // Setup
        Memory memory;
        memory.Reset();

        MockDevice fdc;
        fdc._readByte = fdcReadByte;
        MockDevice ppi;
        ppi._readByte = ppiReadByte;
        MockDevice crtc;
        MockDevice gateArray;
        Bus bus(memory, gateArray, ppi, crtc, fdc);

        // Act
        byte b = bus.Read(addr);

        // Verify
        bool ppiShouldBeCalled = false;
        bool fdcShouldBeCalled = false;

        if ((addr & 0x0800) == 0x0000)
        {
            ppiShouldBeCalled = true;
        }

        if ((addr & 0x0400) == 0x0000)
        {
            if ((addr & 0x0080) == 0x0000)
            {
                if (!ppiShouldBeCalled)
                {
                    fdcShouldBeCalled = true;
                }
            }
        }

        if (ppiShouldBeCalled)
        {
            ASSERT_EQ(ppi._readCalled, true);
            ASSERT_EQ(b, ppiReadByte);
        }
        else
        {
            ASSERT_EQ(ppi._readCalled, false);
        }

        if (fdcShouldBeCalled)
        {
            ASSERT_EQ(fdc._readCalled, true);
            ASSERT_EQ(b, fdcReadByte);
        }
        else
        {
            ASSERT_EQ(fdc._readCalled, false);
        }

        ASSERT_EQ(crtc._readCalled, false);
    }
}

TEST(BusTests, Write)
{
    Mem16k testRom;
    testRom.Fill(0x80);

    Mem16k originalRom;
    originalRom.Fill(0xCD);

    for (word addr : testBusAddresses)
    {
        for (byte b : testBytes)
        {
            // Setup
            Memory memory;
            memory.Reset();
            byte originalSelectedRom = ~b;
            memory.SetUpperROM(originalSelectedRom, originalRom);
            memory.SelectROM(originalSelectedRom);
            memory.SetUpperROM(b, testRom);
            memory.EnableUpperROM(true);

            MockDevice fdc;
            MockDevice ppi;
            MockDevice crtc;
            MockDevice gateArray;
            Bus bus(memory, gateArray, ppi, crtc, fdc);

            // Act
            bus.Write(addr, b);

            // Verify
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

            if ((addr & 0x0480) == 0x0000)
            {
                ASSERT_EQ(true, fdc._writeCalled);
                ASSERT_EQ(b, fdc._writeByte);
            }
            else
            {
                ASSERT_EQ(false, fdc._writeCalled);
            }
        }
    }
}