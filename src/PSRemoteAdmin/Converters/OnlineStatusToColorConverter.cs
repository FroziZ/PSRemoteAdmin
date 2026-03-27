using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Converters;

[ValueConversion(typeof(OnlineStatus), typeof(Brush))]
public class OnlineStatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (OnlineStatus)value switch
        {
            OnlineStatus.Online   => new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E)),
            OnlineStatus.Offline  => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            OnlineStatus.Checking => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
            _                     => new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B))
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
