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
        private long _readPos;

        public List<byte> Content { get; set; }

        public MockBinaryFile() : base(MockBehavior.Strict)
        {
            Setup();
        }

        public MockBinaryFile(List<byte> inputBytes) : base(MockBehavior.Strict)
        {
            Setup();

            Content = new List<byte>(inputBytes);
        }

        private void Setup()
        {
            _readPos = 0;
            Content = new List<byte>();
            Setup(s => s.WriteByte(It.IsAny<byte>())).Callback<byte>(b => {
                Content.Add(b);
                _readPos = Content.Count;
            });
            Setup(s => s.Write(It.IsAny<byte[]>())).Callback<byte[]>(b => {
                Content.AddRange(b);
                _readPos = Content.Count;
            });
            Setup(s => s.ReadByte()).Returns(() => { return Content[(int)_readPos++]; });
            Setup(s => s.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>())).Returns((byte[] bytes, int count) => {
                List<byte> b = Content.GetRange((int)_readPos, count);
                b.CopyTo(bytes);
                _readPos += count;
                return count;
            });
            SetupGet(s => s.Length).Returns(() => Content.Count);
            SetupGet(s => s.Position).Returns(() => _readPos);
            SetupSet(s => s.Position = It.IsAny<long>()).Callback<long>(offset => _readPos = offset);
            Setup(s => s.Close());
        }
    }
}
