#include "gtest/gtest.h"
#include "../cpvc-core/StreamReader.h"
#include "helpers.h"

template<int BUFSIZE>
void LoadReader(StreamReader& reader, byte (&data)[BUFSIZE])
{
    for (byte d : data)
    {
        reader.Push(d);
    }
}

TEST(StreamReaderTests, Read) {
    // Setup
    byte data[] = {
        // bool
        false,
        // byte
        0x01,
        // char
        'A',
        // signed char,
        (byte) -100,
        // word
        0x02, 0x03,
        // dword
        0x04, 0x05, 0x06, 0x07,
        // qword
        0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f,
        // int
        0xfe, 0xff, 0xff, 0xff,
        // byte[]
        0x80, 0x81,
        // bytevector (size followed by data)
        0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x90, 0x91,
        // std::map (size followed by key/value pairs)
        0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x11, 0x21,
        0x12, 0x22
    };

    bool f;
    byte b;
    char c;
    signed char sc;
    word w;
    dword dw;
    qword qw;
    int i;
    byte ba[2] = { 0, 0 };
    bytevector bv;
    std::map<byte, byte> map;

    StreamReader reader;
    LoadReader(reader, data);

    // Act
    reader >> f;
    reader >> b;
    reader >> c;
    reader >> sc;
    reader >> w;
    reader >> dw;
    reader >> qw;
    reader >> i;
    reader >> ba;
    reader >> bv;
    reader >> map;

    // Verify
    ASSERT_EQ(f, false);
    ASSERT_EQ(b, 0x01);
    ASSERT_EQ(c, 'A');
    ASSERT_EQ(sc, -100);
    ASSERT_EQ(w, 0x0302);
    ASSERT_EQ(dw, 0x07060504);
    ASSERT_EQ(qw, 0x0f0e0d0c0b0a0908);
    ASSERT_EQ(i, -2);
    ASSERT_EQ(ba[0], 0x80);
    ASSERT_EQ(ba[1], 0x81);
    ASSERT_EQ(bv.size(), 2);
    ASSERT_EQ(bv.at(0), 0x90);
    ASSERT_EQ(bv.at(1), 0x91);
    ASSERT_EQ(map.size(), 2);
    ASSERT_EQ(map[0x11], 0x21);
    ASSERT_EQ(map[0x12], 0x22);
}

TEST(StreamReaderTests, ReadEndOfData) {
    // Setup
    byte data[] = { 0x01 };
    byte b;

    StreamReader reader;
    LoadReader(reader, data);

    // Act
    reader >> b;

    // Verify
    ASSERT_THROW(reader >> b, std::out_of_range);
}

TEST(StreamReaderTests, ReadNoData) {
    // Setup
    byte b;
    StreamReader reader;

    // Act and Verify
    ASSERT_THROW(reader >> b, std::out_of_range);
}

TEST(StreamReaderTests, ReadArray) {
    // Setup
    byte data[] = { 0x01, 0x02, 0x03 };
    byte b[3] = { 0x00, 0x00, 0x00 };

    StreamReader reader;
    LoadReader(reader, data);

    // Act
    reader.ReadArray((byte*)b, 3);

    // Verify
    ASSERT_EQ(b[0], 0x01);
    ASSERT_EQ(b[1], 0x02);
    ASSERT_EQ(b[2], 0x03);
}

TEST(StreamReaderTests, ReadArrayZeroSize) {
    // Setup
    byte data[] = { 0x01, 0x02, 0x03 };
    byte b[3] = { 0x00, 0x00, 0x00 };

    StreamReader reader;
    LoadReader(reader, data);

    // Act
    reader.ReadArray((byte*)b, 0);

    // Verify
    ASSERT_EQ(b[0], 0x00);
    ASSERT_EQ(b[1], 0x00);
    ASSERT_EQ(b[2], 0x00);
}

TEST(StreamReaderTests, ReadArrayNoStreamData) {
    // Setup
    StreamReader reader;

    // Act and Verify
    EXPECT_NO_THROW(reader.ReadArray<byte>(nullptr, 0));
}
