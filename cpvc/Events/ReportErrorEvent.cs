using System;

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
