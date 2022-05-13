using System;
using System.ComponentModel;

namespace CPvC
{
    public interface IMachine : INotifyPropertyChanged
    {
        //Core Core { get; }

        void AdvancePlayback(int samples);
        int ReadAudio(byte[] buffer, int offset, int samplesRequested);

        UInt64 Ticks { get; }
        RunningState ActualRunningState { get; }
        byte Volume { get; set; }

        string Status { get; set; }

        string Name { get; set; }

        void Close();
        bool CanClose { get; }

        void PushRequest(CoreRequest request);

        byte[] GetState();

        //void Start();
        //void Stop();

        MachineAuditorDelegate Auditors { get; set; }
    }
}
