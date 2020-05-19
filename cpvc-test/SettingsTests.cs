using NUnit.Framework;
using System;

namespace CPvC.Test
{
    public class SettingsTests
    {
        [TestCase(FileTypes.Disc, "abc")]
        [TestCase(FileTypes.Tape, "123")]
        [TestCase(FileTypes.Machine, null)]
        public void SetValidFolder(FileTypes fileType, string expectedFolder)
        {
            // Setup
            Settings settings = new Settings();

            // Act
            settings.SetFolder(fileType, expectedFolder);
            string folder = settings.GetFolder(fileType);

            // Verify
            Assert.AreEqual(expectedFolder, folder);
        }

        [Test]
        public void SetInvalidFolder()
        {
            // Setup
            Settings settings = new Settings();

            // Act and Verify
            Assert.Throws<Exception>(() => settings.SetFolder((FileTypes)99, "test"));
        }

        [Test]
        public void GetInvalidFolder()
        {
            // Setup
            Settings settings = new Settings();

            // Act and Verify
            Assert.Throws<Exception>(() => settings.GetFolder((FileTypes)99));
        }

        [TestCase(null)]
        [TestCase("abc")]
        [TestCase("123")]
        public void SetRecentlyOpened(string recentlyOpened)
        {
            // Setup
            Settings settings = new Settings();

            // Act
            settings.RecentlyOpened = recentlyOpened;

            // Verify
            Assert.AreEqual(recentlyOpened, settings.RecentlyOpened);
        }

        [TestCase(null)]
        [TestCase("abc")]
        [TestCase("123")]
        public void SetRemoteServers(string remoteServers)
        {
            // Setup
            Settings settings = new Settings();

            // Act
            settings.RemoteServers = remoteServers;

            // Verify
            Assert.AreEqual(remoteServers, settings.RemoteServers);
        }
    }
}
