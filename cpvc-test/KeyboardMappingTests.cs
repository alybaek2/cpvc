using NUnit.Framework;
using System;
using System.Windows.Input;

namespace CPvC.Test
{
    public class KeyboardMappingTests
    {
        [Test]
        public void MapAndGetKey()
        {
            // Setup
            KeyboardMapping mapping = new KeyboardMapping();

            // Act
            mapping.Map(Key.A, CPvC.Keys.A);

            // Verify
            byte? key = mapping.GetKey(Key.A);
            Assert.IsTrue(key.HasValue);
            Assert.AreEqual(CPvC.Keys.A, key.Value);
        }

        [Test]
        public void GetUnmappedKey()
        {
            // Setup
            KeyboardMapping mapping = new KeyboardMapping();

            // Verify
            byte? key = mapping.GetKey(Key.A);
            Assert.IsFalse(key.HasValue);
        }
    }
}
