#include "FDC.h"

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

FDC::FDC()
{
}

FDC::~FDC()
{
}

void FDC::Init()
{
    Reset();

    _drives[0].Init();
    _drives[1].Init();
}

void FDC::Reset()
{
    _mainStatus = statusRequestMaster;
    _data = 0;
    SetDataDirection(fdcDataIn);

    _motor = 0;

    _currentDrive = 0;
    _currentHead = 0;

    _status[0] = 0;
    _status[1] = 0;
    _status[2] = 0;
    _status[3] = 0;

    _seekCompleted[0] = false;
    _seekCompleted[1] = false;
    _statusChanged[0] = true;
    _statusChanged[1] = true;

    SetPhase(phCommand);

    memset(&_commandBytes, 0, sizeof(_commandBytes));
    _commandByteCount = 0;
    memset(&_execBytes, 0, sizeof(_execBytes));
    _execByteCount = 0;
    _execIndex = 0;
    memset(&_resultBytes, 0, sizeof(_resultBytes));
    _resultByteCount = 0;
    _resultIndex = 0;

    _stepReadTime = 0;
    _headLoadTime = 0;
    _headUnloadTime = 0;
    _nonDmaMode = 0;

    memset(&_readBuffer, 0, sizeof(_readBuffer));
    _readBufferIndex = 0;

    _readTimeout = 0;
}

byte FDC::Read(word addr)
{
    byte ret = 0x00;

    bool bit0 = Bit(addr, 0);
    bool bit8 = Bit(addr, 8);
    if (bit8)
    {
        if (bit0)
        {
            // Data register
            ret = GetData();
        }
        else
        {
            // Main status register
            ret = GetStatus();
        }
    }

    return ret;
}

void FDC::SetMotor(bool motor)
{
    _motor = motor;

    _statusChanged[0] = true;
    _statusChanged[1] = true;
}

void FDC::Write(word addr, byte b)
{
    byte retval = 0;

    switch (addr & 0x0101)
    {
    case 0x0000:
        SetMotor(b & 0x01);
        break;
    case 0x0001:
        break;
    case 0x0100:
        break;
    case 0x0101:
        SetData(b);
        break;
    }
}

void FDC::ExecuteCommand()
{
    SetPhase(phExecute);

    switch (_commandBytes[0] & 0x1f)
    {
    case cmdSpecify:               CmdSpecify();                break;
    case cmdSenseDriveStatus:      CmdSenseDriveStatus();       break;
    case cmdRecalibrate:           CmdRecalibrate();            break;
    case cmdSenseInterruptStatus:  CmdSenseInterruptStatus();   break;
    case cmdSeek:                  CmdSeek();                   break;
    case cmdReadTrack:             CmdReadTrack();              break;
    case cmdWriteData:             CmdWriteData();              break;
    case cmdReadData:              CmdReadData();               break;
    case cmdWriteDeletedData:      CmdWriteDeletedData();       break;
    case cmdReadId:                CmdReadId();                 break;
    case cmdReadDeletedData:       CmdReadDeletedData();        break;
    case cmdFormatTrack:           CmdFormatTrack();            break;
    case cmdScanLow:               CmdScanLow();                break;
    case cmdScanLowOrEqual:        CmdScanLowOrEqual();         break;
    case cmdScanHighOrEqual:       CmdScanHighOrEqual();        break;
    default:
        break;
    }

    _commandByteCount = 0;
}

void FDC::SetData(byte b)
{
    switch (_phase)
    {
    case phCommand:
        _commandBytes[_commandByteCount] = b;
        _commandByteCount++;

        if (_commandByteCount == CommandLength(_commandBytes[0]))
        {
            // Enough bytes have been written to execute a command...
            ExecuteCommand();
        }
        break;
    case phExecute:
        _execBytes[_execIndex] = b;
        _execIndex++;

        if (_execIndex >= _execByteCount)
        {
            switch (_commandBytes[0] & 0x1f)
            {
            case cmdWriteData:
                _resultBytes[0] = _status[0];
                _resultBytes[1] = _status[1];
                _resultBytes[2] = _status[2];

                CurrentDrive().WriteData(
                    _commandBytes[2],
                    _commandBytes[3],
                    _commandBytes[4],
                    _commandBytes[5],
                    0,
                    0,
                    (byte)_execByteCount,
                    (byte*)&_execBytes,
                    _execByteCount);

                break;
            }

            _execIndex = 0;
            _execByteCount = 0;
            SetPhase(phResult);
        }

        break;
    case phResult:
        break;
    }
}

