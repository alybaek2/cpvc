using System;
using System.Globalization;
using System.Windows.Data;

namespace CPvC.UI.Converters
{
    public class IsNotNull : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return (value != null);
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
