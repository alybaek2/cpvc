#pragma once

#include "StreamReader.h"
#include "StreamWriter.h"

// Class encapsulating a fixed-length buffer allocated on the heap.
// Created to avoid allocating large byte arrays on the stack, which would cause warnings (C6262) about stack size being exceeded.
template<int S>
class Blob
{
public:
    Blob()
    {
        _pData = new byte[S];
    }

    Blob(const byte* pData)
    {
        _pData = new byte[S];
        Copy(pData);
    }

    Blob(const Blob<S>& blob)
    {
        _pData = new byte[S];
        Copy(blob._pData);
    }

    ~Blob()
    {
        delete[] _pData;
        _pData = nullptr;
    }

    void Fill(byte value)
    {
        memset(_pData, value, S);
    }

    operator byte* ()
    {
        return _pData;
    };

    Blob<S>& operator=(const Blob<S>& blob)
    {
        Copy(blob._pData);

        return *this;
    }

    byte& operator[](int i)
    {
        return _pData[i];
    }

private:
    void Copy(const byte* pData)
    {
        memcpy(_pData, pData, S);
    }

    byte* _pData;

    friend StreamWriter& operator<<(StreamWriter& s, const Blob<S>& blob)
    {
        s.WriteArray(blob._pData, S);

        return s;
    }

    friend StreamReader& operator>>(StreamReader& s, Blob<S>& blob)
    {
        s.ReadArray(blob._pData, S);

        return s;
    }
};

