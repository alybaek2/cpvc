namespace CPvC
{
    public interface IPausableMachine : IMachine
    {
        MachineRequest Start();
        MachineRequest Stop();
        bool CanStart { get; }
        bool CanStop { get; }
        MachineRequest ToggleRunning();
    }
}
