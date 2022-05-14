namespace CPvC
{
    public interface IInteractiveMachine : IMachine
    {
        CoreRequest Reset();
        CoreRequest Key(byte keycode, bool down);
        CoreRequest LoadDisc(byte drive, byte[] diskBuffer);
        CoreRequest LoadTape(byte[] tapeBuffer);
    }
}
