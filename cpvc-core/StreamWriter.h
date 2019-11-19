#pragma once

#include "common.h"
#include <map>
#include <vector>

class StreamWriter
{
public:
    StreamWriter()
    {
        _buffer.clear();
    };

    ~StreamWriter()
    {
    };

    size_t Size()
    {
        return _buffer.size();
    }

    size_t CopyTo(byte* pBuffer, size_t bufferSize)
    {
        size_t bytesToCopy = Size();
        if (bytesToCopy > bufferSize)
        {
            bytesToCopy = bufferSize;
        }

        memcpy(pBuffer, _buffer.data(), bytesToCopy);

        return bytesToCopy;
    }

    StreamWriter& operator<<(byte data)
    {
        return Write<byte>(data);
    }

    StreamWriter& operator<<(char data)
    {
        return Write<char>(data);
    }

    StreamWriter& operator<<(signed char data)
    {
        return Write<signed char>(data);
    }

    StreamWriter& operator<<(bool data)
    {
        return Write<bool>(data);
    }

    StreamWriter& operator<<(word data)
    {
        return Write<word>(data);
    }

    StreamWriter& operator<<(dword data)
    {
        return Write<dword>(data);
    }

    StreamWriter& operator<<(int data)
    {
        return Write<int>(data);
    }

    StreamWriter& operator<<(qword data)
    {
        return Write<qword>(data);
    }

    template<class T, int BUFSIZE>
    StreamWriter& operator<<(T(&data)[BUFSIZE])
    {
        for (int i = 0; i < BUFSIZE; i++)
        {
            (*this) << data[i];
        }

        return (*this);
    }

    template<class T>
    StreamWriter& operator<<(const std::vector<T>& vector)
    {
        size_t size = vector.size();
        (*this) << size;

        for (size_t x = 0; x < size; x++)
        {
            (*this) << vector.at(x);
        }

        return (*this);
    }

    template<class K, class V>
    StreamWriter& operator<<(const std::map<K, V>& map)
    {
        size_t count = map.size();
        (*this) << count;

        for (std::pair<K, V> kv : map)
        {
            (*this) << kv.first;
            (*this) << kv.second;
        }

        return (*this);
    }

    template<typename T>
    void WriteArray(T* pArray, size_t size)
    {
        for (size_t x = 0; x < size; x++)
        {
            (*this) << pArray[x];
        }
    }

private:
    bytevector _buffer;

    template<typename T>
    StreamWriter& Write(const T& data)
    {
        byte* p = (byte*)& data;
        for (int x = 0; x < sizeof(data); x++)
        {
            _buffer.push_back(p[x]);
        }

        return (*this);
    }
};
