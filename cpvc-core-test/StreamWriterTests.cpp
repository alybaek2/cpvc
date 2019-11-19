#include "gtest/gtest.h"
#include "../cpvc-core/StreamWriter.h"
#include "helpers.h"
#include <array>

template<int BUFSIZE>
void CheckArray(StreamWriter writer, byte (&expected)[BUFSIZE])
{
    ASSERT_EQ(BUFSIZE, writer.Size());

    std::array<byte, BUFSIZE> streamData;
    writer.CopyTo(streamData.data(), BUFSIZE);

    for (int i = 0; i < BUFSIZE; i++)
    {
        ASSERT_EQ(expected[i], streamData[i]);
    }
}

TEST(StreamWriterTests, Write) {
    // Setup
    byte expectedData[] = {
        // bool
        false,
        // byte
        0x01,
        // char
        'A',
        // signed char,
        (byte)-100,
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

    bool f = false;
    byte b = 0x01;
    char c = 'A';
    signed char sc = -100;
    word w = 0x0302;
    dword dw = 0x07060504;
    qword qw = 0x0f0e0d0c0b0a0908;
    int i = -2;
    byte ba[2] = { 0x80, 0x81 };
    bytevector bv = { 0x90, 0x91 };
    std::map<byte, byte> map = { { 0x11, 0x21 }, { 0x12, 0x22 } };

    StreamWriter writer;

    // Act
    writer << f;
    writer << b;
    writer << c;
    writer << sc;
    writer << w;
    writer << dw;
    writer << qw;
    writer << i;
    writer << ba;
    writer << bv;
    writer << map;

    // Verify
    CheckArray(writer, expectedData);
}

TEST(StreamWriterTests, WriteArray) {
    // Setup
    byte data[] = { 0x01, 0x02, 0x03 };
    StreamWriter writer;

    // Act
    writer.WriteArray(data, sizeof(data) / sizeof(data[0]));

    // Verify
    CheckArray(writer, data);
}

TEST(StreamWriterTests, WriteArrayZeroSize) {
    // Setup
    StreamWriter writer;

    // Act
    writer.WriteArray<byte>(nullptr, 0);

    // Verify
    ASSERT_EQ(writer.Size(), 0);
}

TEST(StreamWriterTests, CopyToZeroLengthBuffer) {
    // Setup
    StreamWriter writer;
    byte buffer[1] = { 0 };

    // Act
    writer << (dword)0xffffffff;
    writer.CopyTo(buffer, 0);

    // Verify
    ASSERT_EQ(buffer[0], 0);
}

TEST(StreamWriterTests, CopyToSmallerBuffer) {
    // Setup
    StreamWriter writer;
    byte buffer[3] = { 0, 0, 0 };

    // Act
    writer << (dword)0xffffffff;
    writer.CopyTo(buffer, 3);

    // Verify
    ASSERT_EQ(buffer[0], 0xff);
    ASSERT_EQ(buffer[1], 0xff);
    ASSERT_EQ(buffer[2], 0xff);
}
