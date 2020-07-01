namespace CPvC
{
    public interface IPausableMachine : ICoreMachine
    {
        void Start();
        void Stop();
        void ToggleRunning();
    }
}
