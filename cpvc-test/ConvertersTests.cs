using NUnit.Framework;
using System;
using System.Windows.Media.Imaging;

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
            object converted = conv.Convert(f.HasValue ? (object)f.Value : null, null, null, null);
            object original = conv.ConvertBack(converted, null, null, null);

            // Verify
            if (f.HasValue)
            {
                Assert.True(converted is bool);
                Assert.AreEqual(!f.Value, (bool)converted);
            }
            else
            {
                Assert.IsNull(converted);
            }

            Assert.IsNull(original);
        }

        [TestCase(null)]
        [TestCase(1)]
        [TestCase("a")]
        public void IsNotNull(object value)
        {
            // Setup
            CPvC.UI.Converters.IsNotNull conv = new UI.Converters.IsNotNull();

            // Act
            object converted = conv.Convert(value, null, null, null);
            object original = conv.ConvertBack(converted, null, null, null);

            // Verify
            Assert.AreEqual(converted, value != null);
            Assert.IsNull(original);
        }

        [TestCase(null, null)]
        [TestCase(RunningState.Paused, "pause")]
        [TestCase(RunningState.Running, "running")]
        [TestCase(RunningState.Reverse, "reverse")]
        [TestCase(100, null)]
        [TestCase((RunningState)101, null)]
        public void RunningIcon(object value, string expectedPath)
        {
            // Setup
            CPvC.UI.Converters.RunningIcon conv = new UI.Converters.RunningIcon();

            // Act
            object converted = conv.Convert(value, null, null, null);
            object original = conv.ConvertBack(converted, null, null, null);

            // Verify
            if (value == null || !(value is RunningState) || expectedPath == null)
            {
                Assert.IsNull(converted);
            }
            else
            {
                string path = ((BitmapImage)converted).UriSource.OriginalString;
                Assert.IsTrue(path.Contains(expectedPath));
            }

            Assert.IsNull(original);
        }
    }
}
