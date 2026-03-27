using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        try
        {
            // Wire up global exception handler
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

            var settingsProvider = new AppSettingsProvider();
            var settings = settingsProvider.Load();

            // Ensure log directory exists before Serilog initialises
            System.IO.Directory.CreateDirectory(
                System.IO.Path.GetDirectoryName(settings.LogFilePath)!);

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
                    services.AddSingleton(settingsProvider);
                    services.AddSingleton(settings);

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
                    // StartupUri is intentionally absent; window is opened here via DI
                    services.AddTransient<Views.MainWindow>();
                    services.AddTransient<Views.SettingsWindow>();
                })
                .Build();

            await _host.StartAsync();

            var mainWindow = _host.Services.GetRequiredService<Views.MainWindow>();
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Startup failed");
            Log.CloseAndFlush();
            MessageBox.Show($"Failed to start PSRemoteAdmin:\n{ex.Message}", "Startup Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during shutdown");
        }
        finally
        {
            Log.CloseAndFlush();
        }
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled UI thread exception");
        Log.CloseAndFlush();
        MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}", "Error",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
        Current.Shutdown(1);
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = e.ExceptionObject as Exception;
        Log.Fatal(ex, "Unhandled background thread exception (IsTerminating={IsTerminating})",
            e.IsTerminating);
        Log.CloseAndFlush();
    }
}
