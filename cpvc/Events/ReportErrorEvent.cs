using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class ReportErrorEventArgs : EventArgs
    {
        public ReportErrorEventArgs()
        {
        }

        public string Message { get; set; }
    }

    public delegate void ReportErrorEventHandler(object sender, ReportErrorEventArgs e);
}
