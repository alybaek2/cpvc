﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPvC
{
    /// <summary>
    /// Small class to deal with copying the screen buffer from a Core to a WriteableBitmap object.
    /// </summary>
    public class Display : INotifyPropertyChanged
    {
        /// <summary>
        /// Helper struct encapsulating information on the Amstrad CPC's colour palette.
        /// </summary>
        private struct CPCColour
        {
            public CPCColour(byte hwColourNumber, byte r, byte g, byte b, byte intensity)
            {
                _hwColourNumber = hwColourNumber;
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
                return (byte)(255 * ((float)v / max));
            }

            // Note that r, g, and b can be either 0, 1, 2, indicating the intensity of each for the colour.
            private readonly byte _r;
            private readonly byte _g;
            private readonly byte _b;

            // Indicates the intensity of the colour for use with grey/green screen rendering.
            private readonly byte _intensity;

            // Indicates the hardware palette colour number.
            public readonly byte _hwColourNumber;

            // Colours.
            static public CPCColour White = new CPCColour(0, 1, 1, 1, 13);
            static public CPCColour White2 = new CPCColour(1, 1, 1, 1, 13);
            static public CPCColour SeaGreen = new CPCColour(2, 0, 2, 1, 19);
            static public CPCColour PastelYellow = new CPCColour(3, 2, 2, 1, 25);
            static public CPCColour Blue = new CPCColour(4, 0, 0, 1, 1);
            static public CPCColour Purple = new CPCColour(5, 2, 0, 1, 7);
            static public CPCColour Cyan = new CPCColour(6, 0, 1, 1, 10);
            static public CPCColour Pink = new CPCColour(7, 2, 1, 1, 16);
            static public CPCColour Purple2 = new CPCColour(8, 2, 0, 1, 7);
            static public CPCColour PastelYellow2 = new CPCColour(9, 2, 2, 1, 25);
            static public CPCColour BrightYellow = new CPCColour(10, 2, 2, 0, 24);
            static public CPCColour BrightWhite = new CPCColour(11, 2, 2, 2, 26);
            static public CPCColour BrightRed = new CPCColour(12, 2, 0, 0, 6);
            static public CPCColour BrightMagenta = new CPCColour(13, 2, 0, 2, 8);
            static public CPCColour Orange = new CPCColour(14, 2, 1, 0, 15);
            static public CPCColour PastelMagenta = new CPCColour(15, 2, 1, 2, 17);
            static public CPCColour Blue2 = new CPCColour(16, 0, 0, 1, 1);
            static public CPCColour SeaGreen2 = new CPCColour(17, 0, 2, 1, 19);
            static public CPCColour BrightGreen = new CPCColour(18, 0, 2, 0, 18);
            static public CPCColour BrightCyan = new CPCColour(19, 0, 2, 2, 20);
            static public CPCColour Black = new CPCColour(20, 0, 0, 0, 0);
            static public CPCColour BrightBlue = new CPCColour(21, 0, 0, 2, 2);
            static public CPCColour Green = new CPCColour(22, 0, 1, 0, 9);
            static public CPCColour SkyBlue = new CPCColour(23, 0, 1, 2, 11);
            static public CPCColour Magenta = new CPCColour(24, 1, 0, 1, 4);
            static public CPCColour PastelGreen = new CPCColour(25, 1, 2, 1, 22);
            static public CPCColour Lime = new CPCColour(26, 1, 2, 0, 21);
            static public CPCColour PastelCyan = new CPCColour(27, 1, 2, 2, 23);
            static public CPCColour Red = new CPCColour(28, 1, 0, 0, 3);
            static public CPCColour Mauve = new CPCColour(29, 1, 0, 2, 5);
            static public CPCColour Yellow = new CPCColour(30, 1, 1, 0, 12);
            static public CPCColour PastelBlue = new CPCColour(31, 1, 1, 2, 14);

            // The entire palette.
            static public List<CPCColour> Palette = new List<CPCColour>
            {
                CPCColour.White,         CPCColour.White2,        CPCColour.SeaGreen,      CPCColour.PastelYellow,  CPCColour.Blue,
                CPCColour.Purple,        CPCColour.Cyan,          CPCColour.Pink,          CPCColour.Purple2,       CPCColour.PastelYellow2,
                CPCColour.BrightYellow,  CPCColour.BrightWhite,   CPCColour.BrightRed,     CPCColour.BrightMagenta, CPCColour.Orange,
                CPCColour.PastelMagenta, CPCColour.Blue2,         CPCColour.SeaGreen2,     CPCColour.BrightGreen,   CPCColour.BrightCyan,
                CPCColour.Black,         CPCColour.BrightBlue,    CPCColour.Green,         CPCColour.SkyBlue,       CPCColour.Magenta,
                CPCColour.PastelGreen,   CPCColour.Lime,          CPCColour.PastelCyan,    CPCColour.Red,           CPCColour.Mauve,
                CPCColour.Yellow,        CPCColour.PastelBlue
            };
        }

        static private readonly BitmapPalette _greyPalette = new BitmapPalette(CPCColour.Palette.Select(c => c.GetGreyscaleColor()).ToList());
        static private readonly BitmapPalette _colourPalette = new BitmapPalette(CPCColour.Palette.Select(c => c.GetColor()).ToList());

        private readonly Int32Rect _drawRect;

        public event PropertyChangedEventHandler PropertyChanged;

        public const ushort Width = 768;
        public const ushort Height = 288;

        public Core Core { get; set; }

        // As Bitmap will use an 8-bit palette, each pixel will require one byte. Thus, Pitch will equal Width.
        public const ushort Pitch = Width;

        private WriteableBitmap _bitmap;

        public Display()
        {
            _drawRect = new Int32Rect(0, 0, Width, Height);

            Bitmap = new WriteableBitmap(Width, Height, 0, 0, PixelFormats.Indexed8, _colourPalette);

            CopyScreen();
        }

        public WriteableBitmap Bitmap
        {
            get
            {
                return _bitmap;
            }

            private set
            {
                // If we already have a bitmap, copy its contents over to the new one.
                if (_bitmap != null)
                {
                    value.WritePixels(_drawRect, GetBitmapBytes(), Pitch, 0);
                }

                _bitmap = value;

                OnPropertyChanged();
            }
        }

        public byte[] GetBitmapBytes()
        {
            byte[] pixels = new byte[_bitmap.PixelWidth * _bitmap.PixelHeight];
            _bitmap.CopyPixels(pixels, Pitch, 0);

            return pixels;
        }

        /// <summary>
        /// Enables or disables greyscale for the bitmap.
        /// </summary>
        /// <param name="enabled">Indicates if greyscale should be enabled or disabled.</param>
        public void EnableGreyscale(bool enabled)
        {
            Bitmap = new WriteableBitmap(Width, Height, 0, 0, PixelFormats.Indexed8, enabled ? _greyPalette : _colourPalette);
        }

        /// <summary>
        /// Asynchronously copies the display's internal screen buffer to Bitmap. This method ensures this is done on the same thread in which the Bitmap was created.
        /// </summary>
        public void CopyScreenAsync()
        {
            Bitmap.Dispatcher.BeginInvoke(new Action(() => CopyScreen()), null);
        }

        /// <summary>
        /// Copies the display's internal screen buffer to Bitmap. Note this method should only be called on the same thread in which the Bitmap was created.
        /// </summary>
        public void CopyScreen()
        {
            Bitmap.Lock();

            Core?.CopyScreen(Bitmap.BackBuffer, (UInt64)(Bitmap.BackBufferStride * Bitmap.PixelHeight));
            Bitmap.AddDirtyRect(_drawRect);

            Bitmap.Unlock();
        }

        /// <summary>
        /// Populates the display from a bookmark.
        /// </summary>
        /// <param name="bookmark">Bookmark to populate the display from.</param>
        public void GetFromBookmark(Bookmark bookmark)
        {
            if (bookmark?.Screen == null)
            {
                // Commenting this out so the code coverage doesn't take forever during a build.
                // Need to find a simpler way of doing this!
                /*
                // Assume a blank screen if no bookmark provided.
                Bitmap.Lock();

                int size = Bitmap.BackBufferStride * Bitmap.PixelHeight;
                for (int i = 0; i < size; i++)
                {
                    Marshal.WriteByte(Bitmap.BackBufferStride, i, CPCColour.Black._hwColourNumber);
                }

                Bitmap.Unlock();

                CopyScreen();
                */
            }
            else
            {
                Bitmap.WritePixels(_drawRect, bookmark.Screen.GetBytes(), Pitch, 0);
            }
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
