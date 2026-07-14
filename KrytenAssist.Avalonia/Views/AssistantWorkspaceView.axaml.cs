using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using KrytenAssist.Avalonia.Models;
using KrytenAssist.Avalonia.ViewModels;

namespace KrytenAssist.Avalonia.Views;

public partial class AssistantWorkspaceView : UserControl
{
    private MainWindowViewModel? _viewModel;
    private bool _isAttached;
    private bool _hasLoaded;

    public AssistantWorkspaceView()
    {
        InitializeComponent();
        ConversationInputBox.AddHandler(
            InputElement.KeyDownEvent,
            ConversationInput_OnKeyDown,
            RoutingStrategies.Tunnel);
        DataContextChanged += AssistantWorkspaceView_OnDataContextChanged;
    }

    protected override async void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _isAttached = true;
        UpdateViewModelSubscription();

        if (!_hasLoaded && _viewModel is not null)
        {
            _hasLoaded = true;
            await _viewModel.LoadAsync();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttached = false;
        UpdateViewModelSubscription();

        base.OnDetachedFromVisualTree(e);
    }

    private async void AssistantWorkspaceView_OnDataContextChanged(
        object? sender,
        EventArgs e)
    {
        UpdateViewModelSubscription();

        if (_isAttached && !_hasLoaded && _viewModel is not null)
        {
            _hasLoaded = true;
            await _viewModel.LoadAsync();
        }
    }

    private void UpdateViewModelSubscription()
    {
        var currentViewModel = _isAttached ? DataContext as MainWindowViewModel : null;

        if (ReferenceEquals(_viewModel, currentViewModel))
        {
            return;
        }

        if (_viewModel is not null)
        {
            _viewModel.ConversationHistory.CollectionChanged -=
                ConversationHistory_OnCollectionChanged;
        }

        _viewModel = currentViewModel;

        if (_viewModel is not null)
        {
            _viewModel.ConversationHistory.CollectionChanged +=
                ConversationHistory_OnCollectionChanged;
        }
    }

    private void ConversationInput_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return;
        }

        e.Handled = true;

        if (_viewModel is not null && _viewModel.SendMessageCommand.CanExecute(null))
        {
            _viewModel.SendMessageCommand.Execute(null);
        }

        ConversationInputBox.Focus();
    }

    private void UsePrompt_OnClick(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() => ConversationInputBox.Focus());
    }

    private void PromptCard_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (_viewModel is not null &&
            sender is Control { DataContext: PromptCardModel prompt } &&
            _viewModel.OpenEditPromptCommand.CanExecute(prompt))
        {
            _viewModel.OpenEditPromptCommand.Execute(prompt);
        }
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

        if (newestMessage is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            ConversationList.ScrollIntoView(newestMessage);
            ConversationInputBox.Focus();
        });
    }
}
