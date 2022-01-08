using System;
using System.Globalization;
using System.Windows.Data;
using UnitsNet;

namespace WpfApp2
{
    public class DoubleToQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (parameter is not Enum unit)
                throw new ArgumentException("Parameter must be the source unit", nameof(parameter));

            double dValue = System.Convert.ToDouble(value);

            return Quantity.From(dValue, unit);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            if (parameter is not Enum unit)
                throw new ArgumentException("Parameter must be the source unit", nameof(parameter));

            IQuantity quantity = (IQuantity)value;

            return quantity.As(unit);
        }
    }
}