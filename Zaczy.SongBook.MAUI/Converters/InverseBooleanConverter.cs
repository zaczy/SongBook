using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace Zaczy.SongBook.MAUI.Converters
{
#pragma warning disable CS8767
    /// <summary>
    /// Value converter that returns the logical negation of a boolean value.
    /// Use in XAML as a StaticResource: &lt;local:InverseBooleanConverter x:Key="InverseBooleanConverter" /&gt;
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
                return true; // Treat null as "false" -> inverse = true

            if (value is bool b)
                return !b;

            return true;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
                return false; // Treat null as "false" -> inverse = true

            if (value is bool b)
                return !b;

            // Fallback: cannot convert back reliably
            return false;
        }
    }

#pragma warning restore CS8767

}