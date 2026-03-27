using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Core.Services;

public interface IMachineStatusService
{
    /// <summary>
    /// Probes each machine concurrently (capped at 50) and yields results as they complete.
    /// </summary>
    IAsyncEnumerable<(string MachineName, OnlineStatus Status)> CheckStatusAsync(
        IReadOnlyList<MachineTarget> machines,
        CancellationToken cancellationToken = default);
}
