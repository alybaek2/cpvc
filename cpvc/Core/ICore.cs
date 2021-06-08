using System;
using System.Collections.Generic;

namespace CPvC
{
    public interface ICore : IDisposable
    {
        bool KeyPress(byte keycode, bool down);
        byte RunUntil(UInt64 stopTicks, byte stopReason, List<UInt16> audioSamples);
        void Reset();
        void LoadDisc(byte drive, byte[] discImage);
        void LoadTape(byte[] tapeImage);
        void LoadLowerROM(byte[] lowerRom);
        void LoadUpperROM(byte slotIndex, byte[] upperRom);
        void SetScreen(UInt16 pitch, UInt16 height, UInt16 width);
        void CopyScreen(IntPtr screenBuffer, UInt64 size);
        UInt64 Ticks();
        void AudioSampleFrequency(UInt32 frequency);
        byte[] GetState();
        void LoadState(byte[] state);
        bool CreateSnapshot(int id);
        void DeleteSnapshot(int id);
        bool RevertToSnapshot(int id);
    }
}
