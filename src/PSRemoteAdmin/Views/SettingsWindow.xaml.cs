using PSRemoteAdmin.ViewModels;
using System.Windows;

namespace PSRemoteAdmin.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
