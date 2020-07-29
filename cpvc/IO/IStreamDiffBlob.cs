namespace CPvC
{
    public interface IStreamDiffBlob : IStreamBlob
    {
        IStreamBlob BaseBlob { get; }
    }
}
