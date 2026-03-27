using System.Globalization;
using System.Windows.Data;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Converters;

[ValueConversion(typeof(CommandMode), typeof(bool))]
public class CommandModeToReadOnlyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (CommandMode)value == CommandMode.File;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
