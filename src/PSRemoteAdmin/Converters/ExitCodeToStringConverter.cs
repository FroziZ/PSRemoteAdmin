using System.Globalization;
using System.Windows.Data;

namespace PSRemoteAdmin.Converters;

[ValueConversion(typeof(int?), typeof(string))]
public class ExitCodeToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        => value is int code ? code.ToString() : "N/A";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
