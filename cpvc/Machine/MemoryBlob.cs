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
