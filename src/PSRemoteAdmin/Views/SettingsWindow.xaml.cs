using System.Security;
using System.Windows;
using PSRemoteAdmin.ViewModels;

namespace PSRemoteAdmin.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        // Transfer password to ViewModel as SecureString, never as plain string
        var secure = new SecureString();
        foreach (char c in PasswordBox.Password)
            secure.AppendChar(c);
        secure.MakeReadOnly();
        _vm.RunAsPassword?.Dispose();
        _vm.RunAsPassword = secure;
        _vm.PasswordChanged = true;
    }
}
