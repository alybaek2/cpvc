using System;
using System.ComponentModel;

namespace CPvC
{
    public interface ICoreMachine : INotifyPropertyChanged
    {
        Core Core { get; set; }

        void AdvancePlayback(int samples);
        int ReadAudio(byte[] buffer, int offset, int samplesRequested);

        UInt64 Ticks { get; }
        RunningState RunningState { get; }
        byte Volume { get; set; }

        string Status { get; set; }

        string Name { get; set; }
        string Filepath { get; }
        IDisposable AutoPause();

        void Close();
        bool CanClose();

        MachineAuditorDelegate Auditors { get; set; }
    }
}
