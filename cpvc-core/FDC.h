#pragma once

#include "common.h"
#include "IBus.h"
#include "FDD.h"
#include "Disk.h"

// Status bits
constexpr byte statusDrive0Busy = 0x01;
constexpr byte statusDrive1Busy = 0x02;
constexpr byte statusDrive2Busy = 0x04;
constexpr byte statusDrive3Busy = 0x08;
constexpr byte statusControllerBusy = 0x10;
constexpr byte statusExecutionMode = 0x20;
constexpr byte statusTransferDirection = 0x40;
constexpr byte statusRequestMaster = 0x80;

// Commands
constexpr byte cmdSpecify = 0x03;
constexpr byte cmdSenseDriveStatus = 0x04;
constexpr byte cmdRecalibrate = 0x07;
constexpr byte cmdSenseInterruptStatus = 0x08;
constexpr byte cmdSeek = 0x0f;
constexpr byte cmdReadTrack = 0x02;
constexpr byte cmdWriteData = 0x05;
constexpr byte cmdReadData = 0x06;
constexpr byte cmdWriteDeletedData = 0x09;
constexpr byte cmdReadId = 0x0a;
constexpr byte cmdReadDeletedData = 0x0c;
constexpr byte cmdFormatTrack = 0x0d;
constexpr byte cmdScanLow = 0x11;
constexpr byte cmdScanLowOrEqual = 0x19;
constexpr byte cmdScanHighOrEqual = 0x1d;

// Data register directions
constexpr byte fdcDataIn = 0;
constexpr byte fdcDataOut = 1;

// Status register flags
constexpr byte st0NormalTerm = 0x00;
constexpr byte st0AbnormalTerm = 0x40;
constexpr byte st0InvalidCommand = 0x80;
constexpr byte st0AbnormalReadyTerm = 0xC0;
constexpr byte st0SeekEnd = 0x20;
constexpr byte st0EquipmentCheck = 0x10;
constexpr byte st0NotReady = 0x08;
constexpr byte st0HeadAddress = 0x04;

// Only drives 0 and 1 used by CPC
constexpr byte st0UnitSelect3 = 0x03;
constexpr byte st0UnitSelect2 = 0x02;
constexpr byte st0UnitSelect1 = 0x01;
constexpr byte st0UnitSelect0 = 0x00;

constexpr byte st1EndOfCylinder = 0x80;
constexpr byte st1DataError = 0x20;
constexpr byte st1Overrun = 0x10;
constexpr byte st1NoData = 0x04;
constexpr byte st1NotWritable = 0x02;
constexpr byte st1MissingAddress = 0x01;

constexpr byte st2ControlMark = 0x40;
constexpr byte st2DataError = 0x20;
constexpr byte st2WrongCylinder = 0x10;
constexpr byte st2ScanEqualHit = 0x80;
constexpr byte st2ScanNotSatisfied = 0x40;
constexpr byte st2BadCylinder = 0x20;
constexpr byte st2MissingAddress = 0x10;

constexpr byte st3Fault = 0x80;
constexpr byte st3WriteProtected = 0x40;
constexpr byte st3Ready = 0x20;
constexpr byte st3Track0 = 0x10;
constexpr byte st3TwoSide = 0x08;
constexpr byte st3HeadAddress = 0x04;
constexpr byte st3UnitSelect1 = 0x02;
constexpr byte st3UnitSelect0 = 0x01;

constexpr byte fdcReadTimeoutFM = 27;
constexpr byte fdcReadTimeoutMFM = 13;

constexpr byte commandLengths[32] = {
    1,
    1,
    9, // 2 - Read Track
    3, // 3 - Specify
    2, // 4 - Sense Drive Status
    9, // 5 - Write Data
    9, // 6 - Read Data
    2, // 7 - Recalibrate
    1, // 8 - Sense Interrupt Status
    9, // 9 - Write Deleted Data
    2, // 10 - Read Id
    1,
    9, // 12 - Read Deleted Data
    6, // 13 - Format Track
    1,
    3, // 15 - Seek
    1,
    9, // 17 - Scan Low
    1,
    1,
    1,
    1,
    1,
    1,
    1,
    9, // 25 - Scan Low or Equal
    1,
    1,
    1,
    9, // 29 - Scan High or Equal
    1,
    1
};

constexpr byte readBufferSize = 4;

// Class representing the floppy drive controller.
class FDC : public IBus
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

    byte Read(word addr);
    void Write(word addr, byte b);

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

