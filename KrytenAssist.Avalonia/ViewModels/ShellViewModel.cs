using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using KrytenAssist.Avalonia.Navigation.Models;
using KrytenAssist.Avalonia.Skills.Models;
using KrytenAssist.Avalonia.Skills.Services;

namespace KrytenAssist.Avalonia.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private const string DashboardNavigationId = "navigation.dashboard";
    private const string AssistantNavigationId = "navigation.assistant";
    private const string SkillNavigationIdPrefix = "navigation.skill:";

    private readonly Dictionary<string, SkillManifest> _manifestsBySkillId;
    private NavigationItem _selectedNavigationItem;
    private SkillManifest? _selectedSkillManifest;

    public ShellViewModel(
        MainWindowViewModel assistantWorkspace,
        CruiseOfTheWeekViewModel cruiseOfTheWeek,
        ISkillRegistry skillRegistry)
    {
        ArgumentNullException.ThrowIfNull(assistantWorkspace);
        ArgumentNullException.ThrowIfNull(cruiseOfTheWeek);
        ArgumentNullException.ThrowIfNull(skillRegistry);

        AssistantWorkspace = assistantWorkspace;
        CruiseOfTheWeek = cruiseOfTheWeek;

        var dashboard = new NavigationItem(
            DashboardNavigationId,
            "Dashboard",
            NavigationDestinationKind.Dashboard);
        var assistant = new NavigationItem(
            AssistantNavigationId,
            "Assistant",
            NavigationDestinationKind.Assistant);

        var navigationItems = new List<NavigationItem> { dashboard, assistant };
        var dashboardCards = new List<DashboardSkillCard>();
        _manifestsBySkillId = new Dictionary<string, SkillManifest>(StringComparer.Ordinal);

        var skills = skillRegistry.Skills;
        foreach (var skill in skills)
        {
            var manifest = skill.Manifest;

            navigationItems.Add(new NavigationItem(
                $"{SkillNavigationIdPrefix}{manifest.Id}",
                manifest.Name,
                NavigationDestinationKind.Skill,
                manifest.Id));
            dashboardCards.Add(new DashboardSkillCard(
                manifest.Id,
                manifest.Name,
                manifest.Description,
                manifest.Version));
            _manifestsBySkillId.Add(manifest.Id, manifest);
        }

        NavigationItems = navigationItems.AsReadOnly();
        DashboardCards = dashboardCards.AsReadOnly();
        _selectedNavigationItem = dashboard;
        NavigateCommand = new NavigationCommand(Navigate);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainWindowViewModel AssistantWorkspace { get; }

    public CruiseOfTheWeekViewModel CruiseOfTheWeek { get; }

    public IReadOnlyList<NavigationItem> NavigationItems { get; }

    public IReadOnlyList<DashboardSkillCard> DashboardCards { get; }

    public bool HasDashboardCards => DashboardCards.Count > 0;

    public NavigationItem SelectedNavigationItem => _selectedNavigationItem;

    public SkillManifest? SelectedSkillManifest => _selectedSkillManifest;

    public bool IsDashboardSelected =>
        SelectedNavigationItem.Kind == NavigationDestinationKind.Dashboard;

    public bool IsAssistantSelected =>
        SelectedNavigationItem.Kind == NavigationDestinationKind.Assistant;

    public bool IsSkillSelected =>
        SelectedNavigationItem.Kind == NavigationDestinationKind.Skill;

    public bool IsCruiseOfTheWeekSelected =>
        IsSkillSelected &&
        string.Equals(SelectedNavigationItem.SkillId, "cruise.of-the-week", StringComparison.Ordinal);

    public bool IsGenericSkillSelected =>
        IsSkillSelected && !IsCruiseOfTheWeekSelected;

    public ICommand NavigateCommand { get; }

    private void Navigate(object? parameter)
    {
        NavigationItem? canonicalItem = parameter switch
        {
            NavigationItem requestedItem => FindByNavigationId(requestedItem.Id),
            DashboardSkillCard dashboardCard => FindBySkillId(dashboardCard.SkillId),
            _ => null
        };

        Select(canonicalItem);
    }

    private NavigationItem? FindByNavigationId(string navigationId)
    {
        foreach (var item in NavigationItems)
        {
            if (string.Equals(item.Id, navigationId, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private NavigationItem? FindBySkillId(string skillId)
    {
        foreach (var item in NavigationItems)
        {
            if (item.Kind == NavigationDestinationKind.Skill &&
                string.Equals(item.SkillId, skillId, StringComparison.Ordinal))
            {
                return item;
            }
        }

        return null;
    }

    private void Select(NavigationItem? canonicalItem)
    {
        if (canonicalItem is null || ReferenceEquals(canonicalItem, _selectedNavigationItem))
        {
            return;
        }

        var selectedManifest = canonicalItem.SkillId is not null &&
                               _manifestsBySkillId.TryGetValue(
                                   canonicalItem.SkillId,
                                   out var manifest)
            ? manifest
            : null;
        var manifestChanged = !ReferenceEquals(selectedManifest, _selectedSkillManifest);
        var wasCruiseSelected = IsCruiseOfTheWeekSelected;

        _selectedNavigationItem = canonicalItem;
        _selectedSkillManifest = selectedManifest;
        OnPropertyChanged(nameof(SelectedNavigationItem));
        OnPropertyChanged(nameof(IsDashboardSelected));
        OnPropertyChanged(nameof(IsAssistantSelected));
        OnPropertyChanged(nameof(IsSkillSelected));
        OnPropertyChanged(nameof(IsCruiseOfTheWeekSelected));
        OnPropertyChanged(nameof(IsGenericSkillSelected));

        if (manifestChanged)
        {
            OnPropertyChanged(nameof(SelectedSkillManifest));
        }

        if (IsCruiseOfTheWeekSelected)
        {
            CruiseOfTheWeek.Activate();
        }
        else if (wasCruiseSelected)
        {
            CruiseOfTheWeek.Deactivate();
        }
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