byte FDC::GetStatus() const
{
    byte retval = _mainStatus;
    if (_phase == phResult)
    {
        // If the FDC is returning results, data direction is FDC to CPU and FDC is busy.
        retval |= (statusTransferDirection | statusControllerBusy);
    }
    else if (_phase == phCommand)
    {
        if (_commandByteCount > 0)
        {
            retval |= statusControllerBusy;
        }
    }

    return retval;
}

byte FDC::GetData()
{
    byte retval = 0;

    switch (_phase)
    {
    case phCommand:
        break;
    case phExecute:
        PopReadBuffer(retval);

        if ((_execIndex >= _execByteCount) && (_readBufferIndex == 0))
        {
            _execIndex = 0;
            _execByteCount = 0;
            SetPhase(phResult);

            switch (_commandBytes[0] & 0x1f)
            {
            case cmdReadData:
                _status[0] |= st0AbnormalTerm;
                _status[1] |= st1EndOfCylinder;

                _resultBytes[0] = _status[0];
                _resultBytes[1] = _status[1];
                _resultBytes[2] = _status[2];

                SetDataReady(true);

                break;
            }
        }

        break;
    case phResult:
        retval = _resultBytes[_resultIndex];
        _resultIndex++;

        if (_resultIndex >= _resultByteCount)
        {
            _resultIndex = 0;
            _resultByteCount = 0;
            SetPhase(phCommand);

            SetDataDirection(fdcDataIn);
        }

        break;
    }

    return retval;
}

void FDC::SetDataDirection(byte direction)
{
    _dataDirection = direction;

    _mainStatus = (_mainStatus & ~0x40) | ((_dataDirection == fdcDataOut) ? 0x40 : 0x00);
}

void FDC::SetPhase(FDC::Phase p)
{
    _phase = p;

    switch (_phase)
    {
    case phCommand:
        SetDataDirection(fdcDataIn);
        _mainStatus &= ~statusExecutionMode;

        SetDataReady(true);

        break;
    case phExecute:
        if (_nonDmaMode)
        {
            _mainStatus |= statusExecutionMode;
        }
        break;
    case phResult:
        _mainStatus &= ~statusExecutionMode;
        break;
    }
}

void FDC::SelectDrive(byte dsByte)
{
    // The second byte of most commands specifies HD, DS1, and DS0 in bits 2, 1, and 0.
    // Since DS1 is disconnected in the CPC, only two drives are available.
    _currentDrive = (dsByte & 0x03);
    _currentHead = (dsByte & 0x04) >> 2;

    // HD, DS1, and DS0 are set in two status registers...
    _status[0] = (_status[0] & 0xF8) | (dsByte & 0x07);
    _status[3] = (_status[0] & 0xF8) | (dsByte & 0x07);
}

FDD& FDC::CurrentDrive()
{
    // DS1 pin is disconnected on the Amstrad...
    return _drives[_currentDrive & 0x01];
}

void FDC::PushReadBuffer(byte data)
{
    // Check for buffer overrun.
    if (_readBufferIndex >= readBufferSize)
    {
        _readBufferIndex = readBufferSize - 1;
        for (int g = 1; g < readBufferSize; g++)
        {
            _readBuffer[g - 1] = _readBuffer[g];
        }

        _status[1] |= st1Overrun;
    }

    _readBuffer[_readBufferIndex] = data;
    _readBufferIndex++;

    SetDataReady(true);
};

bool FDC::PopReadBuffer(byte& data)
{
    if (_readBufferIndex == 0)
    {
        return false;
    }

    data = _readBuffer[0];
    for (int g = 1; g < readBufferSize; g++)
    {
        _readBuffer[g - 1] = _readBuffer[g];
    }

    _readBufferIndex--;

    if (_readBufferIndex == 0)
    {
        // Don't set this to false! Still have bytes left to read in the results phase.
        SetDataReady(false);
    }

    return true;
}

void FDC::SetDataReady(bool ready)
{
    SetStatus(statusRequestMaster, ready ? statusRequestMaster : 0x00);
}

void FDC::SetStatus(byte pins, byte values)
{
    _mainStatus = (_mainStatus & ~pins) | (values & pins);
}

byte FDC::CommandLength(byte command)
{
    return commandLengths[command & 0x1f];
}

