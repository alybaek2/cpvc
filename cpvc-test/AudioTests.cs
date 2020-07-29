using Moq;
using NUnit.Framework;

namespace CPvC.Test
{
    public class AudioTests
    {
        [TestCase(0)]
        [TestCase(4)]
        [TestCase(100)]
        public void Play(int samplesWritten)
        {
            Mock<ReadAudioDelegate> readAudio = new Mock<ReadAudioDelegate>();
            readAudio.Setup(x => x(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(samplesWritten);

            using (Audio audio = new Audio(readAudio.Object))
            {
                // Act
                audio.Start();
                System.Threading.Thread.Sleep(400);
                audio.Stop();
            }

            // Verify
            readAudio.Verify(x => x(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.AtLeastOnce());
            readAudio.VerifyNoOtherCalls();
        }

        [TestCase(0, 0)]
        [TestCase(0, 3)]
        [TestCase(0, 4)]
        [TestCase(0, 100)]
        [TestCase(100, 100)]
        public void Read(int samplesWritten, int bytesRequested)
        {
            // Setup
            Mock<ReadAudioDelegate> readAudio = new Mock<ReadAudioDelegate>();
            readAudio.Setup(x => x(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(samplesWritten);

            byte[] buffer = new byte[1000];
            int bytesWritten = 0;
            using (Audio audio = new Audio(readAudio.Object))
            {
                // Act
                bytesWritten = audio.Read(buffer, 0, bytesRequested);
            }

            // Verify
            if (samplesWritten == 0 && bytesRequested >= 4)
            {
                Assert.AreEqual(4, bytesWritten);
            }
            else
            {
                Assert.AreEqual(samplesWritten * 4, bytesWritten);
            }
        }

        [Test]
        public void NullDelegate()
        {
            // Setup
            byte[] buffer = new byte[1000];
            int bytesWritten = 0;
            using (Audio audio = new Audio(null))
            {
                // Act
                bytesWritten = audio.Read(buffer, 0, 100);
            }

            // Verify
            Assert.AreEqual(4, bytesWritten);
            Assert.Zero(buffer[0]);
            Assert.Zero(buffer[1]);
            Assert.Zero(buffer[2]);
            Assert.Zero(buffer[3]);
        }

        /// <summary>
        /// This isn't a real test as such, but rather a way of ensuring code coverage for the Length
        /// and Position properties which NAudio never seems to call, despite requiring them to be
        /// implemented when deriving a class from WaveStream.
        /// </summary>
        [Test]
        public void CheckProperties()
        {
            // Setup
            Mock<ReadAudioDelegate> readAudio = new Mock<ReadAudioDelegate>();
            readAudio.Setup(x => x(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(100);

            // Act
            using (Audio audio = new Audio(readAudio.Object))
            {
                audio.Position = 999;

                // Verify
                Assert.Greater(audio.Length, 0);
                Assert.AreEqual(999, audio.Position);
            }

        }
    }
}
