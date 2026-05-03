using System;
using System.Globalization;
using System.Windows.Data;

namespace ME.Converters
{
    public class QuantitativeProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double current = 0, target = 1;
            if (values.Length >= 2)
            {
                if (values[0] is double c) current = c;
                else if (values[0] is int ci) current = ci;
                if (values[1] is double t) target = t;
                else if (values[1] is int ti) target = ti;
            }
            if (target <= 0) target = 1;
            return Math.Min(current / target * 100, 100);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
