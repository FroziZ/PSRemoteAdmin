using System.Security;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PSRemoteAdmin.Core.Configuration;
using PSRemoteAdmin.Core.Exceptions;
using PSRemoteAdmin.Core.Models;
using PSRemoteAdmin.Core.Services;
using PSRemoteAdmin.Services;

namespace PSRemoteAdmin.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettingsProvider _provider;
    private readonly CredentialService _credentialService;
    private readonly IActiveDirectoryService _adService;
    private readonly AppSettings _current;

    [ObservableProperty] private string _ldapConnectionString = string.Empty;
    [ObservableProperty] private string _domain = string.Empty;
    [ObservableProperty] private int _winRmPort = 5985;
    [ObservableProperty] private int _maxConcurrency = 10;
    [ObservableProperty] private string? _runAsUsername;
    [ObservableProperty] private string? _connectionTestMessage;
    [ObservableProperty] private bool _connectionTestSuccess;
    [ObservableProperty] private bool _isTesting;

    /// <summary>
    /// Set from code-behind when PasswordBox changes.
    /// Never stored as a string; held as SecureString only.
    /// </summary>
    public SecureString? RunAsPassword { get; set; }
    public bool PasswordChanged { get; set; }

    public SettingsViewModel(AppSettingsProvider provider, CredentialService credentialService,
        IActiveDirectoryService adService, AppSettings currentSettings)
    {
        _provider = provider;
        _credentialService = credentialService;
        _adService = adService;
        _current = currentSettings;

        // Populate from current settings
        LdapConnectionString = currentSettings.LdapConnectionString;
        Domain = currentSettings.Domain;
        WinRmPort = currentSettings.WinRmPort;
        MaxConcurrency = currentSettings.MaxConcurrency;
        RunAsUsername = currentSettings.RunAsUsername;
    }

    [RelayCommand]
    private async Task SaveAsync(Window window)
    {
        _current.LdapConnectionString = LdapConnectionString;
        _current.Domain = Domain;
        _current.WinRmPort = WinRmPort;
        _current.MaxConcurrency = MaxConcurrency;
        _current.RunAsUsername = RunAsUsername;

        await _provider.SaveAsync(_current);

        if (PasswordChanged && RunAsPassword != null && !string.IsNullOrWhiteSpace(RunAsUsername))
        {
            _credentialService.StorePassword(RunAsUsername, RunAsPassword);
        }
        else if (string.IsNullOrWhiteSpace(RunAsUsername))
        {
            if (_current.RunAsUsername != null)
                _credentialService.ClearPassword(_current.RunAsUsername);
        }

        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        ConnectionTestMessage = "Testing...";
        ConnectionTestSuccess = false;
        try
        {
            await _adService.TestConnectionAsync(LdapConnectionString, Domain);
            ConnectionTestMessage = "✅  Connection successful";
            ConnectionTestSuccess = true;
        }
        catch (ActiveDirectoryServiceException ex)
        {
            ConnectionTestMessage = $"❌  {ex.Message}";
            ConnectionTestSuccess = false;
        }
        finally
        {
            IsTesting = false;
        }
    }
}
