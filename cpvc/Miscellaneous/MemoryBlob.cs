namespace CPvC
{
    public class MemoryBlob : IBlob
    {
        private byte[] _bytes;

        private MemoryBlob(byte[] bytes)
        {
            _bytes = bytes;
        }

        static public MemoryBlob Create(byte[] bytes)
        {
            return new MemoryBlob(bytes);
        }

        static public MemoryBlob Create(IBlob blob)
        {
            return blob == null ? null : Create(blob.GetBytes());
        }

        public byte[] GetBytes()
        {
            return _bytes;
        }
    }
}
