using CommunityToolkit.Mvvm.ComponentModel;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.ViewModels;

public partial class MachineTargetViewModel : ObservableObject
{
    public MachineTarget Target { get; }
    public string Name => Target.Name;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusColor))]
    [NotifyPropertyChangedFor(nameof(StatusIcon))]
    private OnlineStatus _status = OnlineStatus.Unknown;

    public string StatusIcon => Status switch
    {
        OnlineStatus.Online   => "●",
        OnlineStatus.Offline  => "●",
        OnlineStatus.Checking => "◌",
        _                     => "○"
    };

    // Returns a hex string so XAML can use it via converter or binding
    public string StatusColor => Status switch
    {
        OnlineStatus.Online   => "#22C55E",
        OnlineStatus.Offline  => "#EF4444",
        OnlineStatus.Checking => "#F59E0B",
        _                     => "#64748B"
    };

    public MachineTargetViewModel(MachineTarget target)
    {
        Target = target;
        _status = target.Status;
    }
}
