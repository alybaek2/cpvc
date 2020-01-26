#include "gtest/gtest.h"
#include "../cpvc-core/Core.h"
#include "helpers.h"

TEST(CoreTests, SetLowerRom)
{
    // Setup
    Mem16k lowerRom;
    lowerRom.Fill(0xff);

    Core* pCore = new Core();
    pCore->SetLowerRom(lowerRom);

    // Act
    pCore->EnableLowerROM(true);
    byte enabledByte = pCore->ReadRAM(0x0000);

    pCore->EnableLowerROM(false);
    byte disabledByte = pCore->ReadRAM(0x0000);

    delete pCore;

    // Verify
    ASSERT_EQ(enabledByte, 0xff);
    ASSERT_EQ(disabledByte, 0x00);
};

TEST(CoreTests, SetUpperRom)
{
    // Setup
    Mem16k upperRom;
    upperRom.Fill(0xff);

    Core* pCore = new Core();
    pCore->SetUpperRom(0, upperRom); // Default selected upper rom slot is 0.

    // Act
    pCore->EnableUpperROM(true);
    byte enabledByte = pCore->ReadRAM(0xc000);

    pCore->EnableUpperROM(false);
    byte disabledByte = pCore->ReadRAM(0xc000);

    delete pCore;

    // Verify
    ASSERT_EQ(enabledByte, 0xff);
    ASSERT_EQ(disabledByte, 0x00);
};

// Ensures that when writing to the screen buffer, the core stops once it hits the
// right-hand edge of the screen.
TEST(CoreTests, SetSmallWidthScreen)
{
    // Setup
    Core* pCore = new Core();

    // Create a screen buffer with a normal height, but small width.
    constexpr word widthChars = 10;
    constexpr word widthPixels = widthChars * 16;  // 16 pixels per CRTC char.
    constexpr word height = 300;
    constexpr int bufsize = widthPixels * height;

    byte* pScreen = new byte[bufsize];
    memset(pScreen, 1, bufsize);
    pCore->SetScreen(pScreen, widthPixels, height, widthPixels);

    // Act - since one CRTC "char" is written every 4 ticks, run the core for the full width
    //       of the screen plus one, and ensure this one extra char does not get written to
    //       the screen buffer. Note that we need to first run the core for 16 scanlines to
    //       compensate for overscan, hence the 16 * 0x40 below (0x40 chars is the default
    //       "Horizontal Total" for the CPC).
    pCore->RunUntil(((16 * 0x40) + widthChars + 1) * 4, 0);

    // Verify
    for (word i = 0; i < bufsize; i++)
    {
        // The core should have written a single line of zero pixels in the screen buffer, and
        // the original ones in the second line should remain.
        ASSERT_EQ(pScreen[i], (i < widthPixels) ? 0 : 1);
    }

    delete pCore;
    delete[] pScreen;
}

// Ensures that when writing to the screen buffer, the core stops once it hits the
// bottom edge of the screen.
TEST(CoreTests, SetSmallHeightScreen)
{
    // Setup
    Core* pCore = new Core();

    // Create a screen buffer with a normal width, but small height.
    constexpr word widthChars = 40;
    constexpr word widthPixels = widthChars * 16;  // 16 pixels per CRTC char.
    constexpr word height = 2;
    constexpr int bufsize = widthPixels * height;

    // Allocate enough space for two lines, but tell the core the buffer height is one less than that.
    byte* pScreen = new byte[bufsize];
    memset(pScreen, 1, bufsize);
    pCore->SetScreen(pScreen, widthPixels, height - 1, widthPixels);

    // Act - run the core for two lines plus the number of overscan lines. Adding overscan lines is necessary
    //       to ensure we actually write into the screen buffer. The default total width is 0x40 chars, so
    //       double this for two lines. Note that one CRTC "char" is written every 4 ticks.
    pCore->RunUntil((0x40 * (2 + 16)) * 4, 0);

    // Verify - ensure that only a single line was written.
    for (word i = 0; i < bufsize; i++)
    {
        // The core should have written a single line of zero pixels in the screen buffer, and
        // the original ones in the second line should remain.
        ASSERT_EQ(pScreen[i], (i < widthPixels) ? 0 : 1);
    }

    delete pCore;
    delete[] pScreen;
}

// Ensure that after running a core for a while, calls to RunUntil should eventually return
// with stopAudioOverrun.
TEST(CoreTests, StopAudioOverrun)
{
    // Setup
    Core* pCore = new Core();

    // Act
    byte stopReason = pCore->RunUntil(4000000, stopAudioOverrun);

    // Verify
    ASSERT_EQ(stopReason, stopAudioOverrun);
    delete pCore;
}

// Ensures that if a core can no longer run due to audio overrun, running can be resumed by
// reading data from the audio buffers.
TEST(CoreTests, ResumeAfterAudioOverrun)
{
    // Setup
    Core* pCore = new Core();
    pCore->RunUntil(4000000, stopAudioOverrun);
    byte buffers[3][4000];
    byte* pBuffers[3] = { buffers[0], buffers[1], buffers[2] };
    qword beforeTicks = pCore->Ticks();

    // Act
    pCore->GetAudioBuffers(4000, pBuffers);
    pCore->RunUntil(4000000, stopAudioOverrun);

    // Verify
    ASSERT_GT(pCore->Ticks(), beforeTicks);
    delete pCore;
}

// Tests that a core can be serialized and deserialized back to the same state. Note this test could probably be improved to
// ensure the core is initially in a state where registers, memory, etc. aren't all zeros and thus more likely to catch errors
// when serialized then deserialized.
TEST(CoreTests, SerializeDeserialize)
{
    // Setup
    Core* pCore = new Core();
    StreamWriter writer;
    writer << (*pCore);
    bytevector state1;
    writer.CopyTo(state1);
    StreamReader reader(writer);

    // Act
    Core* pCore2 = new Core();
    reader >> (*pCore2);
    StreamWriter writer2;
    writer2 << (*pCore2);
    bytevector state2;
    writer2.CopyTo(state2);

    // Verify
    ASSERT_EQ(state1.size(), state2.size());

    for (size_t i = 0; i < state1.size(); i++)
    {
        ASSERT_EQ(state1[i], state2[i]);
    }

    delete pCore;
    delete pCore2;
}

TEST(CoreTests, RunUntilVSync)
{
    // Setup
    Core* pCore = new Core();
    qword ticksEnd = pCore->Ticks() + 4000000;
    int vSyncCount = 0;

    // Act
    while (pCore->Ticks() < ticksEnd)
    {
        pCore->RunUntil(ticksEnd, stopVSync);
        vSyncCount++;
    }

    // Verify - in one second, we should have 50 or 51 VSync's, roughly in line with a 50Hz refresh rate.
    ASSERT_GE(vSyncCount, 50);
    ASSERT_LE(vSyncCount, 51);
    delete pCore;
}

TEST(CoreTests, KeyPress)
{
    // Setup
    Core* pCore = new Core();
 
    // Act
    bool prevdown1 = pCore->KeyPress(65, true);
    bool prevdown2 = pCore->KeyPress(65, true);
    bool prevdown3 = pCore->KeyPress(65, false);

    // Verify
    ASSERT_TRUE(prevdown1);
    ASSERT_FALSE(prevdown2);
    ASSERT_TRUE(prevdown3);


}