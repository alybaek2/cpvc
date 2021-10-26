using System;
using System.ComponentModel;

namespace CPvC
{
    public interface IMachine : INotifyPropertyChanged
    {
        Core Core { get; set; }

        void AdvancePlayback(int samples);
        int ReadAudio(byte[] buffer, int offset, int samplesRequested);

        UInt64 Ticks { get; }
        RunningState RunningState { get; }
        byte Volume { get; set; }

        string Status { get; set; }

        string Name { get; set; }
        IDisposable AutoPause();

        void Close();

        MachineAuditorDelegate Auditors { get; set; }
    }
}
