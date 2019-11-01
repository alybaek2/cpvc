#pragma once

#include <vector>

#include "common.h"
#include "StreamReader.h"
#include "StreamWriter.h"
#include "Sector.h"

class Track
{
public:
    Track();
    Track(const Track& track);
    ~Track();

    // Track info...
    byte _id;
    byte _side;
    byte _sectorSize;
    byte _gap3Length;
    byte _fillerByte;
    bool _formatted;

    byte _dataRate;
    byte _recordingMode;

    // Sectors...
    byte _numSectors;
    std::vector<Sector> _sectors;

    friend StreamWriter& operator<<(StreamWriter& s, const Track& track);
    friend StreamReader& operator>>(StreamReader& s, Track& track);
};
