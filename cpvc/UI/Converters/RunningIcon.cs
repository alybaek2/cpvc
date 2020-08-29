using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CPvC.UI.Converters
{
    public class RunningIcon : IValueConverter
    {
        static private BitmapImage _pausedImage = new BitmapImage(new Uri("../Resources/pause16.png", UriKind.Relative));
        static private BitmapImage _runningImage = new BitmapImage(new Uri("../Resources/running16.png", UriKind.Relative));
        static private BitmapImage _reverseImage = new BitmapImage(new Uri("../Resources/reverse16.png", UriKind.Relative));

        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value == null || !(value is RunningState))
            {
                return null;
            }

            switch ((RunningState)value)
            {
                case RunningState.Reverse:
                    return _reverseImage;
                case RunningState.Paused:
                    return _pausedImage;
                case RunningState.Running:
                    return _runningImage;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
