using System.ComponentModel;

namespace CPvC
{
    public class ConfirmCloseEventArgs : HandledEventArgs
    {
        public ConfirmCloseEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }

        public bool Result { get; set; }
    }

    public delegate void ConfirmCloseEventHandler(object sender, ConfirmCloseEventArgs e);
}
