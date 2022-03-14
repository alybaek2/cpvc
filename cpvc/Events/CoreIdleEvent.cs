using System.ComponentModel;

namespace CPvC
{
    public class CoreIdleEventArgs : HandledEventArgs
    {
        public CoreIdleEventArgs()
        {
        }

        public CoreRequest Request { get; set; }
    }

    public delegate void CoreIdleEventHandler(object sender, CoreIdleEventArgs e);
}
