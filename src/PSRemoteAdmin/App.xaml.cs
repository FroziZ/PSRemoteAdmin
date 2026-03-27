using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PSRemoteAdmin.Core.Configuration;
using PSRemoteAdmin.Core.Models;
using PSRemoteAdmin.Core.Services;
using PSRemoteAdmin.Services;
using PSRemoteAdmin.ViewModels;
using Serilog;
using System.Windows;

namespace PSRemoteAdmin;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Load settings first so Serilog can use the configured log path
        var settingsProvider = new AppSettingsProvider();
        var settings = settingsProvider.Load();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                settings.LogFilePath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices((_, services) =>
            {
                // Settings
                services.AddSingleton(settingsProvider);
                services.AddSingleton(settings);
                services.AddSingleton<IOptions<AppSettings>>(
                    new OptionsWrapper<AppSettings>(settings));

                // Core services
                services.AddSingleton<IActiveDirectoryService, ActiveDirectoryService>();
                services.AddSingleton<IRemoteExecutionService, RemoteExecutionService>();
                services.AddSingleton<IMachineStatusService, MachineStatusService>();

                // WPF services
                services.AddSingleton<CredentialService>();

                // ViewModels
                services.AddTransient<MainViewModel>();
                services.AddTransient<SettingsViewModel>();

                // Windows
                services.AddTransient<Views.MainWindow>();
                services.AddTransient<Views.SettingsWindow>();
            })
            .Build();

        await _host.StartAsync();

        var mainWindow = _host.Services.GetRequiredService<Views.MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
