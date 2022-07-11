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
        void SetScreen(byte[] screen);
        void GetScreen(IntPtr screenBuffer, UInt64 size);
        byte[] GetScreen();
        UInt64 Ticks();
        void AudioSampleFrequency(UInt32 frequency);
        byte[] GetState();
        void LoadState(byte[] state);
        void CreateSnapshot(int id);
        bool DeleteSnapshot(int id);
        bool RevertToSnapshot(int id);
    }
}
