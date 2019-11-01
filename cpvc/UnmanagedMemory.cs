using System;
using System.Runtime.InteropServices;

namespace CPvC
{
    /// <summary>
    /// Simple wrapper for memory allocated with AllocHGlobal.
    /// </summary>
    public class UnmanagedMemory : IDisposable
    {
        private IntPtr _memory;
        private int _size;

        /// <summary>
        /// Creates an instance of UnmanagedMemory.
        /// </summary>
        /// <param name="size">The number of bytes to allocate. Will be deallocated once Dispose or the finalizer is called.</param>
        public UnmanagedMemory(int size)
        {
            _size = size;
            _memory = Marshal.AllocHGlobal(size);

            Clear();
        }

        /// <summary>
        /// Sets all bytes in the unmanager memory to zero.
        /// </summary>
        public void Clear()
        {
            // Zero out the buffer. This isn't the most efficient way of doing this, but there's not
            // currently a pressing need for good performance here as this is called sparingly.
            for (int i = 0; i < _size; i++)
            {
                Marshal.WriteByte(_memory, i, 0);
            }
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
