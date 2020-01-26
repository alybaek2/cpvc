using NUnit.Framework;

namespace CPvC.Test
{
    public class UnmanagedMemoryTests
    {
        [Test]
        public void DisposeTwice()
        {
            // Setup
            UnmanagedMemory um = new UnmanagedMemory(1, 0);

            // Act
            um.Dispose();

            // Verify
            Assert.DoesNotThrow(() => um.Dispose());
        }
    }
}
