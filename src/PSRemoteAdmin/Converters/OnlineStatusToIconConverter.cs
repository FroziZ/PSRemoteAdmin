using System.Globalization;
using System.Windows.Data;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Converters;

[ValueConversion(typeof(OnlineStatus), typeof(string))]
public class OnlineStatusToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (OnlineStatus)value switch
        {
            OnlineStatus.Online   => "●",
            OnlineStatus.Offline  => "●",
            OnlineStatus.Checking => "◌",
            _                     => "○"
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
