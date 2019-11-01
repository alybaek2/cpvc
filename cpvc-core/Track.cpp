#include "Track.h"

Track::Track()
{
    _id = 0;
    _side = 0;
    _sectorSize = 0;
    _gap3Length = 0;
    _fillerByte = 0;
    _formatted = 0;
    _dataRate = 0;
    _recordingMode = 0;
    _numSectors = 0;
    _recordingMode = 0;
    _sectors.clear();
};

Track::Track(const Track& track)
{
    _id = track._id;
    _side = track._side;
    _sectorSize = track._sectorSize;
    _gap3Length = track._gap3Length;
    _fillerByte = track._fillerByte;
    _formatted = track._formatted;
    _dataRate = track._dataRate;
    _recordingMode = track._recordingMode;
    _numSectors = track._numSectors;
    _recordingMode = track._recordingMode;
    _sectors.clear();
    for (size_t i = 0; i < track._sectors.size(); i++)
    {
        _sectors.push_back(track._sectors.at(i));
    }
}

Track::~Track()
{
};

StreamWriter& operator<<(StreamWriter& s, const Track& track)
{
    s << track._id;
    s << track._side;
    s << track._sectorSize;
    s << track._gap3Length;
    s << track._fillerByte;
    s << track._formatted;
    s << track._dataRate;
    s << track._recordingMode;

    s << track._numSectors;
    s << track._sectors;

    return s;
}

StreamReader& operator>>(StreamReader& s, Track& track)
{
    s >> track._id;
    s >> track._side;
    s >> track._sectorSize;
    s >> track._gap3Length;
    s >> track._fillerByte;
    s >> track._formatted;
    s >> track._dataRate;
    s >> track._recordingMode;

    s >> track._numSectors;
    s >> track._sectors;

    return s;
}
