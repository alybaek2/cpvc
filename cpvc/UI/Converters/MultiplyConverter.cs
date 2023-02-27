using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace CPvC.UI.Converters
{
    public class MultiplyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (parameter is string factorStr && Int32.TryParse(factorStr, out int factor))
            {
                switch (value)
                {
                    case System.Windows.Point point:
                        return new System.Windows.Point(point.X * factor, point.Y * factor);
                    case Thickness thickness:
                        return new Thickness(thickness.Left * factor, thickness.Top * factor, thickness.Right * factor, thickness.Bottom * factor);
                    case int integer:
                        return factor * integer;
                    case double d:
                        return factor * d;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class ThickConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            if (parameter is string factorStr && Int32.TryParse(factorStr, out int factor))
            {
                switch (value)
                {
                    case System.Windows.Point point:
                        return new System.Windows.Point(point.X * factor, point.Y * factor);
                    case Thickness thickness:
                        return new Thickness(thickness.Left * factor, thickness.Top * factor, thickness.Right * factor, thickness.Bottom * factor);
                    case int integer:
                        return factor * integer;
                    case double d:
                        return factor * d;
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
