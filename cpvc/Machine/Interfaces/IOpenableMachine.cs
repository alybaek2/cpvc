namespace CPvC
{
    public interface IOpenableMachine
    {
        void Open();
        void Close();

        bool RequiresOpen { get; }
    }
}
