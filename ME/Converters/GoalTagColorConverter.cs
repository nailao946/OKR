using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ME.Converters
{
    public class GoalTagColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hexColor && !string.IsNullOrEmpty(hexColor))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hexColor);
                    return new SolidColorBrush(color);
                }
                catch
                {
                    return new SolidColorBrush(Color.FromRgb(0, 122, 255));
                }
            }
            return new SolidColorBrush(Color.FromRgb(0, 122, 255));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color.ToString();
            }
            return "#007AFF";
        }
    }
}
