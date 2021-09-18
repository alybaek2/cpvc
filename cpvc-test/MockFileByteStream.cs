using Moq;
using System;
using System.Collections.Generic;

namespace CPvC.Test
{
    public class MockFileByteStream : Mock<IFileByteStream>
    {
        private long _readPos;

        public List<byte> Content { get; set; }

        public MockFileByteStream() : base(MockBehavior.Strict)
        {
            Setup();
        }

        public MockFileByteStream(List<byte> inputBytes) : base(MockBehavior.Strict)
        {
            Setup();

            Content = new List<byte>(inputBytes);
        }

        public long Position
        {
            get
            {
                return _readPos;
            }

            set
            {
                _readPos = value;
            }
        }

        private void Setup()
        {
            _readPos = 0;
            Content = new List<byte>();
            Setup(s => s.Write(It.IsAny<byte>())).Callback<byte>(b =>
            {
                Content.Add(b);
                _readPos = Content.Count;
            });
            Setup(s => s.Write(It.IsAny<byte[]>())).Callback<byte[]>(b =>
            {
                Content.AddRange(b);
                _readPos = Content.Count;
            });
            Setup(s => s.ReadByte()).Returns(() =>
            {
                if (_readPos >= Content.Count)
                {
                    throw new Exception("End of file reached!");
                }

                return Content[(int)_readPos++];
            });
            Setup(s => s.ReadBytes(It.IsAny<byte[]>(), It.IsAny<int>())).Returns((byte[] bytes, int count) =>
            {
                int copied = Math.Min((int)(Content.Count - _readPos), count);
                Content.CopyTo((int)_readPos, bytes, 0, copied);
                _readPos += copied;
                return copied;
            });
            SetupGet(s => s.Length).Returns(() => Content.Count);
            SetupGet(s => s.Position).Returns(() => _readPos);
            SetupSet(s => s.Position = It.IsAny<long>()).Callback<long>(offset => _readPos = offset);
            Setup(s => s.Close());
            Setup(s => s.Dispose());
        }
    }
}
