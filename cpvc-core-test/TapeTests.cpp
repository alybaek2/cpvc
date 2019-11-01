#include "gtest/gtest.h"
#include "../cpvc-core/Tape.h"
#include "helpers.h"

wordvector pulseLengths = { 802, 1999 };
bytevector pulseCountBytes = { 50, 255 };
wordvector pulseCountWords = { 50, 35 };
wordvector sync1s = { 39, 90 };
wordvector sync2s = { 13, 26 };
wordvector zeros = { 29, 48 };
wordvector ones = { 51, 18 };
wordvector sampleLengths = { 20, 109, 299 };
wordvector bitCounts = { 5, 8, 11 };
wordvector pauses = { 0, 1001 };

bytevector testData = { 0x17, 0x9A, 0xF2, 0xBC, 0xCD, 0x0A, 0x39 };
bytevector correctTapeHeader = {
    // Signature
    'Z', 'X', 'T', 'a', 'p', 'e', '!', 0x1A,
    // Major and minor revision
    0x01, 0x14
};

struct Tone
{
    Tone(bool startingLevel, word pulseCount, qword pulseLength)
    {
        _startingLevel = startingLevel;
        _pulseCount = pulseCount;
        _pulseLength = pulseLength;
    };

    bool _startingLevel;
    word _pulseCount;
    qword _pulseLength;
};

byte GetData(word bitIndex)
{
    return testData.at(bitIndex / 8);
}

word GetDataLength(word bitCount)
{
    return (bitCount + 7) / 8;
}

byte LastByteUsedBits(word bitCount)
{
    byte lastByteUsedBits = bitCount % 8;
    if (lastByteUsedBits == 0)
    {
        lastByteUsedBits = 8;
    }

    return lastByteUsedBits;
}

byte UseBits(word bitIndex, word bitCount)
{
    byte useBits = 8;
    if ((bitCount - bitIndex) < 8)
    {
        useBits = bitCount - bitIndex;
    }

    return useBits;
}

void AddByte(bytevector& buffer, byte b)
{
    buffer.push_back(b);
}

void AddWord(bytevector& buffer, word w)
{
    buffer.push_back(Low(w));
    buffer.push_back(High(w));
}

void AddTripleByte(bytevector& buffer, dword d)
{
    AddWord(buffer, Low(d));
    AddByte(buffer, Low(High(d)));
}

bool AddTone(std::vector<Tone>& expectedTones, bool startingLevel, word pulseCount, qword pulseLength)
{
    expectedTones.push_back(Tone(startingLevel, pulseCount, pulseLength));

    return startingLevel ^ ((pulseCount & 0x0001) != 0);
}

bool AddDataTones(std::vector<Tone>& expectedTones, bool startingLevel, byte data, byte usedBits, qword zeroPulseLength, qword onePulseLength)
{
    for (int b = 7; b >= 8 - usedBits; b--)
    {
        qword pulseLength = ((data & (1 << b)) == 0) ? zeroPulseLength : onePulseLength;
        expectedTones.push_back(Tone(startingLevel, 2, pulseLength));
    }

    return startingLevel;
}

bool AddPauseTones(std::vector<Tone>& expectedTones, bool startingLevel, word pause)
{
    if (pause > 0)
    {
        // Do 1ms at the opposite of the current level...
        startingLevel = AddTone(expectedTones, startingLevel, 1, 3500);
        startingLevel = AddTone(expectedTones, false, 1, (qword)(3500 * pause));

        return true;
    }

    return startingLevel;
}

void Check(bytevector& tapeBuffer, std::vector<Tone>& expectedTones)
{
    Tape tape;
    tape.Load(tapeBuffer);
    tape._motor = true;

    bool expectedLevel = true;
    qword ticks = 0;
    qword nextExpectedLevelChange = 0;

    for (size_t toneIndex = 0; toneIndex < expectedTones.size(); toneIndex++)
    {
        bool startingLevel = expectedTones[toneIndex]._startingLevel;
        word pulseCount = expectedTones[toneIndex]._pulseCount;
        qword pulseLength = expectedTones[toneIndex]._pulseLength;

        // Convert from the tape's 3.5MHz clock to the CPC's 4MHz clock...
        qword adjustedPulseLength = (8 * pulseLength) / 7;

        expectedLevel = startingLevel;

        for (int pulseIndex = 0; pulseIndex < pulseCount; pulseIndex++)
        {
            nextExpectedLevelChange += adjustedPulseLength;

            while (ticks < nextExpectedLevelChange)
            {
                if (!tape._playing)
                {
                    ASSERT_EQ(true, tape._playing);
                }

                if (expectedLevel != tape._level)
                {
                    ASSERT_EQ(expectedLevel, tape._level);
                }

                tape.Tick();
                ticks += 4;
            }

            if (pulseIndex != (pulseCount - 1) || (toneIndex != (expectedTones.size() - 1)))
            {
                expectedLevel = !expectedLevel;
            }
        }
    }

    if (tape._playing)
    {
        ASSERT_EQ(false, tape._playing);
    }
}

