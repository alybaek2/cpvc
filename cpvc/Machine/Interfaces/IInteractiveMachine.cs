namespace CPvC
{
    public interface IInteractiveMachine
    {
        void Reset();
        void Key(byte keycode, bool down);
        void LoadDisc(byte drive, byte[] diskBuffer);
        void LoadTape(byte[] tapeBuffer);
    }
}
