using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPvC.Test
{
    public class MockBinaryFile : Mock<IBinaryFile>
    {
        public List<byte> _outputBytes;
        public List<byte> _inputBytes;
        public long _readPos;

        public MockBinaryFile() : base(MockBehavior.Strict)
        {
            Setup();
        }

        public MockBinaryFile(List<byte> inputBytes) : base(MockBehavior.Strict)
        {
            Setup();

            _inputBytes = new List<byte>(inputBytes);
        }

        private void Setup()
        {
            _readPos = 0;
            _outputBytes = new List<byte>();
            _inputBytes = new List<byte>();
            Setup(s => s.WriteByte(It.IsAny<byte>())).Callback<byte>(b => _outputBytes.Add(b));
            Setup(s => s.Write(It.IsAny<byte[]>())).Callback<byte[]>(b => _outputBytes.AddRange(b));
            Setup(s => s.Seek(It.IsAny<long>())).Callback<long>(offset => _readPos = offset);
            Setup(s => s.ReadByte()).Returns(() => { return _inputBytes[(int)_readPos++]; });
            Setup(s => s.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>())).Returns((byte[] bytes, int count) => {
                List<byte> b = _inputBytes.GetRange((int)_readPos, count);
                b.CopyTo(bytes);
                _readPos += count;
                return count;
            });
            SetupGet(s => s.Length).Returns(() => _inputBytes.Count);
            SetupGet(s => s.Position).Returns(() => _readPos);
            SetupSet(s => s.Position = It.IsAny<long>()).Callback<long>(offset => _readPos = offset);
            Setup(s => s.Close());
        }
    }
}
