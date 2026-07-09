using Avalonia.Controls;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Interactivity;


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
    
    private async void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.SaveAsync();
        }
    }
}