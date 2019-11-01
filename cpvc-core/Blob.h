#pragma once

#include "StreamReader.h"
#include "StreamWriter.h"

// Class encapsulating a fixed-length buffer allocated on the heap.
// Created to avoid allocating large byte arrays on the stack, which would cause warnings (C6262) about stack size being exceeded.
template<class T, int S>
class Blob
{
public:
    Blob()
    {
        _pData = new T[S];
    }

    Blob(const T* pData)
    {
        _pData = new T[S];
        Copy(pData);
    }

    Blob(const Blob<T, S>& blob)
    {
        _pData = new T[S];
        Copy(blob._pData);
    }

    ~Blob()
    {
        delete[] _pData;
        _pData = nullptr;
    }

    void Fill(T value)
    {
        for (int i = 0; i < S; i++)
        {
            _pData[i] = value;
        }
    }

    operator T* ()
    {
        return _pData;
    };

    Blob<T, S>& operator=(const Blob<T, S>& blob)
    {
        Copy(blob._pData);

        return *this;
    }

    T& operator[](int i)
    {
        return _pData[i];
    }

private:
    void Copy(const T* pData)
    {
        for (int i = 0; i < S; i++)
        {
            _pData[i] = pData[i];
        }
    }

    T* _pData;

    friend StreamWriter& operator<<(StreamWriter& s, const Blob<T, S>& blob)
    {
        s.WriteArray(blob._pData, S);

        return s;
    }

    friend StreamReader& operator>>(StreamReader& s, Blob<T, S>& blob)
    {
        s.ReadArray(blob._pData, S);

        return s;
    }
};

