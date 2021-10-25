using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
