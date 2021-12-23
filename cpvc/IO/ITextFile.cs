using System;

namespace CPvC
{
    public interface ITextFile : IDisposable
    {
        void WriteLine(string line);
        string ReadLine();
    }
}
