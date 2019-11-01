#pragma once

#include "common.h"

class IBus
{
public:
    virtual byte Read(word address) = 0;
    virtual void Write(word address, byte data) = 0;
};

class IBusNoAddress
{
public:
    virtual byte Read() = 0;
    virtual void Write(byte data) = 0;
};
