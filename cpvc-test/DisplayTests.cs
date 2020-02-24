using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CPvC.Test.TestHelpers;

namespace CPvC.Test
{
    public class DisplayTests
    {
        [Test]
        public void CopyFromNullBuffer()
        {
            // Setup
            Display display = new Display();
            display.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => display.CopyFromBuffer());
        }

        [Test]
        public void DisposeTwice()
        {
            // Setup
            Display display = new Display();
            display.Dispose();

            // Act and Verify
            Assert.DoesNotThrow(() => display.Dispose());
        }

        /// <summary>
        /// Tests that the core doesn't throw an exception when there are no PropertyChanged
        /// handlers registered and a property is changed.
        /// </summary>
        [Test]
        public void NoPropertyChangedHandlers()
        {
            // Setup
            Display display = new Display();

            // Act and Verify - note that EnableGreyscale will trigger a change on the "Bitmap" property.
            Assert.DoesNotThrow(() => display.EnableGreyscale(true));
        }
    }
}
