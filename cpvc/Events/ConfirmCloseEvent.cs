using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ConfirmCloseEventArgs : EventArgs
    {
        public ConfirmCloseEventArgs()
        {
        }

        public string Message { get; set; }

        public bool Result { get; set; }
    }

    public delegate void ConfirmCloseEventHandler(object sender, ConfirmCloseEventArgs e);
}
