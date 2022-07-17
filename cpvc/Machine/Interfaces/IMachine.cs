using System;
using System.ComponentModel;

namespace CPvC
{
    public interface IMachine : INotifyPropertyChanged
    {
        void AdvancePlayback(int samples);
        int ReadAudio(byte[] buffer, int offset, int samplesRequested);

        UInt64 Ticks { get; }
        RunningState ActualRunningState { get; }

        string Status { get; set; }

        string Name { get; set; }

        void Close();
        bool CanClose { get; }

        void PushRequest(CoreRequest request);

        byte[] GetState();

        IDisposable AutoPause();

        event MachineEventHandler Event;
    }
}
