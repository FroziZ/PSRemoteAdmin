using PSRemoteAdmin.ViewModels;
using System.Windows;

namespace PSRemoteAdmin.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
