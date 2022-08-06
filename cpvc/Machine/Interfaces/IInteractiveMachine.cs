namespace CPvC
{
    public interface IInteractiveMachine : IMachine
    {
        MachineRequest Reset();
        MachineRequest Key(byte keycode, bool down);
        MachineRequest LoadDisc(byte drive, byte[] diskBuffer);
        MachineRequest LoadTape(byte[] tapeBuffer);
    }
}