TEST(TapeTests, CorrectSignature) {
    Tape tape;
    bool result = tape.Load(correctTapeHeader);

    ASSERT_EQ(result, true);
}

TEST(TapeTests, IncorrectSignature) {
    bytevector buffer = 
    {
        // Signature
        'Z', 'X', 'T', 'a', 'p', 'e', '!', 0x18,
        // Major and minor revision
        0x01, 0x14
    };

    Tape tape;
    bool result = tape.Load(buffer);

    ASSERT_EQ(result, false);
}

TEST(TapeTests, ShortSignature) {
    bytevector buffer =
    {
        // Signature
        'Z', 'X', 'T', 'a', 'p', 'e', '!', 0x1A,
        // Major revision; minor revision is missing.
        0x01
    };

    Tape tape;
    bool result = tape.Load(buffer);

    ASSERT_EQ(result, false);
}

TEST(TapeTests, EmptyFile) {
    bytevector buffer = {};

    Tape tape;
    bool result = tape.Load(buffer);

    ASSERT_EQ(result, false);
}

TEST(TapeTests, ID10) {
    for (word pause : pauses)
    {
        for (word bitCount : bitCounts)
        {
            bool level = false;
            std::vector<Tone> expectedTones;
            bytevector tapeBuffer = correctTapeHeader;

            // Tape block ID 10
            AddByte(tapeBuffer, 0x10);
            AddWord(tapeBuffer, pause);
            AddWord(tapeBuffer, GetDataLength(bitCount));

            level = AddTone(expectedTones, level, ((GetData(0) & 0x80) != 0) ? 3223 : 8063, 2168);  // Pilot pulse
            level = AddTone(expectedTones, level, 1, 667);  // First sync pulse
            level = AddTone(expectedTones, level, 1, 735);  // Second sync pulse

            for (word bitIndex = 0; bitIndex < bitCount; bitIndex += 8)
            {
                byte data = GetData(bitIndex);

                AddByte(tapeBuffer, data);
                AddDataTones(expectedTones, level, data, 8, 855, 1710);
            }

            level = AddPauseTones(expectedTones, level, pause);

            Check(tapeBuffer, expectedTones);
        }
    }
}

