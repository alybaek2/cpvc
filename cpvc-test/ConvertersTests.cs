using NUnit.Framework;
using System;

namespace CPvC.Test
{
    /// <summary>
    /// Ensures converters return expected values. Note that ConvertBack is not implemented on any converted and always returns null.
    /// </summary>
    public class ConvertersTests
    {
        [TestCase(0)]
        [TestCase(4000000)]
        [TestCase(8000004)]
        public void Ticks(Int64 ticks)
        {
            // Setup
            CPvC.UI.Converters.Ticks conv = new UI.Converters.Ticks();

            // Act
            object value = conv.Convert((UInt64)ticks, null, null, null);
            object original = conv.ConvertBack(value, null, null, null);
        
            // Verify
            Assert.True(value is TimeSpan);
            Assert.AreEqual((TimeSpan)value, new TimeSpan(5 * ticks / 2));
            Assert.IsNull(original);
        }

        [TestCase(false)]
        [TestCase(true)]
        [TestCase(null)]
        public void BooleanInverter(bool? f)
        {
            // Setup
            CPvC.UI.Converters.BooleanInverter conv = new UI.Converters.BooleanInverter();

            // Act
            object value = conv.Convert(f.HasValue ? (object)f.Value : null, null, null, null);
            object original = conv.ConvertBack(value, null, null, null);

            // Verify
            if (f.HasValue)
            {
                Assert.IsTrue(value is bool);
                Assert.AreEqual(!f.Value, (bool)value);
            }
            else
            {
                Assert.IsNull(value);
            }

            Assert.IsNull(original);
        }
    }
}
