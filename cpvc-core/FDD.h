#pragma once

#include "common.h"

#include "Disk.h"

struct CHRN
{
    CHRN()
    {
        _cylinder = 0;
        _head = 0;
        _record = 0;
        _num = 0;
    };

    byte _cylinder;      // Track
    byte _head;          // Always 0 for Amstrad floppies
    byte _record;        // Cector
    byte _num;           // Number of bytes in sector
};

// Class representing a floppy drive.
class FDD
{
public:
    FDD();
    ~FDD();

    // Member variables describing the location of the read head.
    byte _currentSector;
    size_t _currentTrack;

    bool _hasDisk;
    Disk _disk;

    void Init();

    void Eject();

    bool Load(Disk& d);

    // Floppy Drive functions...
    bool IsReady();

    bool Seek(const byte cylinder);

    bool ReadId(CHRN& chrn);

    Track& CurrentTrack();

    Sector& CurrentSector();

    bool WriteData(
        const byte cylinder,
        const byte head,
        const byte sector,
        const byte numBytes,
        const byte endOfTrack,
        const byte gapLength,
        const byte dataLength,
        byte* pBuffer,
        word bufferSize);

    bool ReadData(
        const byte cylinder,
        const byte head,
        const byte sector,
        const byte numBytes,
        const byte endOfTrack,
        const byte gapLength,
        const byte dataLength,
        byte*& pBuffer,
        word& bufferSize);

    void ReadDataResult(byte& cylinder, byte& head, byte& sector, byte& numBytes);

    byte GetTrack();

private:
    friend StreamWriter& operator<<(StreamWriter& s, const FDD& fdd);
    friend StreamReader& operator>>(StreamReader& s, FDD& fdd);
};

