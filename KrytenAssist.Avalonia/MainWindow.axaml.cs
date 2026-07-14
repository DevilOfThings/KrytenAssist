using Avalonia.Controls;
using Avalonia.Controls.Selection;
using KrytenAssist.Avalonia.Navigation.Models;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace KrytenAssist.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = Program.Services.GetRequiredService<ShellViewModel>();
    }

    private void NavigationList_OnSelectionChanged(
        object? sender,
        SelectionChangedEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel &&
            NavigationList.SelectedItem is NavigationItem navigationItem &&
            viewModel.NavigateCommand.CanExecute(navigationItem))
        {
            viewModel.NavigateCommand.Execute(navigationItem);
        }
    }
}
