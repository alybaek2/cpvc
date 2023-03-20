using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CPvC.UI.Converters
{
    public class BlobBitmap : IValueConverter
    {
        private readonly ConditionalWeakTable<IBlob, WriteableBitmap> _bitmaps;
        static private Int32Rect _drawRect = new Int32Rect(0, 0, Display.Width, Display.Height);

        public BlobBitmap()
        {
            _bitmaps = new ConditionalWeakTable<IBlob, WriteableBitmap>();
        }

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (!(value is IBlob blob))
            {
                return null;
            }

            if (!_bitmaps.TryGetValue(blob, out WriteableBitmap bitmap))
            {
                bitmap = new WriteableBitmap(Display.Width, Display.Height, 0, 0, PixelFormats.Indexed8, Display.Palette);

                bitmap.Lock();
                bitmap.WritePixels(_drawRect, blob.GetBytes(), Display.Pitch, 0);
                bitmap.AddDirtyRect(_drawRect);
                //bitmap.WritePixels(_drawRect, blob.GetBytes(), )
                bitmap.Unlock();

                _bitmaps.Add(blob, bitmap);

                //UpdateScreen(blob, bitmap);
            }

            return bitmap;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
