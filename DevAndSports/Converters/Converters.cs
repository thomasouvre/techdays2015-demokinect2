using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DevAndSports.Converters
{
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(true) ? parameter : Binding.DoNothing;
        }
    }

    public class BooleanConverter<T> : DependencyObject, IValueConverter
    {
        public T TrueValue
        {
            get { return (T)GetValue(TrueValueProperty); }
            set { SetValue(TrueValueProperty, value); }
        }

        public static readonly DependencyProperty TrueValueProperty =
            DependencyProperty.Register("TrueValue", typeof(T), typeof(BooleanConverter<T>), new PropertyMetadata(null));

        public T FalseValue
        {
            get { return (T)GetValue(FalseValueProperty); }
            set { SetValue(FalseValueProperty, value); }
        }

        public static readonly DependencyProperty FalseValueProperty =
            DependencyProperty.Register("FalseValue", typeof(T), typeof(BooleanConverter<T>), new PropertyMetadata(null));

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? TrueValue : FalseValue;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T)value, TrueValue);
        }
    }

    public class BooleanToColorBrushConverter : BooleanConverter<Brush>
    {
    }

    public class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
    }

    public class BooleanToTextWrappingConverter : BooleanConverter<TextWrapping>
    {
    }

    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool?)
            {
                var val = value as bool?;
                return val.HasValue ? val.Value : false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
                return new bool?((bool)value);
            return new bool?(false);
        }
    }
}
