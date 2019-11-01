#pragma once

#include "common.h"

class IPSG
{
public:
    virtual byte Read() = 0;
    virtual void Write(byte data) = 0;

    virtual void SetControl(bool bdir, bool bc1) = 0;
    virtual bool Bc1() = 0;
    virtual bool Bdir() = 0;
};
