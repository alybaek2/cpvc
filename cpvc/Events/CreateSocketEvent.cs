using System;

namespace CPvC
{
    public class CreateSocketEventArgs : EventArgs
    {
        public CreateSocketEventArgs()
        {
        }

        public ISocket CreatedSocket { get; set; }
    }

    public delegate void CreateSocketEventHandler(object sender, CreateSocketEventArgs e);
}
