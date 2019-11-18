#pragma once

#include "common.h"
#include <map>
#include <vector>

class StreamReader
{
public:
    StreamReader()
    {
        _bufferIndex = 0;
        _buffer.clear();
    }

    ~StreamReader()
    {
    }

    void Push(byte b)
    {
        _buffer.push_back(b);
    }

    StreamReader& operator>>(bool& data)
    {
        return Read<bool>(data);
    }

    StreamReader& operator>>(byte& data)
    {
        return Read<byte>(data);
    }

    StreamReader& operator>>(char& data)
    {
        return Read<char>(data);
    }

    StreamReader& operator>>(signed char& data)
    {
        return Read<signed char>(data);
    }

    StreamReader& operator>>(word& data)
    {
        return Read<word>(data);
    }

    StreamReader& operator>>(dword& data)
    {
        return Read<dword>(data);
    }

    StreamReader& operator>>(int& data)
    {
        return Read<int>(data);
    }

    StreamReader& operator>>(qword& data)
    {
        return Read<qword>(data);
    }

    template<class T, int BUFSIZE>
    StreamReader& operator>>(T(&data)[BUFSIZE])
    {
        for (int i = 0; i < BUFSIZE; i++)
        {
            (*this) >> data[i];
        }

        return (*this);
    }

    template<typename T>
    StreamReader& operator>>(std::vector<T>& vector)
    {
        size_t size = 0;
        (*this) >> size;

        vector.resize(size);
        for (size_t x = 0; x < size; x++)
        {
            (*this) >> vector.at(x);
        }

        return (*this);
    }

    template<class K, class V>
    StreamReader& operator>>(std::map<K, V>& map)
    {
        size_t count = 0;
        (*this) >> count;

        map.clear();

        for (size_t i = 0; i < count; i++)
        {
            byte slot = 0;
            (*this) >> slot;
            (*this) >> map[slot];
        }

        return (*this);
    }

    template<typename T>
    void ReadArray(T* pArray, size_t size)
    {
        for (size_t x = 0; x < size; x++)
        {
            (*this) >> pArray[x];
        }
    }

private:
    size_t _bufferIndex;
    bytevector _buffer;

    template<typename T>
    StreamReader& Read(T& data)
    {
        int count = sizeof(data);
        if ((_bufferIndex + count) > _buffer.size())
        {
            throw std::out_of_range("No more data in the buffer");
        }

        memcpy((byte*)& data, _buffer.data() + _bufferIndex, count);
        _bufferIndex += count;

        return (*this);
    }
};
