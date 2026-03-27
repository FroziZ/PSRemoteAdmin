using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Core.Services;

public class RemoteExecutionService : IRemoteExecutionService
{
    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<RemoteExecutionService> _logger;

    public RemoteExecutionService(IOptions<AppSettings> options, ILogger<RemoteExecutionService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async IAsyncEnumerable<ExecutionResult> ExecuteAsync(
        string script,
        IReadOnlyList<MachineTarget> machines,
        PSCredential? credential,
        int maxConcurrency,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var semaphore = new SemaphoreSlim(maxConcurrency);
        var channel = Channel.CreateUnbounded<ExecutionResult>();

        var tasks = machines.Select(async machine =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (cancellationToken.IsCancellationRequested) return;
                var result = await ExecuteOnMachineAsync(script, machine, credential, cancellationToken);
                await channel.Writer.WriteAsync(result, CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
                await channel.Writer.WriteAsync(new ExecutionResult
                {
                    MachineName = machine.Name,
                    ErrorOutput = "Execution was cancelled.",
                    HadErrors = true,
                    ExitCode = null,
                    Timestamp = DateTime.Now,
                    Duration = TimeSpan.Zero
                }, CancellationToken.None);
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        _ = Task.WhenAll(tasks).ContinueWith(_ => channel.Writer.Complete(), CancellationToken.None);

        await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
        {
            yield return result;
        }
    }

    private async Task<ExecutionResult> ExecuteOnMachineAsync(
        string script,
        MachineTarget machine,
        PSCredential? credential,
        CancellationToken cancellationToken)
    {
        var start = DateTime.Now;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var connectionInfo = BuildConnectionInfo(machine, credential);
            using var runspace = RunspaceFactory.CreateRunspace(connectionInfo);
            await Task.Run(() => runspace.Open(), cancellationToken);

            using var ps = PowerShell.Create();
            ps.Runspace = runspace;
            ps.AddScript(script);

            var outputSb = new System.Text.StringBuilder();
            var errorSb = new System.Text.StringBuilder();

            ps.Streams.Error.DataAdded += (_, e) =>
            {
                var err = ps.Streams.Error[e.Index];
                errorSb.AppendLine(err.ToString());
            };

            var output = await Task.Run(() => ps.Invoke(), cancellationToken);
            foreach (var item in output)
                outputSb.AppendLine(item?.ToString() ?? string.Empty);

            stopwatch.Stop();

            int? exitCode = ps.HadErrors ? 1 : 0;
            if (ps.Runspace.SessionStateProxy != null)
            {
                try
                {
                    var lastExit = ps.Runspace.SessionStateProxy.GetVariable("LASTEXITCODE");
                    if (lastExit is int code) exitCode = code;
                }
                catch { /* non-critical */ }
            }

            return new ExecutionResult
            {
                MachineName = machine.Name,
                Output = outputSb.ToString().TrimEnd(),
                ErrorOutput = errorSb.ToString().TrimEnd(),
                HadErrors = ps.HadErrors,
                ExitCode = exitCode,
                Timestamp = start,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Failed to execute on {Machine}", machine.Name);
            return new ExecutionResult
            {
                MachineName = machine.Name,
                ErrorOutput = ex.Message,
                HadErrors = true,
                ExitCode = null,
                Timestamp = start,
                Duration = stopwatch.Elapsed
            };
        }
    }

    private WSManConnectionInfo BuildConnectionInfo(MachineTarget machine, PSCredential? credential)
    {
        var host = machine.DnsHostName ?? machine.Name;
        var port = _options.Value.WinRmPort;
        var info = new WSManConnectionInfo(new Uri($"http://{host}:{port}/wsman"));
        if (credential != null)
            info.Credential = credential;
        return info;
    }
}
