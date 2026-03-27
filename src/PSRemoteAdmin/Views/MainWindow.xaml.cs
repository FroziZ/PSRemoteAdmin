using Microsoft.Extensions.DependencyInjection;
using PSRemoteAdmin.ViewModels;
using System.Windows;

namespace PSRemoteAdmin.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.SettingsRequested += (_, _) =>
        {
            var settingsVm = App.Services.GetRequiredService<SettingsViewModel>();
            var win = new SettingsWindow(settingsVm) { Owner = this };
            if (win.ShowDialog() == true)
            {
                // Reload AD tree after settings change
                viewModel.LoadTreeCommand.Execute(null);
            }
        };

        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }
}
