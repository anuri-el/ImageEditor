using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ImageEditor.Converters
{
    public class SelBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool selected = (bool)value;
            return selected ? Brushes.Yellow : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
