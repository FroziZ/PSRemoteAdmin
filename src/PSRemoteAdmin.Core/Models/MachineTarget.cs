namespace PSRemoteAdmin.Core.Models;

public class MachineTarget
{
    public required string Name { get; init; }
    public string? DnsHostName { get; init; }
    public required string DistinguishedName { get; init; }
    public OnlineStatus Status { get; set; } = OnlineStatus.Unknown;
}

public enum OnlineStatus
{
    Unknown,
    Checking,
    Online,
    Offline
}
