namespace CPvC
{
    public interface IReversibleMachine
    {
        MachineRequest Reverse();
        MachineRequest ReverseStop();

        void ToggleReversibilityEnabled();
    }
}
