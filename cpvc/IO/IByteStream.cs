namespace CPvC
{
    public interface IByteStream
    {
        void Write(byte b);
        void Write(byte[] b);
        byte ReadByte();
        int ReadBytes(byte[] array, int count);

        long Length { get; }
        long Position { get; set; }
    }
}
