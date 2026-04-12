using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MyWpfApp
{
    /// <summary>
    /// Converter: يرجع True لو النص مش فاضي (للـ UpdateBadge)
    /// </summary>
    public class NotEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s && !string.IsNullOrEmpty(s);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
