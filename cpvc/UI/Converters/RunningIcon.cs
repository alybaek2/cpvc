using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CPvC.UI.Converters
{
    public class RunningIcon : IValueConverter
    {
        static private BitmapImage _pausedImage = new BitmapImage(new Uri("../Resources/pause16.png", UriKind.Relative));
        static private BitmapImage _runningImage = new BitmapImage(new Uri("../Resources/resume16.png", UriKind.Relative));

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? _runningImage : _pausedImage;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
