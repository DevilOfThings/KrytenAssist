using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KrytenAssist.Avalonia.Navigation.Models;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private const string DashboardNavigationId = "navigation.dashboard";
    private const string AssistantNavigationId = "navigation.assistant";

    private NavigationItem _selectedNavigationItem;

    public ShellViewModel(MainWindowViewModel assistantWorkspace)
    {
        ArgumentNullException.ThrowIfNull(assistantWorkspace);

        AssistantWorkspace = assistantWorkspace;

        var dashboard = new NavigationItem(
            DashboardNavigationId,
            "Dashboard",
            NavigationDestinationKind.Dashboard);
        var assistant = new NavigationItem(
            AssistantNavigationId,
            "Assistant",
            NavigationDestinationKind.Assistant);

        NavigationItems = Array.AsReadOnly([dashboard, assistant]);
        _selectedNavigationItem = dashboard;
        NavigateCommand = new NavigationCommand(Navigate);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel AssistantWorkspace { get; }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public NavigationItem SelectedNavigationItem => _selectedNavigationItem;

    public bool IsDashboardSelected =>
        SelectedNavigationItem.Kind == NavigationDestinationKind.Dashboard;

    public bool IsAssistantSelected =>
        SelectedNavigationItem.Kind == NavigationDestinationKind.Assistant;

    public bool IsSkillSelected =>
        SelectedNavigationItem.Kind == NavigationDestinationKind.Skill;

    public ICommand NavigateCommand { get; }

    private void Navigate(object? parameter)
    {
        if (parameter is not NavigationItem requestedItem)
        {
            return;
        }

        NavigationItem? canonicalItem = null;
        foreach (var item in NavigationItems)
        {
            if (string.Equals(item.Id, requestedItem.Id, StringComparison.Ordinal))
            {
                canonicalItem = item;
                break;
            }
        }

        if (canonicalItem is null || ReferenceEquals(canonicalItem, _selectedNavigationItem))
        {
            return;
        }

        _selectedNavigationItem = canonicalItem;
        OnPropertyChanged(nameof(SelectedNavigationItem));
        OnPropertyChanged(nameof(IsDashboardSelected));
        OnPropertyChanged(nameof(IsAssistantSelected));
        OnPropertyChanged(nameof(IsSkillSelected));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed class NavigationCommand : ICommand
    {
        private readonly Action<object?> _execute;

        public NavigationCommand(Action<object?> execute)
        {
            ArgumentNullException.ThrowIfNull(execute);

            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute(parameter);
    }
}
