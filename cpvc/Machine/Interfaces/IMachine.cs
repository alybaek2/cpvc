using System;
using System.ComponentModel;

namespace CPvC
{
    public interface IMachine : INotifyPropertyChanged
    {
        void AdvancePlayback(int samples);
        int ReadAudio(byte[] buffer, int offset, int samplesRequested);

        UInt64 Ticks { get; }
        RunningState RunningState { get; }

        string Status { get; set; }

        string Name { get; set; }

        void Close();
        bool CanClose { get; }

        void PushRequest(MachineRequest request);

        byte[] GetState();

        IDisposable Lock();

        event MachineEventHandler Event;
    }
}
