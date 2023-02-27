using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CPvC.UI
{
    public class Extensions
    {
        public static readonly DependencyProperty CenterProperty =
          DependencyProperty.RegisterAttached(
            "Center",
            typeof(System.Windows.Point),
            typeof(Extensions),
            new FrameworkPropertyMetadata(new System.Windows.Point(0, 0), OnCenterChanged));

        private static void OnCenterChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is System.Windows.Shapes.Ellipse ellipse)
            {
                System.Windows.Point center = (System.Windows.Point)args.NewValue;
                ellipse.Margin = new Thickness(center.X - ellipse.Width / 2, center.Y - ellipse.Height / 2, 0, 0);
            }
        }

        public static System.Windows.Point GetCenter(DependencyObject target)
        {
            return (System.Windows.Point)target.GetValue(CenterProperty);
        }

        public static void SetCenter(DependencyObject target, System.Windows.Point value)
        {
            target.SetValue(CenterProperty, value);
        }

        //
        public static readonly DependencyProperty CenterYProperty =
          DependencyProperty.RegisterAttached(
            "CenterY",
            typeof(double),
            typeof(Extensions),
            new FrameworkPropertyMetadata(0.0, OnCenterYChanged));

        private static void OnCenterYChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if (obj is FrameworkElement element)
            {
                //System.Windows.Point center = (double)args.NewValue;
                element.Margin = new Thickness(element.Margin.Left, ((double)args.NewValue) - element.ActualHeight / 2, element.Margin.Right, element.Margin.Bottom);
                //element.Margin = new Thickness(element.Margin.Left, ((double)args.NewValue), element.Margin.Right, element.Margin.Bottom);
                //element.SetValue(Canvas.LeftProperty, newPoint.X - (element.ActualWidth / 2));
                //element.SetValue(Canvas.TopProperty, newPoint.Y - (element.ActualHeight / 2));
                //ellipse.Margin = new Thickness(center.X - ellipse.Width / 2, center.Y - ellipse.Height / 2, 0, 0);
            }
        }

        public static System.Windows.Point GetCenterY(DependencyObject target)
        {
            return (System.Windows.Point)target.GetValue(CenterYProperty);
        }

        public static void SetCenterY(DependencyObject target, double value)
        {
            target.SetValue(CenterYProperty, value);
        }


    }
}
