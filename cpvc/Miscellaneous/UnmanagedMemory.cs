using System;
using System.Runtime.InteropServices;

namespace CPvC
{
    /// <summary>
    /// Simple wrapper for a memory buffer allocated with AllocHGlobal.
    /// </summary>
    public class UnmanagedMemory : IDisposable
    {
        private IntPtr _memory;
        private readonly int _size;

        /// <summary>
        /// Creates an instance of UnmanagedMemory.
        /// </summary>
        /// <param name="size">The number of bytes to allocate. Will be deallocated once Dispose or the finalizer is called.</param>
        /// <param name="initialValue">The initial value to set all bytes in the buffer to.</param>
        public UnmanagedMemory(int size, byte initialValue)
        {
            _size = size;
            _memory = Marshal.AllocHGlobal(size);

            Clear(initialValue);
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlFillMemory", SetLastError = false)]
        static extern void FillMemory(IntPtr destination, uint length, byte fill);

        /// <summary>
        /// Sets all bytes in the buffer to a given value.
        /// </summary>
        /// <param name="b">The value to set all bytes in the buffer to.</param>
        public void Clear(byte b)
        {
            FillMemory(_memory, (uint)_size, b);
        }

        ~UnmanagedMemory()
        {
            Dispose(false);
        }

        public static implicit operator IntPtr(UnmanagedMemory um)
        {
            return um._memory;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_memory != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_memory);
                _memory = IntPtr.Zero;
            }

            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
        }
    }
}
