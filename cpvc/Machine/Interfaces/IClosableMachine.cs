namespace CPvC
{
    public delegate void OnCloseDelegate();

    public interface IClosableMachine
    {
        void Close();
        bool CanClose();
    }
}
