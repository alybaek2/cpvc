#include "gtest/gtest.h"
#include "../cpvc-core/Core.h"
#include "helpers.h"

TEST(CoreTests, SetLowerRom) {
    // Setup
    Mem16k lowerRom;
    lowerRom.Fill(0xff);

    Core* pCore = new Core();
    pCore->SetLowerRom(lowerRom);

    // Act
    pCore->EnableLowerRom(true);
    byte enabledByte = pCore->ReadRAM(0x0000);

    pCore->EnableLowerRom(false);
    byte disabledByte = pCore->ReadRAM(0x0000);

    delete pCore;

    // Verify
    ASSERT_EQ(enabledByte, 0xff);
    ASSERT_EQ(disabledByte, 0x00);
};

TEST(CoreTests, SetUpperRom) {
    // Setup
    Mem16k upperRom;
    upperRom.Fill(0xff);

    Core* pCore = new Core();
    pCore->SetUpperRom(0, upperRom); // Default selected upper rom slot is 0.

    // Act
    pCore->EnableUpperRom(true);
    byte enabledByte = pCore->ReadRAM(0xc000);

    pCore->EnableUpperRom(false);
    byte disabledByte = pCore->ReadRAM(0xc000);

    delete pCore;

    // Verify
    ASSERT_EQ(enabledByte, 0xff);
    ASSERT_EQ(disabledByte, 0x00);
};
