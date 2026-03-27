using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PSRemoteAdmin.Core.Exceptions;
using PSRemoteAdmin.Core.Models;
using PSRemoteAdmin.Core.Services;
using PSRemoteAdmin.Services;
using Serilog;

namespace PSRemoteAdmin.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IActiveDirectoryService _adService;
    private readonly IRemoteExecutionService _executionService;
    private readonly IMachineStatusService _statusService;
    private readonly AppSettings _settings;
    private readonly CredentialService _credentialService;

    private CancellationTokenSource? _executeCts;
    private CancellationTokenSource? _statusCts;
    private Timer? _statusDebounceTimer;

    [ObservableProperty] private bool _isExecuting;
    [ObservableProperty] private bool _isLoadingTree;
    [ObservableProperty] private string _commandText = string.Empty;
    [ObservableProperty] private string? _loadedFilePath;
    [ObservableProperty] private CommandMode _activeMode = CommandMode.Manual;
    [ObservableProperty] private string? _errorBanner;

    public ObservableCollection<AdTreeNodeViewModel> AdTreeRoots { get; } = new();
    public ObservableCollection<MachineTargetViewModel> TargetMachines { get; } = new();
    public ObservableCollection<ExecutionResultViewModel> ExecutionResults { get; } = new();

    public string ExecuteButtonLabel => TargetMachines.Count > 0
        ? $"▶  Execute ({TargetMachines.Count})"
        : "▶  Execute";

    public MainViewModel(
        IActiveDirectoryService adService,
        IRemoteExecutionService executionService,
        IMachineStatusService statusService,
        AppSettings settings,
        CredentialService credentialService)
    {
        _adService = adService;
        _executionService = executionService;
        _statusService = statusService;
        _settings = settings;
        _credentialService = credentialService;

        TargetMachines.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ExecuteButtonLabel));
    }

    public async Task InitializeAsync()
    {
        await LoadTreeAsync();
    }

    [RelayCommand]
    private async Task LoadTreeAsync()
    {
        IsLoadingTree = true;
        ErrorBanner = null;
        AdTreeRoots.Clear();
        TargetMachines.Clear();

        try
        {
            var roots = await _adService.GetRootNodesAsync();
            foreach (var node in roots)
            {
                var vm = CreateNodeViewModel(node);
                AdTreeRoots.Add(vm);
            }
        }
        catch (ActiveDirectoryServiceException ex)
        {
            ErrorBanner = $"Cannot connect to Active Directory: {ex.Message}. Check Settings.";
            Log.Warning(ex, "AD connection failed");
        }
        finally
        {
            IsLoadingTree = false;
        }
    }

    [RelayCommand]
    private async Task LoadChildrenAsync(AdTreeNodeViewModel node)
    {
        if (node.IsLoadingChildren || !node.HasDummyChild) return;

        node.IsLoadingChildren = true;
        try
        {
            var children = await _adService.GetChildrenAsync(node.Node.DistinguishedName);
            var childVms = children.Select(CreateNodeViewModel).ToList();
            node.SetChildren(childVms);
        }
        catch (ActiveDirectoryServiceException ex)
        {
            ErrorBanner = $"Failed to load '{node.Name}': {ex.Message}";
            Log.Warning(ex, "Failed to load children of {Node}", node.Name);
        }
        finally
        {
            node.IsLoadingChildren = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecute))]
    private async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(CommandText) || TargetMachines.Count == 0) return;

        IsExecuting = true;
        ExecutionResults.Clear();
        _executeCts = new CancellationTokenSource();

        var targets = TargetMachines.Select(m => m.Target).ToList();
        var script = CommandText;
        var credResult = _credentialService.GetRunAsCredential(_settings);

        if (credResult.PasswordMissing)
            ErrorBanner = "RunAs credentials not found in vault — using current Windows credentials.";

        Log.Information("Executing command against {Count} machines. User={User} Mode={Mode} Script={Script}",
            targets.Count,
            Environment.UserName,
            ActiveMode,
            script.Length > 500 ? script[..500] + "..." : script);

        try
        {
            await foreach (var result in _executionService.ExecuteAsync(
                script, targets, credResult.Credential,
                _settings.MaxConcurrency, _executeCts.Token))
            {
                var vm = new ExecutionResultViewModel(result);
                Application.Current.Dispatcher.Invoke(() => ExecutionResults.Add(vm));

                Log.Information("Result: Machine={Machine} Success={Success} ExitCode={ExitCode} Duration={Duration}",
                    result.MachineName, !result.HadErrors, result.ExitCode, result.Duration);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("Execution cancelled by user");
        }
        finally
        {
            _executeCts.Dispose();
            _executeCts = null;
            IsExecuting = false;
            ExecuteCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanExecute() => !IsExecuting && TargetMachines.Count > 0 && !string.IsNullOrWhiteSpace(CommandText);

    [RelayCommand]
    private void CancelExecution()
    {
        _executeCts?.Cancel();
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select PowerShell Script",
            Filter = "PowerShell Scripts (*.ps1)|*.ps1|All Files (*.*)|*.*",
            FilterIndex = 1
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var content = File.ReadAllText(dlg.FileName);
            LoadedFilePath = dlg.FileName;
            CommandText = content;
            ActiveMode = CommandMode.File;
        }
        catch (Exception ex)
        {
            ErrorBanner = $"Failed to read file: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? SettingsRequested;

    // Called when ActiveMode tab changes in View (Manual -> clear file path)
    partial void OnActiveModeChanged(CommandMode value)
    {
        if (value == CommandMode.Manual)
            LoadedFilePath = null;
    }

    // Called when CommandText changes — re-evaluate execute button
    partial void OnCommandTextChanged(string value)
    {
        ExecuteCommand.NotifyCanExecuteChanged();
    }

    private AdTreeNodeViewModel CreateNodeViewModel(AdNode node)
    {
        return new AdTreeNodeViewModel(node, OnNodeSelectionChanged);
    }

    private void OnNodeSelectionChanged(AdTreeNodeViewModel _)
    {
        RebuildTargetList();
        ScheduleStatusCheck();
    }

    private void RebuildTargetList()
    {
        TargetMachines.Clear();
        CollectSelectedComputers(AdTreeRoots, TargetMachines);
    }

    private static void CollectSelectedComputers(
        IEnumerable<AdTreeNodeViewModel> nodes,
        ObservableCollection<MachineTargetViewModel> result)
    {
        foreach (var node in nodes)
        {
            if (node.Node.NodeType == AdNodeType.Computer && node.IsSelected == true)
            {
                result.Add(new MachineTargetViewModel(new MachineTarget
                {
                    Name = node.Node.Name,
                    DnsHostName = node.Node.DnsHostName,
                    DistinguishedName = node.Node.DistinguishedName
                }));
            }
            else if (node.Node.NodeType == AdNodeType.OrganizationalUnit)
            {
                CollectSelectedComputers(node.Children, result);
            }
        }
    }

    private void ScheduleStatusCheck()
    {
        _statusDebounceTimer?.Dispose();
        _statusDebounceTimer = new Timer(async _ => await RunStatusCheckAsync(), null,
            TimeSpan.FromMilliseconds(500), Timeout.InfiniteTimeSpan);
    }

    private async Task RunStatusCheckAsync()
    {
        _statusCts?.Cancel();
        _statusCts = new CancellationTokenSource();
        var cts = _statusCts;

        var targets = Application.Current.Dispatcher.Invoke(
            () => TargetMachines.Select(m => m.Target).ToList());

        if (targets.Count == 0) return;

        // Set all to Checking
        Application.Current.Dispatcher.Invoke(() =>
        {
            foreach (var m in TargetMachines) m.Status = OnlineStatus.Checking;
        });

        try
        {
            await foreach (var (name, status) in _statusService.CheckStatusAsync(targets, cts.Token))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var vm = TargetMachines.FirstOrDefault(m => m.Name == name);
                    if (vm != null) vm.Status = status;
                });
            }
        }
        catch (OperationCanceledException) { /* debounced away */ }
    }
}
