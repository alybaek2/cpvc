namespace CPvC
{
    public interface IByteStream
    {
        void Write(byte b);
        void Write(byte[] b);
        byte ReadByte();
        int ReadBytes(byte[] array, int count);

        string ReadLine();

        void SeekToEnd();
        long Length { get; }
        long Position { get; set; }
    }
}
