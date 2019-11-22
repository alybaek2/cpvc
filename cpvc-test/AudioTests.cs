using Moq;
using NUnit.Framework;

namespace CPvC.Test
{
    public class AudioTests
    {
        [Test]
        public void Read()
        {
            Mock<ReadAudioDelegate> readAudio = new Mock<ReadAudioDelegate>();
            readAudio.Setup(x => x(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns(100);

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

    }
}
