#include "gtest/gtest.h"
#include "../cpvc-core/PSG.h"
#include "helpers.h"

TEST(PSGTests, ReadRegister) {
    for (byte reg : allBytes)
    {
        for (byte value : allBytes)
        {
            Keyboard keyboard;
            PSG psg(keyboard);

            psg.Reset();

            psg.SetControl(true, true);
            psg.Write(reg);

            psg.SetControl(true, false);

            if (reg == 14)
            {
                // Special case for register 14 (IO Port A)
                keyboard.SelectLine(5);

                for (int i = 0; i < 8; i++)
                {
                    keyboard.KeyPress(5, i, (value & (1 << i)) == 0);
                }
            }
            else
            {
                psg.Write(value);
            }

            // Read it back
            psg.SetControl(false, true);
            byte b = psg.Read();

            if (reg < 16)
            {
                ASSERT_EQ(b, value);
            }
            else
            {
                ASSERT_EQ(b, 0x00);
            }
        }
    }
}