// Emulates one microsecond of time for the FDC.
void FDC::Tick()
{
    // Read command
    if ((_commandBytes[0] & 0x1f) == cmdReadData)
    {
        _readTimeout--;
        if (_readTimeout <= 0)
        {
            // Grab next byte for disk and stick it in the read buffer.
            if (_execIndex < _execByteCount)
            {
                FDD& drive = CurrentDrive();
                Sector& sec = drive._disk._tracks[drive._currentTrack]._sectors[drive._currentSector];
                byte b = sec._data.at(_execIndex);

                PushReadBuffer(b);
                _execIndex++;

                byte* pBuffer = NULL;
                word bufSize = 0;

                if (_execIndex == (_commandBytes[5] * 0x100))
                {
                    _commandBytes[4]++;
                    if (_commandBytes[4] <= _commandBytes[6])
                    {
                        drive.ReadData(
                            _commandBytes[2],
                            _commandBytes[3],
                            _commandBytes[4],
                            _commandBytes[5],
                            _commandBytes[6],
                            _commandBytes[7],
                            _commandBytes[8],
                            pBuffer,
                            bufSize);

                        _execIndex = 0;
                    }
                    else
                    {
                        // Last sector has been read. Read Data execution phase is done.
                        _resultBytes[0] = _status[0];
                        _resultBytes[1] = _status[1];
                        _resultBytes[2] = _status[2];

                        _resultBytes[3] = _commandBytes[2];
                        _resultBytes[4] = _commandBytes[3];
                        _resultBytes[5] = _commandBytes[4];
                        _resultBytes[6] = _commandBytes[5];

                        // Get next sector...
                        byte oldTrack = _resultBytes[3];
                        drive.ReadDataResult(_resultBytes[3], _resultBytes[4], _resultBytes[5], _resultBytes[6]);

                        if (_resultBytes[3] != oldTrack)
                        {
                            // Reached end of the track...
                            _status[1] |= st1EndOfCylinder;
                        }
                    }
                }

                _readTimeout = fdcReadTimeoutFM;
            }
        }
    }
}

void FDC::CmdReadData()
{
    SetDataDirection(fdcDataOut);
    SetDataReady(false);

    SelectDrive(_commandBytes[1]);

    byte& cylinder = _commandBytes[2];
    byte& head = _commandBytes[3];
    byte& sector = _commandBytes[4];
    byte& numBytes = _commandBytes[5];

    byte& endOfTrack = _commandBytes[6];
    byte& gapLength = _commandBytes[7];
    byte& dataLength = _commandBytes[8];

    if (!CurrentDrive()._hasDisk)
    {
        SetPhase(phResult);

        _status[0] = st0AbnormalTerm | st0EquipmentCheck | st0NotReady;

        _resultByteCount = 7;
        _resultBytes[0] = _status[0];
        _resultBytes[1] = 0;
        _resultBytes[2] = 0;

        _resultBytes[3] = cylinder;
        _resultBytes[4] = head;
        _resultBytes[5] = sector;
        _resultBytes[6] = numBytes;

        return;
    }

    // Read the sector and place it in the results array...
    byte* pBuffer = nullptr;
    word bufferSize = 0;

    // Seek to the correct track first...
    CurrentDrive()._currentTrack = cylinder;
    CurrentDrive()._currentSector = 0;

    bool res = CurrentDrive().ReadData(cylinder, head, sector, numBytes, endOfTrack, gapLength, dataLength, pBuffer, bufferSize);

    _execIndex = 0;
    _execByteCount = bufferSize;
    memcpy(_execBytes, pBuffer, bufferSize);

    SetPhase(phExecute);

    _readTimeout = fdcReadTimeoutFM;
    _readBufferIndex = 0;

    _resultByteCount = 7;
    _resultBytes[0] = 0;
    _resultBytes[1] = 0;
    _resultBytes[2] = 0;

    _status[0] = 0;
    _status[1] = 0;
    _status[2] = 0;
    _status[3] = 0;

    // Return adjusted CHRN info in result phase.
    // See http://cpctech.cpc-live.com/docs/i8272/8272sp.htm
    _resultBytes[3] = cylinder;
    _resultBytes[4] = head;
    _resultBytes[5] = sector;
    _resultBytes[6] = numBytes;
}

void FDC::CmdReadDeletedData()
{
    SetDataDirection(fdcDataOut);
}

