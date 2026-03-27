namespace PSRemoteAdmin.Core.Models;

public class ExecutionResult
{
    public required string MachineName { get; init; }
    public string Output { get; init; } = string.Empty;
    public string ErrorOutput { get; init; } = string.Empty;
    public bool HadErrors { get; init; }
    /// <summary>
    /// null = transport failure (WinRM unreachable before execution began).
    /// 0 = success. Non-zero = script-level failure.
    /// </summary>
    public int? ExitCode { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public TimeSpan Duration { get; init; }
}
