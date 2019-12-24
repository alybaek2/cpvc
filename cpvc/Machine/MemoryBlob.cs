using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC
{
    public class MemoryBlob : IBlob
    {
        private byte[] _bytes;

        public MemoryBlob(byte[] bytes)
        {
            _bytes = bytes;
        }

        public byte[] GetBytes()
        {
            return _bytes;
        }
    }
}
