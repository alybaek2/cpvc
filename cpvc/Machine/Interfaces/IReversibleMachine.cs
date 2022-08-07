namespace CPvC
{
    public interface IReversibleMachine
    {
        MachineRequest Reverse();

        void ToggleReversibilityEnabled();
    }
}
