using System;

namespace CPvC
{
    public interface IPausableMachine : IMachine
    {
        void Start();
        void Stop();
        bool CanStart { get; }
        bool CanStop { get; }
        void ToggleRunning();
    }
}
