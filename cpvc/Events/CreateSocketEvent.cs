using System.ComponentModel;

namespace CPvC
{
    public class CreateSocketEventArgs : HandledEventArgs
    {
        public CreateSocketEventArgs()
        {
        }

        public ISocket CreatedSocket { get; set; }
    }

    public delegate void CreateSocketEventHandler(object sender, CreateSocketEventArgs e);
}
