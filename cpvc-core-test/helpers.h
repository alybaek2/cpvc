#include<vector>

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

static bytevector flagBytes = { 0x00, 0xFF };
static bytevector allBytes = Range<byte>(0x00, 0xFF);
static bytevector testBytes = { 0x00, 0x0F, 0x55, 0xAA, 0xF0, 0xFF };
static wordvector testAddresses = { 0x0000, 0x3FFF, 0x4000, 0x7FFF, 0x8000, 0xBFFF, 0xC000, 0xFFFF };
static std::vector<offset> testOffsets = { 0, 127, -128 };
