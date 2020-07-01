namespace CPvC
{
    public interface IConnection
    {
        bool SendMessage(byte[] msg);
        void Close();

        event NewMessageDelegate OnNewMessage;
        event CloseConnectionDelegate OnCloseConnection;

        bool IsConnected { get; }
    }
}
