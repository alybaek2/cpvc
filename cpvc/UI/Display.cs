using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPvC
{
    /// <summary>
    /// Small class to deal with copying the screen buffer from a Core to a WriteableBitmap object.
    /// </summary>
    public class Display : IDisposable
    {
        /// <summary>
        /// Helper struct encapsulating information on the Amstrad CPC's colour palette.
        /// </summary>
        private struct CPCColour
        {
            public CPCColour(byte r, byte g, byte b, byte intensity)
            {
                _r = r;
                _g = g;
                _b = b;
                _intensity = intensity;
            }

            public Color GetColor()
            {
                return Color.FromRgb(Scale(_r, 2), Scale(_g, 2), Scale(_b, 2));
            }

            public Color GetGreyscaleColor()
            {
                byte i = Scale(_intensity, 26);

                return Color.FromRgb(i, i, i);
            }

            private byte Scale(byte v, byte max)
            {
                return (byte)(255 * ((float) v / max));
            }

            // Note that r, g, and b can be either 0, 1, 2, indicating the intensity of each for the colour.
            private byte _r;
            private byte _g;
            private byte _b;

            // Indicates the intensity of the colour for use with grey/green screen rendering.
            private byte _intensity;
        }

        static private List<CPCColour> _colours = new List<CPCColour>
        {
            new CPCColour(1, 1, 1, 13),   //  0 - White
            new CPCColour(1, 1, 1, 13),   //  1 - White
            new CPCColour(0, 2, 1, 19),   //  2 - Sea Green
            new CPCColour(2, 2, 1, 25),   //  3 - Pastel Yellow
            new CPCColour(0, 0, 1, 1),    //  4 - Blue
            new CPCColour(2, 0, 1, 7),    //  5 - Purple
            new CPCColour(0, 1, 1, 10),   //  6 - Cyan
            new CPCColour(2, 1, 1, 16),   //  7 - Pink
            new CPCColour(2, 0, 1, 7),    //  8 - Purple
            new CPCColour(2, 2, 1, 25),   //  9 - Pastel Yellow
            new CPCColour(2, 2, 0, 24),   // 10 - Bright Yellow
            new CPCColour(2, 2, 2, 26),   // 11 - Bright White
            new CPCColour(2, 0, 0, 6),    // 12 - Bright Red
            new CPCColour(2, 0, 2, 8),    // 13 - Bright Magenta
            new CPCColour(2, 1, 0, 15),   // 14 - Orange
            new CPCColour(2, 1, 2, 17),   // 15 - Pastel Magenta
            new CPCColour(0, 0, 1, 1),    // 16 - Blue
            new CPCColour(0, 2, 1, 19),   // 17 - Sea Green
            new CPCColour(0, 2, 0, 18),   // 18 - Bright Green
            new CPCColour(0, 2, 2, 20),   // 19 - Bright Cyan
            new CPCColour(0, 0, 0, 0),    // 20 - Black
            new CPCColour(0, 0, 2, 2),    // 21 - Bright Blue
            new CPCColour(0, 1, 0, 9),    // 22 - Green
            new CPCColour(0, 1, 2, 11),   // 23 - Sky Blue
            new CPCColour(1, 0, 1, 4),    // 24 - Magenta
            new CPCColour(1, 2, 1, 22),   // 25 - Pastel Green
            new CPCColour(1, 2, 0, 21),   // 26 - Lime
            new CPCColour(1, 2, 2, 23),   // 27 - Pastel Cyan
            new CPCColour(1, 0, 0, 3),    // 28 - Red
            new CPCColour(1, 0, 2, 5),    // 29 - Mauve
            new CPCColour(1, 1, 0, 12),   // 30 - Yellow
            new CPCColour(1, 1, 2, 14)    // 31 - Pastel Blue
        };
        
        static private readonly BitmapPalette _greyPalette = new BitmapPalette(_colours.Select(c => c.GetGreyscaleColor()).ToList());
        static private readonly BitmapPalette _colourPalette = new BitmapPalette(_colours.Select(c => c.GetColor()).ToList());

        private readonly Int32Rect _drawRect;

        public UnmanagedMemory Buffer { get; private set; }

        public const ushort Width = 768;
        public const ushort Height = 288;

        // As Bitmap will use an 8-bit palette, each pixel will require one byte. Thus, Pitch will equal Width.
        public const ushort Pitch = Width;

        public WriteableBitmap Bitmap { get; }

        public Display()
        {
            _drawRect = new Int32Rect(0, 0, Width, Height);

            Buffer = new UnmanagedMemory(Height * Pitch);
            Bitmap = new WriteableBitmap(Width, Height, 0, 0, PixelFormats.Indexed8, _colourPalette);
        }

        /// <summary>
        /// Asynchronously copies the display's internal screen buffer to Bitmap. This method ensures this is done on the same thread in which the Bitmap was created.
        /// </summary>
        public void CopyFromBufferAsync()
        {
            Bitmap.Dispatcher.BeginInvoke(new Action(() =>
            {
                CopyFromBuffer();
            }), null);
        }

        /// <summary>
        /// Copies the display's internal screen buffer to Bitmap. Note this method should only be called on the same thread in which the Bitmap was created.
        /// </summary>
        public void CopyFromBuffer()
        {
            if (Buffer != IntPtr.Zero)
            {
                Bitmap.WritePixels(_drawRect, Buffer, Pitch * Height, Pitch);
            }
        }

        /// <summary>
        /// Populates the display from a bookmark.
        /// </summary>
        /// <param name="bookmark">Bookmark to populate the display from.</param>
        public void GetFromBookmark(Bookmark bookmark)
        {
            // Assume a blank screen if no bookmark provided.
            if (bookmark == null)
            {
                Buffer.Clear();
                return;
            }

            // Otherwise, use the bookmark to create a core, and run it for 2 VSync's in order to populate the screen buffer.
            using (Core core = Core.Create(bookmark.State))
            {
                core.SetScreenBuffer(Buffer);
                core.RunForVSync(2);

                CopyFromBuffer();
            }
        }

        public WriteableBitmap ConvertToGreyscale()
        {
            WriteableBitmap bitmap = new WriteableBitmap(Width, Height, 0, 0, PixelFormats.Indexed8, _greyPalette);

            byte[] pixels = new byte[Bitmap.PixelWidth * Bitmap.PixelHeight];
            Bitmap.CopyPixels(pixels, Pitch, 0);

            bitmap.WritePixels(_drawRect, pixels, Pitch, 0);

            return bitmap;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Buffer != null)
            {
                Buffer.Dispose();
            }
        }
    }
}
