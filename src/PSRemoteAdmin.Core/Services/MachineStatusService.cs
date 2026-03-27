using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PSRemoteAdmin.Core.Models;

namespace PSRemoteAdmin.Core.Services;

public class MachineStatusService : IMachineStatusService
{
    private const int MaxConcurrentProbes = 50;
    private const int ProbeTimeoutMs = 3000;

    private readonly IOptions<AppSettings> _options;
    private readonly ILogger<MachineStatusService> _logger;

    public MachineStatusService(IOptions<AppSettings> options, ILogger<MachineStatusService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async IAsyncEnumerable<(string MachineName, OnlineStatus Status)> CheckStatusAsync(
        IReadOnlyList<MachineTarget> machines,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var port = _options.Value.WinRmPort;
        var semaphore = new SemaphoreSlim(MaxConcurrentProbes);
        var channel = Channel.CreateUnbounded<(string, OnlineStatus)>();

        var tasks = machines.Select(async m =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var status = await ProbeAsync(m.DnsHostName ?? m.Name, port, cancellationToken);
                await channel.Writer.WriteAsync((m.Name, status), cancellationToken);
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

    private static async Task<OnlineStatus> ProbeAsync(string host, int port, CancellationToken ct)
    {
        try
        {
            using var tcp = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ProbeTimeoutMs);
            await tcp.ConnectAsync(host, port, cts.Token);
            return OnlineStatus.Online;
        }
        catch
        {
            return OnlineStatus.Offline;
        }
    }
}
