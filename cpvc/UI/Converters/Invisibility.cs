using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CPvC.UI.Converters
{
    public class Invisibility : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (value == null || !(value is bool))
            {
                return Visibility.Visible;
            }

            return ((bool)value) ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
