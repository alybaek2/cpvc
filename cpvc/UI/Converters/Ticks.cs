using System;
using System.Globalization;
using System.Windows.Data;

namespace CPvC.UI.Converters
{
    public class Ticks : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            // 2.5 "100-nanosecond" units in a single tick of a 4 MHz clock...
            UInt64 ticks = (UInt64)value;

            return new TimeSpan((long)(5 * ticks / 2));
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
