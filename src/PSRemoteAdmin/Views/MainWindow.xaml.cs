using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using PSRemoteAdmin.Core.Models;
using PSRemoteAdmin.ViewModels;

namespace PSRemoteAdmin.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;

        viewModel.SettingsRequested += (_, _) =>
        {
            var settingsVm = App.Services.GetRequiredService<SettingsViewModel>();
            var win = new SettingsWindow(settingsVm) { Owner = this };
            if (win.ShowDialog() == true)
                viewModel.LoadTreeCommand.Execute(null);
        };

        Loaded += async (_, _) => await viewModel.InitializeAsync();
    }

    // e.OriginalSource identifies the specific TreeViewItem that expanded (not sender=TreeView).
    // e.Handled=true prevents parent TreeViewItems from also firing this handler as the event bubbles.
    private void TreeViewItem_Expanded(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is TreeViewItem { DataContext: AdTreeNodeViewModel nodeVm })
        {
            _vm.LoadChildrenCommand.Execute(nodeVm);
            e.Handled = true;
        }
    }

    private void ManualMode_Click(object sender, RoutedEventArgs e)
    {
        _vm.ActiveMode = CommandMode.Manual;
    }

    private void FileMode_Click(object sender, RoutedEventArgs e)
    {
        _vm.ActiveMode = CommandMode.File;
        if (_vm.LoadedFilePath == null)
            _vm.BrowseFileCommand.Execute(null);
    }
}
