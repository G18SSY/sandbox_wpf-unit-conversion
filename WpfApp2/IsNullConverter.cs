﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfApp2
{
    public class IsNullConverter : IValueConverter
    {
        public bool NullValue { get; set; } = true;

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? NullValue : !NullValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}