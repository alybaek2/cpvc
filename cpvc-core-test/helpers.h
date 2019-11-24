#include<vector>
#include "gtest/gtest.h"

template<typename T>
inline std::vector<T> Range(T first, T last)
{
    std::vector<T> bytes;
    for (int i = first; i <= last; i++)
    {
        bytes.push_back(i);
    }

    return bytes;
}

template<typename T, int BUFSIZE>
inline void ArraysEqual(const T(&a)[BUFSIZE], const T(&b)[BUFSIZE])
{
    for (int i = 0; i < BUFSIZE; i++)
    {
        ASSERT_EQ(a[i], b[i]);
    }
}

template<typename T, int BUFSIZE, int BUFSIZE2>
inline void ArraysEqual(const T(&a)[BUFSIZE][BUFSIZE2], const T(&b)[BUFSIZE][BUFSIZE2])
{
    for (int i = 0; i < BUFSIZE; i++)
    {
        ArraysEqual(a[i], b[i]);
    }
}

template<typename T, int BUFSIZE, int BUFSIZE2, int BUFSIZE3>
inline void ArraysEqual(const T(&a)[BUFSIZE][BUFSIZE2][BUFSIZE3], const T(&b)[BUFSIZE][BUFSIZE2][BUFSIZE3])
{
    for (int i = 0; i < BUFSIZE; i++)
    {
        ArraysEqual(a[i], b[i]);
    }
}

static bytevector flagBytes = { 0x00, 0xFF };
static bytevector allBytes = Range<byte>(0x00, 0xFF);
static bytevector testBytes = { 0x00, 0x0F, 0x55, 0xAA, 0xF0, 0xFF };
static wordvector testAddresses = { 0x0000, 0x3FFF, 0x4000, 0x7FFF, 0x8000, 0xBFFF, 0xC000, 0xFFFF };
static std::vector<offset> testOffsets = { 0, 127, -128 };
