#include "gtest/gtest.h"
#include "../cpvc-core/CRTC.h"
#include "helpers.h"

TEST(CRTCTests, WriteOnlyRegister) {
    for (byte reg : Range<byte>(0x00, 0x0B))
    {
        for (byte value : allBytes)
        {
            bool requestInterrupt;
            CRTC crtc(requestInterrupt);

            // Select register
            crtc.Write(0xBC00, reg);

            // Write register
            crtc.Write(0xBD00, value);

            // Read register
            byte r = crtc.Read(0xBF00);

            ASSERT_EQ(0x00, r);
        }
    }
};

TEST(CRTCTests, ReadWriteRegister) {
    for (byte reg : Range<byte>(0x0C, 0x0F))
    {
        for (byte value : allBytes)
        {
            bool requestInterrupt;
            CRTC crtc(requestInterrupt);

            // Select register
            crtc.Write(0xBC00, reg);

            // Write register
            crtc.Write(0xBD00, value);

            // Read register
            byte r = crtc.Read(0xBF00);

            byte expected = value;
            if (reg == 0x0C || reg == 0x0E)
            {
                expected &= 0x3F;
            }

            ASSERT_EQ(expected, r);
        }
    }
};

TEST(CRTCTests, ReadOnlyRegister) {
    for (byte reg : Range<byte>(0x10, 0x11))
    {
        for (byte value : allBytes)
        {
            bool requestInterrupt;
            CRTC crtc(requestInterrupt);

            crtc._register[reg] = value;

            // Select register
            crtc.Write(0xBC00, reg);

            // Write register
            crtc.Write(0xBD00, ~value);

            // Read register
            byte r = crtc.Read(0xBF00);

            ASSERT_EQ(value, r);
        }
    }
};

TEST(CRTCTests, ReadNonExistentRegister) {
    for (byte reg : Range<byte>(0x12, 0xFF))
    {
        for (byte value : allBytes)
        {
            bool requestInterrupt;
            CRTC crtc(requestInterrupt);

            // Select register
            crtc.Write(0xBC00, reg);

            // Write register
            crtc.Write(0xBD00, value);

            // Read register
            byte r = crtc.Read(0xBF00);

            ASSERT_EQ(0x00, r);
        }
    }
};
