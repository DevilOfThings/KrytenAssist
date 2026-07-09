using Avalonia.Controls;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = Program.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = viewModel;

        Opened += async (_, _) => await viewModel.LoadAsync();
    }
}