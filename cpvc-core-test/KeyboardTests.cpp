#include "gtest/gtest.h"
#include "../cpvc-core/Keyboard.h"
#include "helpers.h"

TEST(KeyboardTests, OneKey) {
    for (byte line : Range<byte>(0, 9))
    {
        for (byte bit : Range<byte>(0, 7))
        {
            Keyboard keyboard;

            // Test keys "down"
            keyboard.KeyPress(line, bit, true);

            for (byte checkLineDown : Range<byte>(0, 9))
            {
                keyboard.SelectLine(checkLineDown);
                byte matrixLine = keyboard.ReadSelectedLine();
                if (checkLineDown == line)
                {
                    ASSERT_EQ((byte)~(1 << bit), matrixLine);
                }
                else
                {
                    ASSERT_EQ(0xFF, matrixLine);
                }
            }

            // Test keys "up"
            keyboard.KeyPress(line, bit, false);

            for (byte checkLineUp : Range<byte>(0, 9))
            {
                keyboard.SelectLine(checkLineUp);
                ASSERT_EQ(0xFF, keyboard.ReadSelectedLine());
            }
        }
    }
}

TEST(KeyboardTests, TwoKeys) {
    for (byte line0 : Range<byte>(0, 9))
    {
        for (byte line1 : Range<byte>(0, 9))
        {
            for (byte bit0 : Range<byte>(0, 7))
            {
                for (byte bit1 : Range<byte>(0, 7))
                {
                    byte expectedMatrix[10];
                    for (int i = 0; i < 10; i++)
                    {
                        expectedMatrix[i] = 0xFF;
                    }

                    expectedMatrix[line0] &= (byte)~(1 << bit0);
                    expectedMatrix[line1] &= (byte)~(1 << bit1);

                    Keyboard keyboard;

                    // Test keys "down"
                    keyboard.KeyPress(line0, bit0, true);
                    keyboard.KeyPress(line1, bit1, true);

                    for (int i = 0; i < 10; i++)
                    {
                        keyboard.SelectLine(i);
                        ASSERT_EQ(expectedMatrix[i], keyboard.ReadSelectedLine());
                    }

                    // Test keys "up"
                    keyboard.KeyPress(line0, bit0, false);
                    keyboard.KeyPress(line1, bit1, false);

                    for (byte checkLineUp : Range<byte>(0, 9))
                    {
                        keyboard.SelectLine(checkLineUp);
                        ASSERT_EQ(0xFF, keyboard.ReadSelectedLine());
                    }
                }
            }
        }
    }
};

TEST(KeyboardTests, ThreeKeysClash) {
    for (byte line0 : Range<byte>(0, 9))
    {
        for (byte line1 : Range<byte>(0, 9))
        {
            if (line1 == line0)
            {
                continue;
            }

            for (byte bit0 : Range<byte>(0, 7))
            {
                for (byte bit1 : Range<byte>(0, 7))
                {
                    if (bit1 == bit0)
                    {
                        continue;
                    }

                    Keyboard keyboard;

                    // Test that three keys down causes keyboard clash
                    keyboard.KeyPress(line0, bit0, true);
                    keyboard.KeyPress(line0, bit1, true);
                    keyboard.KeyPress(line1, bit0, true);

                    byte expected = 0xFF & (~(1 << bit0)) & (~(1 << bit1));

                    for (byte checkLine : Range<byte>(0, 9))
                    {
                        keyboard.SelectLine(checkLine);
                        byte matrixLine = keyboard.ReadSelectedLine();
                        if (checkLine == line0 || checkLine == line1)
                        {
                            ASSERT_EQ(expected, matrixLine);
                        }
                        else
                        {
                            ASSERT_EQ(0xFF, matrixLine);
                        }
                    }

                    // Test that one of the three keys going back up removes the clash
                    keyboard.KeyPress(line0, bit0, false);

                    for (byte checkLineUp : Range<byte>(0, 9))
                    {
                        keyboard.SelectLine(checkLineUp);
                        byte matrixLine = keyboard.ReadSelectedLine();
                        if (checkLineUp == line0)
                        {
                            byte expected = 0xFF & (~(1 << bit1));
                            ASSERT_EQ(expected, matrixLine);
                        }
                        else if (checkLineUp == line1)
                        {
                            byte expected = 0xFF & (~(1 << bit0));
                            ASSERT_EQ(expected, matrixLine);
                        }
                        else
                        {
                            ASSERT_EQ(0xFF, matrixLine);
                        }
                    }
                }
            }
        }
    }
};
