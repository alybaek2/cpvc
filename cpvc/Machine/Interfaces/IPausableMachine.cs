using System;

namespace CPvC
{
    public interface IPausableMachine : IMachine
    {
        void Start();
        void Stop();
        void RequestStop();
        bool CanStart { get; }
        bool CanStop { get; }
        void ToggleRunning();
        IDisposable AutoPause();
    }
}
