using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class CoreEventArgs : EventArgs
    {
        public CoreEventArgs(CoreRequest request, CoreAction action)
        {
            Request = request;
            Action = action;
        }

        public CoreRequest Request { get; }
        public CoreAction Action { get; }
    }

    public delegate void CoreEventHandler(object sender, CoreEventArgs e);
}
