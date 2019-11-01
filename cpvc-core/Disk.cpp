#include "Disk.h"

Disk::Disk()
{
}

Disk::Disk(const Disk& disk)
{
    _tracks = disk._tracks;
}

Disk::~Disk()
{
}

bool Disk::LoadDisk(const byte* pBuffer, int size)
{
    bool retval = false;
    if (memcmp(pBuffer, "EXTENDED CPC DSK File\r\nDisk-Info\r\n", 0x22) == 0)
    {
        retval = LoadDiskV2(pBuffer);
    }
    else if (memcmp(pBuffer, "MV - CPC", 0x08) == 0)
    {
        retval = LoadDiskV1(pBuffer, size);
    }

    return retval;
}

bool Disk::LoadDiskV1(const byte* pBuffer, int size)
{
    // Read Disc Information Block...
    byte tracksCount = pBuffer[0x30];
    byte sideCount = pBuffer[0x31];

    word trackSize = *((word*)(pBuffer + 0x32));

    _tracks.clear();

    // Read tracks...
    const byte* pTrackInfo = pBuffer + 0x100;
    for (byte t = 0; t < tracksCount; t++)
    {
        for (byte s = 0; s < sideCount; s++)
        {
            // Check we're still in pBuffer...
            if (pTrackInfo >= (pBuffer + size))
            {
                return false;
            }

            // Read Track t, Side s...
            Track track;
            if (!LoadTrackV1(track, pTrackInfo, trackSize))
            {
                return false;
            }

            pTrackInfo += trackSize;

            _tracks.push_back(track);
        }
    }

    return true;
}

bool Disk::LoadDiskV2(const byte* pBuffer)
{
    byte tracksCount = pBuffer[0x30];
    byte sideCount = pBuffer[0x31];

    const byte* pTrackSizeTable = pBuffer + 0x34;
    const byte* pTrackInfo = pBuffer + 0x100;

    for (byte trackNumber = 0; trackNumber < tracksCount; trackNumber++)
    {
        for (byte sideNumber = 0; sideNumber < sideCount; sideNumber++)
        {
            word trackSize = (*pTrackSizeTable) * 0x100;

            Track track;
            LoadTrackV2(track, pTrackInfo);

            _tracks.push_back(track);

            pTrackSizeTable++;
            pTrackInfo += trackSize;
        }
    }

    return true;
}

bool Disk::LoadTrackV1(Track& track, const byte* pTrackInfo, word trackSize)
{
    // Signature...
    int c = memcmp(pTrackInfo, "Track-Info\r\n", 0x0c);
    if (c != 0)
    {
        return false;
    }

    track._id = pTrackInfo[0x10];
    track._side = pTrackInfo[0x11];

    track._sectorSize = pTrackInfo[0x14];
    track._numSectors = pTrackInfo[0x15];
    track._gap3Length = pTrackInfo[0x16];
    track._fillerByte = pTrackInfo[0x17];

    track._formatted = (trackSize != 0);

    // Allocate space for sectors...
    track._sectors.resize(track._numSectors);

    // Now read sectors...
    const byte* pSectorInfoList = pTrackInfo + 0x18;
    const byte* pSectorImage = pTrackInfo + 0x100;

    for (byte sec = 0; sec < track._numSectors; sec++)
    {
        Sector& sector = track._sectors[sec];

        sector._track = pSectorInfoList[0x00];
        sector._side = pSectorInfoList[0x01];
        sector._id = pSectorInfoList[0x02];
        sector._size = pSectorInfoList[0x03];
        sector._fdcRegister1 = pSectorInfoList[0x04];
        sector._fdcRegister2 = pSectorInfoList[0x05];
        sector._dataLength = sector._size * 0x100;

        sector._data.resize(sector._dataLength);
        memcpy((void*)&sector._data.front(), pSectorImage, sector._dataLength);

        pSectorInfoList += 8;
        pSectorImage += sector._dataLength;
    }

    return true;
}

bool Disk::LoadTrackV2(Track& track, const byte* pTrackInfo)
{
    int c = memcmp(pTrackInfo, "Track-Info\r\n", 0x0c);
    if (c != 0)
    {
        return false;
    }

    track._id = pTrackInfo[0x10];
    track._side = pTrackInfo[0x11];

    track._sectorSize = pTrackInfo[0x14];
    track._numSectors = pTrackInfo[0x15];
    track._gap3Length = pTrackInfo[0x16];
    track._fillerByte = pTrackInfo[0x17];

    // Extensions made for Extended DSK format...
    track._dataRate = pTrackInfo[0x1c];
    track._recordingMode = pTrackInfo[0x1d];

    // Allocate space for sectors...
    track._sectors.resize(track._numSectors);

    const byte* pSectorInfoList = pTrackInfo + 0x18;
    const byte* pSectorData = pTrackInfo + 0x100;

    for (byte s = 0; s < track._numSectors; s++)
    {
        Sector& sector = track._sectors.at(s);

        LoadSectorV2(sector, pSectorInfoList, pSectorData);
    }

    return true;
}

bool Disk::LoadSectorV2(Sector& sector, const byte*& pSectorInfoList, const byte*& pSectorData)
{
    sector._track = pSectorInfoList[0x00];
    sector._side = pSectorInfoList[0x01];
    sector._id = pSectorInfoList[0x02];
    sector._size = pSectorInfoList[0x03];
    sector._fdcRegister1 = pSectorInfoList[0x04];
    sector._fdcRegister2 = pSectorInfoList[0x05];
    sector._dataLength = *((word*)(pSectorInfoList + 0x06));

    sector._data.resize(sector._dataLength);
    memcpy((void*)&sector._data.front(), pSectorData, sector._dataLength);

    pSectorInfoList += 8;
    pSectorData += sector._dataLength;

    return true;
}

StreamWriter& operator<<(StreamWriter& s, const Disk& disk)
{
    s << disk._tracks;

    return s;
}

StreamReader& operator>>(StreamReader& s, Disk& disk)
{
    s >> disk._tracks;

    return s;
}
