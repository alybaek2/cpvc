namespace CPvC
{
    public interface IConnection
    {
        bool SendMessage(byte[] msg);
        void Close();

        event NewMessageDelegate OnNewMessage;
        CloseConnectionDelegate OnCloseConnection { get; set; }

        bool IsConnected { get; }
    }
}
