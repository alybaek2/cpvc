#include "gtest/gtest.h"
#include "../cpvc-core/Memory.h"
#include "helpers.h"

TEST(MemoryTests, Configurations)
{
    Mem16k lowerROM;
    lowerROM.Fill(0x12);

    Mem16k upperROM;
    upperROM.Fill(0xFE);

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
                memory.EnableUpperROM(upperRomEnabled);
                memory.SetLowerROM(lowerROM);

                for (byte romIndex : allBytes)
                {
                    memory.SetUpperROM(romIndex, upperROM);
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
                                expected = upperROM[addr & 0x3FFF];
                            }
                            else if (lowerRomEnabled && addr < 0x4000)
                            {
                                expected = lowerROM[addr & 0x3FFF];
                            }

                            ASSERT_EQ(b, memory.VideoRead(addr));
                            ASSERT_EQ(expected, memory.Read(addr));
                        }
                    }

                    memory.RemoveUpperROM(romIndex);
                }
            }
        }
    }
}

TEST(MemoryTests, SelectROM)
{
    // Setup
    Mem16k basicROM;
    basicROM.Fill(0x12);

    Mem16k testROM;
    testROM.Fill(0xFE);

    for (byte selectedRom : testBytes)
    {
        for (byte rom : testBytes)
        {
            Memory memory;
            memory.Reset();
            memory.EnableUpperROM(true);
            memory.SetUpperROM(0, basicROM);
            if (rom != 0x00)
            {
                memory.SetUpperROM(rom, testROM);
            }

            Mem16k& expectedRom = (selectedRom != 0 && rom == selectedRom) ? testROM : basicROM;

            // Act
            memory.SelectROM(selectedRom);
            memory.SetRAMConfig(0);

            // Verify
            ASSERT_EQ(memory.Read(0xC000), expectedRom[0x0000]);
            ASSERT_EQ(memory.Read(0xFFFF), expectedRom[0x3FFF]);
        }
    }
}

// Tests that serializing/deserializing correctly restores the RAM configuration
// (i.e. how the 64k address space is mapped to the physical RAM on the board).
TEST(MemoryTests, SerializeRamConfig)
{
    // Setup
    Memory memory;
    memory.Reset();

    // Set the ram config so all 16k blocks differ from the default configuration.
    memory.EnableLowerROM(false);
    memory.EnableUpperROM(false);
    memory.SetRAMConfig(2);
    memory.Write(0x0000, 0x04);
    memory.Write(0x4000, 0x05);
    memory.Write(0x8000, 0x06);
    memory.Write(0xc000, 0x07);

    // Act
    StreamWriter writer;
    writer << memory;

    // Verify
    bytevector blob;
    blob.resize(writer.Size());
    writer.CopyTo(blob.data(), blob.size());
    StreamReader reader;
    for (byte b : blob)
    {
        reader.Push(b);
    }

    Memory memory2;
    reader >> memory2;

    // Verify that deserializing the Memory object has correctly configured the RAM
    ASSERT_EQ(memory2.Read(0x0000), 0x04);
    ASSERT_EQ(memory2.Read(0x4000), 0x05);
    ASSERT_EQ(memory2.Read(0x8000), 0x06);
    ASSERT_EQ(memory2.Read(0xc000), 0x07);
    memory2.SetRAMConfig(0);
    ASSERT_EQ(memory2.Read(0x0000), 0x00);
    ASSERT_EQ(memory2.Read(0x4000), 0x00);
    ASSERT_EQ(memory2.Read(0x8000), 0x00);
    ASSERT_EQ(memory2.Read(0xc000), 0x00);
}

// Tests that serializing/deserializing correctly restores the enabled state of the
// upper and lower ROMs.
TEST(MemoryTests, SerializeLowerUpperEnabed)
{
    // Setup
    Memory memory;
    memory.Reset();

    Mem16k lowerROM;
    lowerROM.Fill(0x01);
    Mem16k upperROM;
    upperROM.Fill(0x02);

    memory.EnableLowerROM(true);
    memory.EnableUpperROM(true);
    memory.SetLowerROM(lowerROM);
    memory.SetUpperROM(0, upperROM);

    // Act
    StreamWriter writer;
    writer << memory;

    // Verify
    bytevector blob;
    blob.resize(writer.Size());
    writer.CopyTo(blob.data(), blob.size());
    StreamReader reader;
    for (byte b : blob)
    {
        reader.Push(b);
    }

    Memory memory2;
    reader >> memory2;

    // Verify that deserializing the Memory object has correctly configured the
    // enabled state of the upper and lower ROMs.
    ASSERT_EQ(memory2.Read(0x0000), 0x01);
    ASSERT_EQ(memory2.Read(0xc000), 0x02);
    memory2.EnableLowerROM(false);
    memory2.EnableUpperROM(false);
    ASSERT_EQ(memory2.Read(0x0000), 0x00);
    ASSERT_EQ(memory2.Read(0xc000), 0x00);
}
