#pragma once

#include "..\cpvc-core\Core.h"

using namespace System;

namespace CPvC {

    public interface class ICoreCLR : public IDisposable
    {
        void LoadLowerROM(array<byte>^ lowerRom);
        void LoadUpperROM(byte slotIndex, array<byte>^ rom);
        byte RunUntil(UInt64 stopTicks, byte stopReason);
        void Reset();
        void SetScreen(IntPtr pBuffer, UInt16 pitch, UInt16 height, UInt16 width);
        bool KeyPress(byte keycode, bool down);
        qword Ticks();
        void LoadTape(array<byte>^ tapeBuffer);
        void LoadDisc(byte drive, array<byte>^ discBuffer);
        int GetAudioBuffers(int samples, array<byte>^ channelA, array<byte>^ channelB, array<byte>^ channelC);
        void AdvancePlayback(int samples);
        void AudioSampleFrequency(dword frequency);
        array<byte>^ GetState();
        void LoadState(array<byte>^ state);
    };
}
