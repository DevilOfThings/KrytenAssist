using System.Collections.Specialized;
using Avalonia.Input;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KrytenAssist.Avalonia.ViewModels;
using Microsoft.Extensions.DependencyInjection;


namespace KrytenAssist.Avalonia;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ConversationInputBox.AddHandler(
            InputElement.KeyDownEvent,
            ConversationInput_OnKeyDown,
            RoutingStrategies.Tunnel);

        var viewModel = Program.Services.GetRequiredService<MainWindowViewModel>();
        DataContext = viewModel;
        viewModel.ConversationHistory.CollectionChanged += ConversationHistory_OnCollectionChanged;

        Opened += async (_, _) => await viewModel.LoadAsync();
    }

    private void ConversationInput_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        e.Handled = true;

        if (DataContext is MainWindowViewModel viewModel &&
            viewModel.SendMessageCommand.CanExecute(null))
        {
            viewModel.SendMessageCommand.Execute(null);
        }

        ConversationInputBox.Focus();
    }

    private void ConversationHistory_OnCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems is null)
        {
            return;
        }

        var newestMessage = e.NewItems[^1];
        Dispatcher.UIThread.Post(() =>
        {
            ConversationList.ScrollIntoView(newestMessage);
            ConversationInputBox.Focus();
        });
    }
}
