namespace CPvC
{
    public class LazyLoadBlob : IBlob
    {
        private string _hex;
        private byte[] _bytes;
        private bool _compressed;

        public LazyLoadBlob(string hex, bool compressed)
        {
            _hex = hex;
            _bytes = null;
            _compressed = compressed;
        }

        public byte[] GetBytes()
        {
            if (_bytes == null && _hex != null)
            {
                if (_hex == "")
                {
                    _bytes = null;
                }
                else
                {
                    _bytes = Helpers.BytesFromStr(_hex);

                    if (_compressed)
                    {
                        _bytes = Helpers.Uncompress(_bytes);
                    }
                }

                _hex = null;
            }

            return _bytes;
        }
    }
}
