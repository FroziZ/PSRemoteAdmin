using System.Management.Automation;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Core.Services;

public interface IRemoteExecutionService
{
    /// <summary>
    /// Executes script against each machine in parallel (throttled by maxConcurrency).
    /// Yields results as each machine completes. Pass credential=null to use current Windows credentials.
    /// </summary>
    IAsyncEnumerable<ExecutionResult> ExecuteAsync(
        string script,
        IReadOnlyList<MachineTarget> machines,
        PSCredential? credential,
        int maxConcurrency,
        CancellationToken cancellationToken = default);
}