TEST(TapeTests, ID11)
{
    for (word sync1 : sync1s)
    {
        for (word sync2 : sync2s)
        {
            for (word zero : zeros)
            {
                for (word one : ones)
                {
                    for (word pilotPulseLength : pulseLengths)
                    {
                        for (word pilotPulseCount : pulseCountWords)
                        {
                            for (word pause : pauses)
                            {
                                for (word bitCount : bitCounts)
                                {
                                    bool level = false;
                                    std::vector<Tone> expectedTones;
                                    bytevector tapeBuffer = correctTapeHeader;

                                    // Tape block ID 11
                                    AddByte(tapeBuffer, 0x11);
                                    AddWord(tapeBuffer, pilotPulseCount);
                                    AddWord(tapeBuffer, sync1);
                                    AddWord(tapeBuffer, sync2);
                                    AddWord(tapeBuffer, zero);
                                    AddWord(tapeBuffer, one);
                                    AddWord(tapeBuffer, pilotPulseLength);
                                    AddByte(tapeBuffer, LastByteUsedBits(bitCount));
                                    AddWord(tapeBuffer, pause);
                                    AddTripleByte(tapeBuffer, GetDataLength(bitCount));

                                    level = AddTone(expectedTones, level, pilotPulseLength, pilotPulseCount);
                                    level = AddTone(expectedTones, level, 1, sync1);
                                    level = AddTone(expectedTones, level, 1, sync2);

                                    for (word b = 0; b < bitCount; b += 8)
                                    {
                                        byte data = GetData(b);

                                        AddByte(tapeBuffer, data);
                                        level = AddDataTones(expectedTones, level, data, UseBits(b, bitCount), zero, one);
                                    }

                                    level = AddPauseTones(expectedTones, level, pause);

                                    Check(tapeBuffer, expectedTones);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

TEST(TapeTests, ID12)
{
    for (word pulseLength : pulseLengths)
    {
        for (word pulseCount : pulseCountWords)
        {
            bool level = false;
            std::vector<Tone> expectedTones;
            bytevector tapeBuffer = correctTapeHeader;

            // Tape block ID 12
            AddByte(tapeBuffer, 0x12);
            AddWord(tapeBuffer, pulseLength);
            AddWord(tapeBuffer, pulseCount);

            level = AddTone(expectedTones, level, pulseCount, pulseLength);

            Check(tapeBuffer, expectedTones);
        }
    }
}

TEST(TapeTests, ID13)
{
    for (byte pulseCount : pulseCountBytes)
    {
        bool level = false;
        std::vector<Tone> expectedTones;
        bytevector tapeBuffer = correctTapeHeader;

        // Tape block ID 13
        AddByte(tapeBuffer, 0x13);
        AddByte(tapeBuffer, pulseCount);

        for (word i = 0; i < pulseCount; i++)
        {
            word pulseLength = i + 1;
            AddWord(tapeBuffer, pulseLength);
            level = AddTone(expectedTones, level, 1, pulseLength);
        }

        Check(tapeBuffer, expectedTones);
    }
}

TEST(TapeTests, ID14)
{
    for (word zero : zeros)
    {
        for (word one : ones)
        {
            for (word pause : pauses)
            {
                for (word bitCount : bitCounts)
                {
                    bool level = false;
                    std::vector<Tone> expectedTones;
                    bytevector tapeBuffer = correctTapeHeader;

                    // Tape block ID 14
                    AddByte(tapeBuffer, 0x14);
                    AddWord(tapeBuffer, zero);
                    AddWord(tapeBuffer, one);
                    AddByte(tapeBuffer, LastByteUsedBits(bitCount));
                    AddWord(tapeBuffer, pause);
                    AddTripleByte(tapeBuffer, GetDataLength(bitCount));

                    for (word bitIndex = 0; bitIndex < bitCount; bitIndex += 8)
                    {
                        byte data = GetData(bitIndex);

                        AddByte(tapeBuffer, data);
                        AddDataTones(expectedTones, level, data, UseBits(bitIndex, bitCount), zero, one);
                    }

                    level = AddPauseTones(expectedTones, level, pause);

                    Check(tapeBuffer, expectedTones);
                }
            }
        }
    }
}

TEST(TapeTests, ID15)
{
    for (word sampleLength : sampleLengths)
    {
        for (word pause : pauses)
        {
            for (word bitCount : bitCounts)
            {
                bool level = false;
                std::vector<Tone> expectedTones;
                bytevector tapeBuffer = correctTapeHeader;

                // Tape block ID 15
                AddByte(tapeBuffer, 0x15);
                AddWord(tapeBuffer, sampleLength);
                AddWord(tapeBuffer, pause);
                AddByte(tapeBuffer, LastByteUsedBits(bitCount));
                AddTripleByte(tapeBuffer, GetDataLength(bitCount));

                for (word bitIndex = 0; bitIndex < bitCount; bitIndex += 8)
                {
                    byte data = testData.at(bitIndex / 8);
                    byte useBits = UseBits(bitIndex, bitCount);

                    AddByte(tapeBuffer, data);
                    for (int i = 0; i < useBits; i++)
                    {
                        level = Bit(data, 7);
                        expectedTones.push_back(Tone(level, 1, sampleLength));
                        data = data << 1;
                    }
                }
                
                level = AddPauseTones(expectedTones, !level, pause);

                Check(tapeBuffer, expectedTones);
            }
        }
    }
}

TEST(TapeTests, ID20)
{
    for (word pause1 : pauses)
    {
        for (word pause2 : pauses)
        {
            bool level = false;
            std::vector<Tone> expectedTones;
            bytevector tapeBuffer = correctTapeHeader;

            // Tape block ID 20
            AddByte(tapeBuffer, 0x20);
            AddWord(tapeBuffer, pause1);

            // Tape block ID 20
            AddByte(tapeBuffer, 0x20);
            AddWord(tapeBuffer, pause2);

            level = AddPauseTones(expectedTones, level, pause1);

            // If the first pause is zero, then the tape should be stopped.
            if (pause1 != 0)
            {
                level = AddPauseTones(expectedTones, level, pause2);
            }

            Check(tapeBuffer, expectedTones);
        }
    }
}
