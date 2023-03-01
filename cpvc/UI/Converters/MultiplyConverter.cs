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

            if (GetFactor(parameter, out double factor))
            {
                switch (value)
                {
                    case System.Windows.Point point:
                        return new System.Windows.Point(point.X * factor, point.Y * factor);
                    case Thickness thickness:
                        return new Thickness(thickness.Left * factor, thickness.Top * factor, thickness.Right * factor, thickness.Bottom * factor);
                    case int integer:
                        return factor * integer;
                    case float f:
                        return factor * f;
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

        static private bool GetFactor(object parameter, out double d)
        {
            if (parameter is double)
            {
                d = (double)parameter;
                return true;
            }

            if (parameter is string)
            {
                return double.TryParse((string)parameter, out d);
            }

            d = 1.0;

            return false;
        }
    }
}
