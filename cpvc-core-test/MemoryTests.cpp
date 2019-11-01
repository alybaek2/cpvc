#include "gtest/gtest.h"
#include "../cpvc-core/Memory.h"
#include "helpers.h"

TEST(MemoryTests, Configurations)
{
    Mem16k lowerRom;
    lowerRom.Fill(0x12);

    Mem16k upperRom;
    upperRom.Fill(0xFE);

    byte configs[8][4] = {
        { 0, 1, 2, 3 },
        { 0, 1, 2, 7 },
        { 4, 5, 6, 7 },
        { 0, 3, 2, 7 },
        { 0, 4, 2, 3 },
        { 0, 5, 2, 3 },
        { 0, 6, 2, 3 },
        { 0, 7, 2, 3 }
    };

    for (byte config = 0; config < 8; config++)
    {
        Memory memory;
        memory.Reset();

        for (bool lowerRomEnabled : { false, true })
        {
            memory.EnableLowerROM(lowerRomEnabled);
            for (bool upperRomEnabled : { false, true })
            {
                memory.EnableUpperRom(upperRomEnabled);
                memory.SetLowerROM(lowerRom);

                for (byte romIndex : allBytes)
                {
                    memory.AddUpperRom(romIndex, upperRom);
                    memory.SelectROM(romIndex);
                    memory.SetRAMConfig(config);

                    for (word addr : testAddresses)
                    {
                        byte bank = configs[config][addr >> 14];
                        word bankAddr = addr & 0x3fff;

                        for (byte b : { 0xFF, 0x00 })
                        {
                            memory.Write(addr, b);

                            byte expected = b;
                            if (upperRomEnabled && addr >= 0xC000)
                            {
                                expected = upperRom[addr & 0x3FFF];
                            }
                            else if (lowerRomEnabled && addr < 0x4000)
                            {
                                expected = lowerRom[addr & 0x3FFF];
                            }

                            ASSERT_EQ(b, memory.VideoRead(addr));
                            ASSERT_EQ(expected, memory.Read(addr));
                        }
                    }

                    memory.RemoveUpperRom(romIndex);
                }
            }
        }
    }
}

TEST(MemoryTests, SelectROM)
{
    Mem16k basicRom;
    basicRom.Fill(0x12);

    Mem16k testRom;
    testRom.Fill(0xFE);

    for (byte selectedRom : allBytes)
    {
        for (byte rom : allBytes)
        {
            Memory memory;
            memory.Reset();
            memory.EnableUpperRom(true);
            memory.AddUpperRom(0, basicRom);
            if (rom != 0x00)
            {
                memory.AddUpperRom(rom, testRom);
            }

            Mem16k& expectedRom = (selectedRom != 0 && rom == selectedRom) ? testRom : basicRom;

            memory.SelectROM(selectedRom);
            memory.SetRAMConfig(0);

            ASSERT_EQ(memory.Read(0xC000), expectedRom[0x0000]);
            ASSERT_EQ(memory.Read(0xFFFF), expectedRom[0x3FFF]);
        }
    }
}
