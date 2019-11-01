#pragma once

#include "common.h"
#include "StreamReader.h"
#include "StreamWriter.h"

class Sector
{
public:
    Sector();
    Sector(const Sector& sector);
    ~Sector();

    // Sector info...
    byte _track;
    byte _side;
    byte _id;
    byte _size;
    byte _fdcRegister1;
    byte _fdcRegister2;

    // Actual data...
    word _dataLength;
    bytevector _data;

    word DataLength();

    friend StreamWriter& operator<<(StreamWriter& s, const Sector& sector);
    friend StreamReader& operator>>(StreamReader& s, Sector& sector);
};
