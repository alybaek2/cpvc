using System;

namespace CPvC
{
    static public class Diagnostics
    {
        static public void Trace(string format, params object[] args)
        {
            System.Diagnostics.Trace.WriteLine(String.Format(format, args));
        }
    }
}
