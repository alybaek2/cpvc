namespace CPvC
{
    public interface IPausableMachine : ICoreMachine
    {
        void Start();
        void Stop();
        bool CanStart { get; }
        bool CanStop { get; }
        void ToggleRunning();
    }
}