void FDC::CmdWriteData()
{
    SetDataDirection(fdcDataIn);
    SelectDrive(_commandBytes[1]);

    byte& cylinder = _commandBytes[2];
    byte& head = _commandBytes[3];
    byte& sector = _commandBytes[4];
    byte& numBytes = _commandBytes[5];

    byte& endOfTrack = _commandBytes[6];
    byte& gapLength = _commandBytes[7];
    byte& dataLength = _commandBytes[8];

    if (!CurrentDrive()._hasDisk)
    {
        SetPhase(phResult);

        _status[0] = st0AbnormalTerm | st0EquipmentCheck | st0NotReady;

        _resultByteCount = 7;
        _resultBytes[0] = _status[0];
        _resultBytes[1] = 0;
        _resultBytes[2] = 0;

        _resultBytes[3] = cylinder;
        _resultBytes[4] = head;
        _resultBytes[5] = sector;
        _resultBytes[6] = numBytes;

        return;
    }

    // Read the sector and place it in the results array...
    byte* pBuffer = nullptr;
    word bufferSize = numBytes * 0x0100;

    _execIndex = 0;
    _execByteCount = bufferSize;

    SetPhase(phExecute);

    _resultByteCount = 7;
    _resultBytes[0] = 0;
    _resultBytes[1] = 0;
    _resultBytes[2] = 0;

    // Return adjusted CHRN info in result phase.
    // See http://cpctech.cpc-live.com/docs/i8272/8272sp.htm
    _resultBytes[3] = cylinder;
    _resultBytes[4] = head;
    _resultBytes[5] = sector;
    _resultBytes[6] = numBytes;
}

void FDC::CmdWriteDeletedData()
{
    SetDataDirection(fdcDataIn);
}

void FDC::CmdReadTrack()
{
    SetDataDirection(fdcDataOut);
}

void FDC::CmdReadId()
{
    SetDataDirection(fdcDataOut);

    SelectDrive(_commandBytes[1]);

    _status[2] = 0;

    // Report first ID information on current cylinder
    // If none found, set IC on ST0 to 1 and MA on ST1 to 1
    CHRN chrn;
    if (!CurrentDrive().ReadId(chrn))
    {
        _status[0] = (_status[0] & 0x1f) | st0AbnormalTerm | st0NotReady;
        _status[1] = 0;
    }
    else
    {
        _status[0] = 0;
        _status[1] = 0;
    }

    SetPhase(phResult);

    _resultByteCount = 7;
    _resultBytes[0] = _status[0];
    _resultBytes[1] = _status[1];
    _resultBytes[2] = _status[2];
    _resultBytes[3] = chrn._cylinder;
    _resultBytes[4] = chrn._head;
    _resultBytes[5] = chrn._record;
    _resultBytes[6] = chrn._num;
}

void FDC::CmdFormatTrack()
{
    SetDataDirection(fdcDataIn);
}

void FDC::CmdScanLow()
{
    SetDataDirection(fdcDataIn);
}

void FDC::CmdScanLowOrEqual()
{
    SetDataDirection(fdcDataIn);
}

void FDC::CmdScanHighOrEqual()
{
    SetDataDirection(fdcDataIn);
}

void FDC::CmdRecalibrate()
{
    SetDataDirection(fdcDataOut);

    SelectDrive(_commandBytes[1]);

    // Recalibrate is pretty much the same as Seek to track 0.
    _commandBytes[2] = 0;
    CmdSeek();
}

void FDC::CmdSenseInterruptStatus()
{
    SetDataDirection(fdcDataOut);

    _resultByteCount = 2;

    // Disc ready?
    if (!_motor || !CurrentDrive().IsReady())
    {
        _status[0] |= st0NotReady;
    }
    else
    {
        _status[0] &= (~st0NotReady);
    }

    if (_seekCompleted[0])
    {
        _seekCompleted[0] = false;
        _statusChanged[0] = false;

        _status[0] |= st0SeekEnd | st0UnitSelect0;

        _resultBytes[0] = _status[0];
        _resultBytes[1] = _drives[0].GetTrack();
    }
    else if (_seekCompleted[1])
    {
        _seekCompleted[1] = false;
        _statusChanged[1] = false;

        _status[0] |= st0SeekEnd | st0UnitSelect1;

        _resultBytes[0] = _status[0];
        _resultBytes[1] = _drives[1].GetTrack();
    }
    else if (_statusChanged[0])
    {
        _statusChanged[0] = false;

        _status[0] = st0AbnormalReadyTerm | st0UnitSelect0;
        if (!_motor || !_drives[0].IsReady())
        {
            _status[0] |= st0NotReady;
        }

        _resultBytes[0] = _status[0];
        _resultBytes[1] = _drives[0].GetTrack();
    }
    else if (_statusChanged[1])
    {
        _statusChanged[1] = false;

        _status[0] = st0AbnormalReadyTerm | st0UnitSelect1;
        if (!_motor || !_drives[1].IsReady())
        {
            _status[0] |= st0NotReady;
        }

        _resultBytes[0] = _status[0];
        _resultBytes[1] = _drives[1].GetTrack();
    }
    else
    {
        _resultBytes[0] = st0InvalidCommand;
        _resultByteCount = 1;
    }

    SetPhase(phResult);
}

