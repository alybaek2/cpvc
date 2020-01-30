using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public interface ICore : IDisposable
    {
        bool KeyPress(byte keycode, bool down);
        byte RunUntil(UInt64 stopTicks, byte stopReason);
        void Reset();
        void LoadDisc(byte drive, byte[] discImage);
        void LoadTape(byte[] tapeImage);
        void LoadLowerROM(byte[] lowerRom);
        void LoadUpperROM(byte slotIndex, byte[] upperRom);
        void AdvancePlayback(int samples);
        int GetAudioBuffers(int samples, byte[] channelA, byte[] channelB, byte[] channelC);
        void SetScreen(IntPtr screenBuffer, UInt16 pitch, UInt16 height, UInt16 width);
        UInt64 Ticks();
        void AudioSampleFrequency(UInt32 frequency);
        byte[] GetState();
        void LoadState(byte[] state);
    }
}
