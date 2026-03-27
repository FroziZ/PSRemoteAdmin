using CommunityToolkit.Mvvm.ComponentModel;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.ViewModels;

public partial class ExecutionResultViewModel : ObservableObject
{
    public ExecutionResult Result { get; }

    public string MachineName => Result.MachineName;
    public string Output => Result.Output;
    public string ErrorOutput => Result.ErrorOutput;
    public bool HadErrors => Result.HadErrors;
    public int? ExitCode => Result.ExitCode;
    public string ExitCodeDisplay => Result.ExitCode.HasValue ? Result.ExitCode.Value.ToString() : "N/A";
    public string FormattedTimestamp => Result.Timestamp.ToString("HH:mm:ss");
    public string FormattedDuration => $"{Result.Duration.TotalSeconds:F1}s";
    public bool IsSuccess => !Result.HadErrors && Result.ExitCode.HasValue && Result.ExitCode.Value == 0;

    public string StatusIcon => IsSuccess ? "✅" : "❌";
    public string Summary => IsSuccess
        ? $"exit:{ExitCodeDisplay}  {FormattedDuration}  [{FormattedTimestamp}]"
        : $"exit:{ExitCodeDisplay}  errors  {FormattedDuration}  [{FormattedTimestamp}]";

    [ObservableProperty] private bool _isExpanded;

    public ExecutionResultViewModel(ExecutionResult result)
    {
        Result = result;
    }
}