void FDC::CmdSpecify()
{
    SetDataDirection(fdcDataOut);

    // Do we need to save SRT, HUT, HLT, ND
    _stepReadTime = (_commandBytes[1] & 0xF0) >> 4;
    _headUnloadTime = (_commandBytes[1] & 0x0F);
    _headLoadTime = (_commandBytes[2] & 0xFE) >> 1;
    _nonDmaMode = (_commandBytes[2] & 0x01);

    SetPhase(phCommand);
}

void FDC::CmdSeek()
{
    SetDataDirection(fdcDataOut);

    SelectDrive(_commandBytes[1]);

    _status[0] &= ~st0AbnormalReadyTerm;
    if (!_motor || !CurrentDrive().IsReady())
    {
        // Abnormal Termination (FDD not ready)
        _status[0] |= st0AbnormalReadyTerm;
    }
    else if (!CurrentDrive().Seek(_commandBytes[2]))
    {
        // Abnormal Termination (Command started but didn't finish)
        _status[0] |= st0AbnormalTerm;
    }
    else
    {
        // Normal Termination
        _status[0] |= st0NormalTerm;
    }

    if ((_currentDrive & 0x01) == 0)
    {
        _seekCompleted[0] = true;
    }
    else
    {
        _seekCompleted[1] = true;
    }

    SetPhase(phCommand);
}

void FDC::CmdSenseDriveStatus()
{
    SetDataDirection(fdcDataOut);

    _resultBytes[0] = _status[3];
    _resultByteCount = 1;

    SetPhase(phResult);
}

StreamWriter& operator<<(StreamWriter& s, const FDC& fdc)
{
    s << fdc._drives;

    s << fdc._readTimeout;

    s << fdc._mainStatus;
    s << fdc._data;
    s << fdc._dataDirection;
    s << fdc._motor;
    s << fdc._currentDrive;
    s << fdc._currentHead;
    s << fdc._status;

    s << fdc._seekCompleted;
    s << fdc._statusChanged;

    s << fdc._phase;
    s << fdc._commandBytes;
    s << fdc._commandByteCount;
    s << fdc._execBytes;
    s << fdc._execByteCount;
    s << fdc._execIndex;
    s << fdc._resultBytes;
    s << fdc._resultByteCount;
    s << fdc._resultIndex;

    s << fdc._stepReadTime;
    s << fdc._headLoadTime;
    s << fdc._headUnloadTime;
    s << fdc._nonDmaMode;
    s << fdc._readBuffer;
    s << fdc._readBufferIndex;

    return s;
}

StreamReader& operator>>(StreamReader& s, FDC& fdc)
{
    s >> fdc._drives;

    s >> fdc._readTimeout;

    s >> fdc._mainStatus;
    s >> fdc._data;
    s >> fdc._dataDirection;
    s >> fdc._motor;
    s >> fdc._currentDrive;
    s >> fdc._currentHead;
    s >> fdc._status;

    s >> fdc._seekCompleted;
    s >> fdc._statusChanged;

    s >> fdc._phase;
    s >> fdc._commandBytes;
    s >> fdc._commandByteCount;
    s >> fdc._execBytes;
    s >> fdc._execByteCount;
    s >> fdc._execIndex;
    s >> fdc._resultBytes;
    s >> fdc._resultByteCount;
    s >> fdc._resultIndex;

    s >> fdc._stepReadTime;
    s >> fdc._headLoadTime;
    s >> fdc._headUnloadTime;
    s >> fdc._nonDmaMode;
    s >> fdc._readBuffer;
    s >> fdc._readBufferIndex;

    return s;
}

StreamWriter& operator<<(StreamWriter& s, const FDC::Phase& phase)
{
    s << (int) phase;

    return s;
}

StreamReader& operator>>(StreamReader& s, FDC::Phase& phase)
{
    s >> (int&) phase;

    return s;
}
