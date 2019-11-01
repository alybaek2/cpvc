#pragma once

#include "common.h"
#include "FDD.h"
#include "Disk.h"

constexpr byte readBufferSize = 4;

// Class representing the floppy drive controller.
class FDC
{
public:
    FDC();
    ~FDC();

    // Note that while the FDC technically does support 4 drives, the DS1 (Drive Select 1)
    // pin is physically disconnected on the CPC, meaning only 2 drives can be supported.
    FDD _drives[2];

    void Reset();

    // Emulates one microsecond of time for the FDC.
    void Tick();

public:
    byte _readBuffer[readBufferSize];
    byte _readBufferIndex;

    void Init();

    byte Read(const word& addr);
    void Write(const word& addr, byte b);

private:
    signed char _readTimeout;

    byte _mainStatus;
    byte _data;
    byte _dataDirection;
    byte _motor;
    byte _currentDrive;
    byte _currentHead;
    byte _status[4];

    bool _seekCompleted[2];
    bool _statusChanged[2];

    enum Phase
    {
        phCommand,
        phExecute,
        phResult
    };

    Phase _phase;
    byte _commandBytes[100];
    byte _commandByteCount;
    byte _execBytes[1024];
    word _execByteCount;
    word _execIndex;
    byte _resultBytes[100];
    byte _resultByteCount;
    byte _resultIndex;

    byte _stepReadTime;
    byte _headLoadTime;
    byte _headUnloadTime;
    byte _nonDmaMode;

    void ExecuteCommand();

    void SetMotor(bool motor);

    void SetData(byte b);
    byte GetStatus() const;
    byte GetData();
    void SetDataDirection(byte direction);
    void SetPhase(FDC::Phase p);
    void SelectDrive(byte dsByte);
    FDD& CurrentDrive();
    void PushReadBuffer(byte data);
    bool PopReadBuffer(byte& data);
    void SetDataReady(bool ready);
    void SetStatus(byte pins, byte values);
    byte CommandLength(byte command);

    void CmdReadData();
    void CmdReadDeletedData();
    void CmdWriteData();
    void CmdWriteDeletedData();
    void CmdReadTrack();
    void CmdReadId();
    void CmdFormatTrack();
    void CmdScanLow();
    void CmdScanLowOrEqual();
    void CmdScanHighOrEqual();
    void CmdRecalibrate();
    void CmdSenseInterruptStatus();

    void CmdSpecify();
    void CmdSeek();
    void CmdSenseDriveStatus();

    friend StreamWriter& operator<<(StreamWriter& s, const FDC& fdc);
    friend StreamReader& operator>>(StreamReader& s, FDC& fdc);

    friend StreamWriter& operator<<(StreamWriter& s, const Phase& fdc);
    friend StreamReader& operator>>(StreamReader& s, Phase& fdc);
};

