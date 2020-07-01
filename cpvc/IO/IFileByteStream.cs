using System;

namespace CPvC
{
    public interface IFileByteStream : IByteStream, IDisposable
    {
        void Close();
    }
}
