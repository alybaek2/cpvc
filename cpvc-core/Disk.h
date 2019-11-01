#pragma once

#include <vector>

#include "common.h"
#include "StreamReader.h"
#include "StreamWriter.h"
#include "Track.h"

class Disk
{
public:
    Disk();
    Disk(const Disk& disk);
    ~Disk();

    std::vector<Track> _tracks;

    bool LoadDisk(const byte* pBuffer, int size);
    bool LoadDiskV1(const byte* pBuffer, int size);
    bool LoadDiskV2(const byte* pBuffer);

    bool LoadTrackV1(Track& track, const byte* pTrackInfo, word trackSize);
    bool LoadTrackV2(Track& track, const byte* pTrackInfo);

    bool LoadSectorV2(Sector& sector, const byte*& pSectorInfoList, const byte*& pSectorData);

    friend StreamWriter& operator<<(StreamWriter& s, const Disk& disk);
    friend StreamReader& operator>>(StreamReader& s, Disk& disk);
};